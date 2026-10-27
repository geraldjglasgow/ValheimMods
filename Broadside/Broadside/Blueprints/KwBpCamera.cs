using BuildCamera;
using HarmonyLib;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The blueprint table's "Build ship" camera, on the BuildCamera library: the local player's view leaves the body and
    /// flies free round the dock the table serves, at most <see cref="Range"/> m from the dock's keel line (from its aft
    /// piece to the end of its fore one, so the whole dock is covered, its middle and both ends), above the ground and the
    /// water. The game's movement keys fly it flat, Jump and Crouch raise and lower it, Run speeds it up, mouse look turns
    /// it. The hammer (taken in hand from the inventory if need be) places, removes and repairs from it along its view, as
    /// far as the game's 50 m build ray reaches, and the kit's tool clicks aim the same way (<see cref="KwTools.Aim"/> reads
    /// the game camera); the build menu and turning pieces work as usual. The body stays by the table at rest. The button
    /// again, Esc, death, a teleport, putting the hammer away or being hurt brings the view back. Local only: pieces go
    /// through the game's own networked placement, and other players see the builder standing by the table.
    /// </summary>
    public static class KwBpCamera
    {
        public const float Range = 30f;
        private const float Speed = 10f, RunMultiplier = 3f, Reach = 50f, LookEvery = 0.5f;
        private static float lookedAt = float.MinValue;
        private static bool serves;

        public static bool Out => FreeCamera.IsOut;

        /// <summary>The library's patches (none on a dedicated server); a game member it cannot find is logged and the camera stays off.</summary>
        public static void Install()
        {
            foreach (string missing in FreeCamera.Install(new Harmony(Broadside.PluginGuid + ".camera")))
                Log.Error($"Build camera off: the game has no {missing}");
        }

        /// <summary>The table the window is open at serves a dock (looked up twice a second, not every frame the window shows).</summary>
        public static bool Serves()
        {
            if (Time.time - lookedAt < LookEvery)
                return serves;
            lookedAt = Time.time;
            Component? table = KwBpShelf.Shelf.Station;
            serves = table != null && KwBpDock.Find(table.transform.position) != null;
            return serves;
        }

        /// <summary>The camera out over the dock with the hammer in hand, and the table's window closed; or why not.</summary>
        public static void Enter(Player player, KwBpDockInfo dock)
        {
            if (!CameraTool.Hold(player, IsShipTool))
                Say(player, "You need a hammer to build the ship");
            else if (!FreeCamera.Enter(player, Flight(dock)))
                Say(player, "The build camera cannot come out now");
            else
            {
                KwBpShelf.Shelf.Close();
                Say(player, "Build camera: $KEY_Forward $KEY_Left $KEY_Backward $KEY_Right fly, $KEY_Jump up, $KEY_Crouch down, $KEY_Run faster\nEsc or the table's Build ship comes back");
            }
        }

        public static void Leave() => FreeCamera.Leave();

        private static CameraFlight Flight(KwBpDockInfo dock)
        {
            Transform frame = dock.Frame;
            SegmentBounds keelLine = new SegmentBounds(frame.position, frame.position + frame.forward * dock.Length, Range);
            return new CameraFlight(keelLine)
            {
                Speed = Speed,
                RunMultiplier = RunMultiplier,
                Reach = _ => Reach,
                AboveGround = true,
                Stays = player => player.InPlaceMode(),
                WheelMoves = _ => KwForm.IsOpen,
                FliesWhile = _ => KwForm.IsOpen && !Hotkeys.Typing.Active,
                Ended = Ended,
            };
        }

        // The tool that holds the kit: the hammer, whose Misc tab carries the table among Broadside's pieces (KwHammer).
        private static bool IsShipTool(ItemDrop.ItemData item) =>
            item.m_shared.m_buildPieces.m_pieces.Exists(piece => piece != null && piece.name == KwBlueprintStation.Prefab);

        // Back by itself: the player is told only when the reason is not plain to see.
        private static void Ended(CameraEnd why)
        {
            Player? player = Player.m_localPlayer;
            if (player == null)
                return;
            if (why == CameraEnd.Hurt)
                Say(player, "You were hurt: the view is back with you");
            else if (why == CameraEnd.HostRule)
                Say(player, "The hammer is away: the view is back with you");
        }

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);
    }
}
