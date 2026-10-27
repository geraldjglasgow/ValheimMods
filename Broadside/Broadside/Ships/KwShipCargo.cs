using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's cargo: what the players aboard carry and the carts on its deck (<see cref="KwShipLoad.Carts"/>).
    /// Each player's own game, while aboard, tells the ship's owner what its inventory weighs every few seconds
    /// (<see cref="Call"/>; nobody else knows another player's inventory); the owner adds up the players aboard and the
    /// carts every two seconds and keeps the sum in the ship's ZDO (<see cref="LoadKey"/>), which every peer shows. On the
    /// owner, where the physics runs, the load is added to the body's mass, so the hull floats deeper on its own
    /// (<see cref="KwShipHydro"/> lifts by the water displaced), and the drag along the ship grows with it
    /// (<see cref="KwShipLoad.Drag"/>), so a laden ship is slower. The mass and the drag are changed by what this
    /// component added before, never set outright, so whatever else changes them stays. Past the most the hull can float
    /// (<see cref="KwShipLoad.Most"/>) the ship is overloaded: it sits no deeper (it would swamp), but the extra weight
    /// keeps slowing it, to at most four times the drag (about half speed), and everyone aboard is told.
    /// </summary>
    public sealed class KwShipCargo : MonoBehaviour
    {
        public const string LoadKey = "kw_ship_load";
        private const string Call = "KW_ShipLoad";
        private const float ReportEvery = 3f, TallyEvery = 2f, Fresh = 10f, WarnEvery = 30f;
        private static readonly int LoadHash = LoadKey.GetStableHashCode();

        private readonly Dictionary<long, (float Weight, float At)> reports = new Dictionary<long, (float Weight, float At)>();
        private Ship? ship;
        private ZNetView? view;
        private KwBpHull? hull;
        private float unladen, most, added, dragged = 1f, nextReport, nextTally, warned = -99f;

        /// <summary>The load aboard as the owner last counted it (kg).</summary>
        public float Load => view != null && view.IsValid() ? view.GetZDO().GetFloat(LoadHash) : 0f;

        /// <summary>The most load the hull floats (kg).</summary>
        public float Most => most;

        public bool Overloaded => Load > most + 1f;

        public void Set(Ship game, KwShipHydro hydro, KwBpHull measured)
        {
            (ship, view, hull) = (game, GetComponent<ZNetView>(), measured);
            unladen = game.m_body.mass;
            most = KwShipLoad.Most(hydro.Slices, measured, hydro.Waterline, unladen);
            if (view != null && view.GetZDO() != null)
                view.Register<float>(Call, (sender, weight) => reports[sender] = (weight, Time.time));
        }

        private void Update()
        {
            if (ship == null || view == null || !view.IsValid())
                return;
            float now = Time.time;
            if (now >= nextReport)
            {
                nextReport = now + ReportEvery;
                Report();
            }
            if (now < nextTally || !view.IsOwner())
                return;
            nextTally = now + TallyEvery;
            Tally();
        }

        // This player's game, while aboard: what its inventory weighs, to the ship's owner; told when it is overloaded.
        private void Report()
        {
            Player? me = Player.m_localPlayer;
            if (me == null || !ship!.IsPlayerInBoat(me))
                return;
            view!.InvokeRPC(Call, me.GetInventory().GetTotalWeight());
            if (!Overloaded || Time.time - warned < WarnEvery)
                return;
            warned = Time.time;
            me.Message(MessageHud.MessageType.Center, "The ship is overloaded: it sits as low as it can and crawls");
        }

        // On the owner: the players aboard (as they last told it) and the carts on deck, onto the body and into the ZDO.
        private void Tally()
        {
            float load = Players() + KwShipLoad.Carts(transform, hull!);
            Apply(load);
            if (Mathf.Abs(load - Load) >= 1f)
                view!.GetZDO().Set(LoadHash, load);
        }

        private float Players()
        {
            float sum = 0f;
            foreach (Player player in ship!.m_players)
            {
                if (player != null && reports.TryGetValue(player.GetOwner(), out (float Weight, float At) report) && Time.time - report.At < Fresh)
                    sum += report.Weight;
            }
            return sum;
        }

        // The floated part of the load onto the body's mass (its turning weight with it), the whole load onto the drag.
        private void Apply(float load)
        {
            Rigidbody body = ship!.m_body;
            float floated = Mathf.Min(load, most);
            if (Mathf.Abs(floated - added) >= 0.5f)
            {
                float mass = Mathf.Max(1f, body.mass + floated - added);
                body.inertiaTensor *= mass / body.mass;
                body.mass = mass;
                added = floated;
            }
            float drag = KwShipLoad.Drag(load, unladen);
            ship.m_dampingForward *= drag / dragged;
            dragged = drag;
        }
    }
}
