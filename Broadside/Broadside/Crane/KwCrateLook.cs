using BundlePrefabs;
using PatchGuard;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// Where a crate is drawn, on every peer, from what is already replicated. Hanging (<see cref="KwCrate.HangingFrom"/>):
    /// its sling's meeting point under the crane's hook, turned with the turret, the four sling ropes drawn from its corner
    /// loops up to the hook. Free: at its ZDO's place, falling there under gravity when that is lower (set down from a
    /// hook), moved there at once otherwise, and riding the waves when afloat. The lid opens while someone has the crate
    /// open, as a chest's does. Sends nothing; on a dedicated server it only places the crate.
    /// </summary>
    public sealed class KwCrateLook : MonoBehaviour
    {
        public const string SlingPrefix = "kwc_sling_", LidName = "lid";
        private const float Gravity = 9.81f, Slide = 6f, Turn = 240f, LidTime = 0.4f;
        /// <summary>The lid's opening turn about its hinge (the workshop's report gives its sign).</summary>
        private static readonly Vector3 LidOpen = new Vector3(-100f, 0f, 0f);

        private KwCrate? crate;
        private Transform? lid, lift;
        private readonly Transform?[] corners = new Transform?[4];
        private readonly LineRenderer?[] slings = new LineRenderer?[4];
        private Quaternion lidRest;
        private float fall, open;
        private bool screens, broken;

        private void Start() => Guard.Run("Broadside crate look", static l => l.Init(), this);

        private void Init()
        {
            crate = GetComponent<KwCrate>();
            screens = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
            lid = GameMaterials.Find(transform, LidName);
            lidRest = lid != null ? lid.localRotation : Quaternion.identity;
            lift = GameMaterials.Find(transform, KwCrate.LiftName);
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i] = GameMaterials.Find(transform, "sling_" + i);
                Transform? line = transform.Find(SlingPrefix + i);
                slings[i] = screens && line != null ? line.GetComponent<LineRenderer>() : null;
            }
        }

        // After the cranes have set their hooks this frame. A throw is reported once and stops this crate's look.
        private void LateUpdate()
        {
            if (broken || crate == null || crate.View == null || !crate.View.IsValid())
                return;
            broken = true;
            Guard.Run("Broadside crate look", static l => l.Draw(Time.deltaTime), this);
            broken = false;
        }

        private void Draw(float dt)
        {
            KwCrane? crane = crate!.HangingFrom();
            if (crane != null)
                Hang(crane);
            else
                Free(dt);
            Slings(crane);
            if (screens)
                Lid(dt);
        }

        private void Hang(KwCrane crane)
        {
            Quaternion turn = Quaternion.Euler(0f, crane.Rig!.Turret.eulerAngles.y + crate!.HangYaw, 0f);
            transform.SetPositionAndRotation(crane.Rig.Hook!.position - turn * crate.LiftLocal, turn);
            fall = 0f;
        }

        // To the ZDO's place: down under gravity, sideways and up quickly; afloat, on the waves where it floats.
        private void Free(float dt)
        {
            ZDO zdo = crate!.View!.GetZDO();
            Vector3 at = zdo.GetPosition(), now = transform.position;
            float? waves = crate.Afloat ? KwCargoCast.Water(at, 1f) : null;
            if (waves.HasValue)
                at.y = waves.Value - KwCrateRest.Draft;
            fall = now.y > at.y + 0.01f ? fall + Gravity * dt : 0f;
            float y = fall > 0f ? Mathf.Max(at.y, now.y - fall * dt) : at.y;
            Vector3 across = Vector3.MoveTowards(new Vector3(now.x, 0f, now.z), new Vector3(at.x, 0f, at.z), Slide * dt);
            Quaternion turn = Quaternion.RotateTowards(transform.rotation, zdo.GetRotation(), Turn * dt);
            if ((new Vector3(across.x, y, across.z) - now).sqrMagnitude > 1e-8f || Quaternion.Angle(turn, transform.rotation) > 0.01f)
                transform.SetPositionAndRotation(new Vector3(across.x, y, across.z), turn);
        }

        // The four ropes from the corner loops up to the hook, only while it hangs.
        private void Slings(KwCrane? crane)
        {
            for (int i = 0; i < slings.Length; i++)
            {
                LineRenderer? line = slings[i];
                if (line == null || corners[i] == null)
                    continue;
                line.enabled = crane != null;
                if (crane == null)
                    continue;
                line.positionCount = 2;
                line.SetPosition(0, corners[i]!.position);
                line.SetPosition(1, lift != null ? lift.position : crate!.LiftPoint);
            }
        }

        // The lid eases open while the crate is open (its owner's flag, or the game's in-use value elsewhere).
        private void Lid(float dt)
        {
            if (lid == null)
                return;
            bool inUse = crate!.View!.IsOwner() ? crate.Box != null && crate.Box.IsInUse() : crate.View.GetZDO().GetInt(ZDOVars.s_inUse) == 1;
            float goal = inUse ? 1f : 0f;
            if (Mathf.Approximately(open, goal))
                return;
            open = Mathf.MoveTowards(open, goal, dt / LidTime);
            lid.localRotation = lidRest * Quaternion.Euler(LidOpen * Mathf.SmoothStep(0f, 1f, open));
        }
    }
}
