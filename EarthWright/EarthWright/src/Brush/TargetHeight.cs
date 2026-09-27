using EarthWright.Actions;
using EarthWright.Terrain;
using UnityEngine;

namespace EarthWright.Brush
{
    /// <summary>
    /// Works out the level target height every frame from the chosen mode and writes
    /// <see cref="BrushState.TargetHeight"/> and <see cref="BrushState.TargetSource"/>:
    /// Feet (the ground under the player, as the game does; the aimed ground while AltPlace/Shift is held), Aimed,
    /// Continued (the flat of earlier edits near the crosshair, else the crosshair), and the fixed modes. Runs on the
    /// player's machine; the height travels in the edit, so every owner levels to the same number.
    /// </summary>
    public static class TargetHeight
    {
        /// <summary>The entry levels toward a target, so the target modes and the target line apply.</summary>
        public static bool Used(ToolAction action)
        {
            if (action == null || EntryKinds.IsPath(action))
                return false;
            return action.UsesTarget || action.Height == HeightOp.Level || action.Height == HeightOp.SetMin || action.Height == HeightOp.SetMax;
        }

        public static void Refresh(Player player)
        {
            if (player == null)
                return;
            TargetSource mode = BrushState.TargetMode;
            if (TargetState.IsFixedMode(mode))
            {
                Publish(TargetState.Fixed, mode);
                return;
            }
            if (mode == TargetSource.Continued && BrushState.HasAim && ContinueFlat.TryHeight(BrushState.AimPoint, BrushState.Radius, out float flat))
            {
                Publish(flat, TargetSource.Continued);
                return;
            }
            bool aimed = mode != TargetSource.Feet || AltPlaceHeld(player);
            if (aimed && BrushState.HasAim)
                Publish(BrushState.AimPoint.y, TargetSource.Aimed);
            else
                Publish(FeetHeight(player), TargetSource.Feet);
        }

        /// <summary>Every write of the target by the brush goes through here, so others' writes can be told apart.</summary>
        public static void Publish(float height, TargetSource source)
        {
            BrushState.TargetHeight = height;
            BrushState.TargetSource = source;
            ExternalEdits.RecordTarget();
        }

        /// <summary>The ground under the player, as the game's own level ground uses it.</summary>
        public static float FeetHeight(Player player)
        {
            Vector3 feet = player.transform.position;
            return ZoneSystem.instance != null ? ZoneSystem.instance.GetGroundHeight(feet) : feet.y;
        }

        /// <summary>The game's own "place at the aimed point" button (Shift, or the gamepad's alt place), read as the game reads it.</summary>
        public static bool AltPlaceHeld(Player player)
        {
            if (ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive())
                return player.m_altPlace;
            return ZInput.GetButton("AltPlace") || (ZInput.GetButton("JoyAltPlace") && !ZInput.GetButton("JoyRotate"));
        }
    }
}
