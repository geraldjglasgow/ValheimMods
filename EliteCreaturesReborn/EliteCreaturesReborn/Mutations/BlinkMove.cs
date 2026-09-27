using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// The jump itself, on the owner, through the path the game uses to put a character somewhere at once (a ladder, a
    /// trap): the transform set, then physics synced so the rigidbody goes with it. Its momentum is dropped, so it
    /// arrives standing rather than still running the way it was going; it faces its target; and its AI forgets the
    /// path it was following, which would otherwise walk it back round to where it came from. ZSyncTransform writes the
    /// new position to the ZDO; a client snaps a jump of 5 m or more and eases a shorter one, which
    /// <see cref="BlinkVeil"/> hides.
    /// </summary>
    internal static class BlinkMove
    {
        public static void Jump(Character me, Vector3 dest, Vector3 faceToward)
        {
            Transform body = me.transform;
            body.position = dest;
            Vector3 look = BlinkSpot.Flat(faceToward - dest);
            if (look != Vector3.zero)
            {
                body.rotation = Quaternion.LookRotation(look);
                me.SetLookDir(look);
            }
            Still(me);
            Physics.SyncTransforms();
            Forget(me.GetBaseAI());
        }

        private static void Still(Character me)
        {
            me.m_currentVel = Vector3.zero; // the smoothed run velocity the next physics step would otherwise re-apply
            me.m_maxAirAltitude = me.transform.position.y; // no landing thud for a fall that never happened
            Rigidbody rigidbody = me.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }
        }

        // The AI caches its path for up to 5 s while the target stays put; drop it so the next step plans from here.
        private static void Forget(BaseAI ai)
        {
            if (ai != null)
            {
                ai.m_path.Clear();
                ai.m_lastFindPathTime = -1000f;
            }
        }
    }
}
