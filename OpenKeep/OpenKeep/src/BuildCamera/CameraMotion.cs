using UnityEngine;

namespace OpenKeep.BuildCamera
{
    /// <summary>
    /// Moves the camera one frame. The game's movement keys and the left stick pan it flat along where it looks; Jump
    /// and the right trigger raise it, Crouch and the left trigger lower it; Run (Shift, or the gamepad's run) speeds it
    /// up by Run Multiplier. Only while the game takes input (no inventory, chat, console, map, menu, build menu or
    /// radial menu). The move is held inside the stations' area first, then swept against the ground.
    /// </summary>
    public static class CameraMotion
    {
        public static void Step(Player player, float dt)
        {
            if (player == null || !player.TakeInput() || Hud.IsPieceSelectionVisible() || Hud.InRadial())
                return;
            Vector3 move = Direction() * Speed() * dt;
            if (move.sqrMagnitude < 1e-8f)
                return;
            Vector3 from = CameraState.Position;
            Vector3 wanted = CameraArea.Clamp(from + move);
            CameraState.Position = CameraCollision.Move(from, wanted);
        }

        private static Vector3 Direction()
        {
            Vector3 local = Vector3.zero;
            if (ZInput.GetButton("Forward"))
                local.z += 1f;
            if (ZInput.GetButton("Backward"))
                local.z -= 1f;
            if (ZInput.GetButton("Left"))
                local.x -= 1f;
            if (ZInput.GetButton("Right"))
                local.x += 1f;
            Vector2 stick = ZInput.GetJoyLeftStick();
            local.x += stick.x;
            local.z += stick.y;
            local.y = Vertical();
            return Quaternion.Euler(0f, CameraState.Yaw, 0f) * Vector3.ClampMagnitude(local, 1f);
        }

        private static float Vertical()
        {
            float up = ZInput.GetJoyRTrigger() - ZInput.GetJoyLTrigger();
            if (ZInput.GetButton("Jump"))
                up += 1f;
            if (ZInput.GetButton("Crouch"))
                up -= 1f;
            return Mathf.Clamp(up, -1f, 1f);
        }

        private static float Speed()
        {
            bool fast = ZInput.GetButton("Run") || ZInput.GetButton("JoyRun");
            return CameraPrefs.Speed.Value * (fast ? CameraPrefs.RunMultiplier.Value : 1f);
        }
    }
}
