using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The sail, bowsprit and Post cap tools aimed at a launched ship (<see cref="KwSeaTools"/>), on the clicking player's
    /// game, as they work on the dock. Each takes its own part first: the sail tool a mast aimed at (its column, a yard, a
    /// boom, a sail: its window), else the top of a deck, a hold floor, a keel or a cabin roof (a new mast's window there);
    /// the bowsprit tool a bowsprit aimed at, else a stem or stern post's top aimed at (its bowsprit's window, or a new one's,
    /// running out along the keel the way the player looks); the Post cap tool a cap aimed at, else a post's top (its cap's
    /// window, or a new cap at once in the tool's style, length and lean, shown solid there while aimed at). Any other rig
    /// piece aimed at opens its own window (as the dock's tool switch does). Within the player's build reach only.
    /// </summary>
    public static class KwSeaRigTools
    {
        public const string Far = "Move closer to build there";

        public static bool Click(Player player, string tool, KwSeaAim aim)
        {
            if (KwForm.IsOpen)
                return true;
            int rig = Rigged(aim);
            if (tool == KwCapTool.Tool)
                return Cap(player, aim, rig);
            if (tool == KwBowspritTool.Tool)
                return Sprit(player, aim, rig);
            return Mast(player, aim, rig);
        }

        /// <summary>The Post cap tool's ghost on a free post's top aimed at; nothing for the others.</summary>
        public static bool Hover(Player player, string tool, KwSeaAim aim)
        {
            if (tool != KwCapTool.Tool || KwForm.IsOpen)
                return false;
            KwSeaPost? post = Post(aim);
            if (post == null || !aim.Within(player, post.Top) || KwSeaRigSites.On(aim.Ship.Blueprint, post.Top, KwCaps.Is) >= 0)
                return false;
            KwSeaRigGhost.Cap(aim.Ship, NewCap(post), true);
            return true;
        }

        // The rig piece whose hit collider the aim met first; -1 for none.
        private static int Rigged(KwSeaAim aim)
        {
            int index = aim.Rig != null ? aim.Rig.Piece : -1;
            return index >= 0 && index < aim.Ship.Blueprint.Pieces.Count && KwSeaRig.Is(aim.Ship.Blueprint.Pieces[index].Prefab) ? index : -1;
        }

        private static bool Mast(Player player, KwSeaAim aim, int rig)
        {
            if (rig >= 0)
                return Edit(player, aim, rig);
            if (!aim.Hit || !KwSeaRigSites.Deck(aim.Name, aim.Normal))
                return Say(player, KwSeaRigWords.MastPlace);
            if (!aim.Within(player, aim.Point))
                return Say(player, Far);
            Quaternion turn = KwShapeSites.Facing(Vector3.forward, aim.Ship.LocalWay(KwShapeSites.Way()));
            return KwSeaRigWindows.NewMast(player, aim.Ship, aim.Point, turn);
        }

        private static bool Sprit(Player player, KwSeaAim aim, int rig)
        {
            KwBlueprint blueprint = aim.Ship.Blueprint;
            if (rig >= 0 && blueprint.Pieces[rig].Prefab == KwBowsprit.Made)
                return Edit(player, aim, rig);
            KwSeaPost? post = Post(aim);
            if (post == null)
                return rig >= 0 ? Edit(player, aim, rig) : Say(player, KwSeaRigWords.PostPlace);
            int there = KwSeaRigSites.On(blueprint, post.Top, p => p == KwBowsprit.Made);
            if (there >= 0 || !aim.Within(player, post.Top))
                return there >= 0 ? Edit(player, aim, there) : Say(player, Far);
            Quaternion turn = KwShapeSites.Facing(post.Axis, aim.Ship.LocalWay(KwShapeSites.Way()));
            return KwSeaRigWindows.NewSprit(player, aim.Ship, post, turn);
        }

        private static bool Cap(Player player, KwSeaAim aim, int rig)
        {
            KwBlueprint blueprint = aim.Ship.Blueprint;
            if (rig >= 0 && KwCaps.Is(blueprint.Pieces[rig].Prefab))
                return Edit(player, aim, rig);
            KwSeaPost? post = Post(aim);
            if (post == null)
                return rig >= 0 ? Edit(player, aim, rig) : Say(player, KwSeaRigWords.PostPlace);
            int there = KwSeaRigSites.On(blueprint, post.Top, KwCaps.Is);
            if (there >= 0 || !aim.Within(player, post.Top))
                return there >= 0 ? Edit(player, aim, there) : Say(player, Far);
            KwSeaRigGhost.Clear();
            KwCapSpec spec = KwCapTool.Current(post.Width, post.Depth);
            KwSeaAsk.Send(player, aim.Ship, KwSeaRigWindows.Edit(aim.Ship, -1, NewCap(post), "post cap", spec.Words + ": post cap built"), post.Top);
            return true;
        }

        // A new cap on a post: the tool's style, length, lean and facing on the post's own section, in its keel's tier.
        private static KwBpPiece NewCap(KwSeaPost post) =>
            KwSeaPiece.Make(KwCapSpec.Made, post.Top, KwShapeSites.Facing(post.Axis, post.Axis), KwCapTool.Current(post.Width, post.Depth).ToString(), post.Tier);

        // The post's top the aim passes nearest, ahead of what it met.
        private static KwSeaPost? Post(KwSeaAim aim) =>
            KwSeaRigSites.Aimed(aim.Ship.Blueprint, aim.From, aim.Way, aim.Hit ? Vector3.Distance(aim.From, aim.Point) : 50f);

        // A rig piece's own window, if it is within reach (where the aim met it, or its foot: a sail high up is changed from the deck).
        private static bool Edit(Player player, KwSeaAim aim, int index)
        {
            Vector3 at = KwSeaRigPick.Reached(aim, index);
            return aim.Within(player, at) ? KwSeaRigWindows.Open(player, aim.Ship, index) : Say(player, Far);
        }

        private static bool Say(Player player, string text)
        {
            player.Message(MessageHud.MessageType.Center, text);
            return true;
        }
    }
}
