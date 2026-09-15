namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// A snapshot of a character's movement speeds as the prefab defined them, so a live multiplier (star scaling,
    /// Mad, Devouring's slowing) can be re-derived from the base every time rather than compounding onto itself.
    /// </summary>
    public struct BaseSpeeds
    {
        private float _speed, _acceleration, _crouch, _walk, _run, _turn, _runTurn, _flySlow, _flyFast, _flyTurn;

        /// <summary>The prefab's own run speed, before any scaling - the yardstick the movement clamp measures against.</summary>
        public float RunSpeed => _run;

        public static BaseSpeeds Capture(Character c)
        {
            return new BaseSpeeds
            {
                _speed = c.m_speed, _acceleration = c.m_acceleration, _crouch = c.m_crouchSpeed,
                _walk = c.m_walkSpeed, _run = c.m_runSpeed, _turn = c.m_turnSpeed, _runTurn = c.m_runTurnSpeed,
                _flySlow = c.m_flySlowSpeed, _flyFast = c.m_flyFastSpeed, _flyTurn = c.m_flyTurnSpeed,
            };
        }

        public void ApplyTo(Character c, float factor)
        {
            c.m_speed = _speed * factor;
            c.m_acceleration = _acceleration * factor;
            c.m_crouchSpeed = _crouch * factor;
            c.m_walkSpeed = _walk * factor;
            c.m_runSpeed = _run * factor;
            c.m_turnSpeed = _turn * factor;
            c.m_runTurnSpeed = _runTurn * factor;
            c.m_flySlowSpeed = _flySlow * factor;
            c.m_flyFastSpeed = _flyFast * factor;
            c.m_flyTurnSpeed = _flyTurn * factor;
        }
    }
}
