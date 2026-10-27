using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's captain's wheel (a drawn copy of the blueprint's piece, <see cref="KwWheel"/>): its wheel turned
    /// with the ship's helm, three quarters of a turn each way at full helm, from the ship's synced rudder value, so every
    /// screen turns it alike. When its steering gear reaches the rudder (the gear on the ship's rudder names it,
    /// <see cref="KwRudderGearLook"/>) the ship's helm is at it (<see cref="TakesHelm"/>): the helm's controls on its hub,
    /// the helmsman on his own feet behind it, planted there, his hands on its handles (<see cref="KwWheelGrip"/>); E at it
    /// takes the helm and its hover is the helm's. Otherwise its hover says why it does not steer the rudder, which keeps
    /// its deck tiller. Only drawn: a dedicated server makes none (<see cref="KwShipBody.Drawn"/>), and its helm sits at
    /// the stern, where nobody takes it from.
    /// </summary>
    public sealed class KwShipWheel : MonoBehaviour, Interactable, Hoverable
    {
        private const string StandName = "stand";
        private const float Near = 0.6f;

        private Ship? ship;
        private Transform? wheel, stand;
        private ShipControlls? helm;
        private float shown = float.NaN;

        /// <summary>A ship's working copy of a wheel piece, with the place behind it where the helmsman stands.</summary>
        public static void Add(Transform holder, KwBpPiece piece)
        {
            Transform made = KwShipParts.Copy(holder, piece, "wheel");
            KwShipWheel working = made.gameObject.AddComponent<KwShipWheel>();
            working.wheel = made.Find(KwWheel.WheelName);
            Transform stand = working.stand = new GameObject(StandName) { layer = made.gameObject.layer }.transform;
            stand.SetParent(made, false);
            stand.localPosition = KwMesh.ToUnity(KwWheel.Stand);
            BoxCollider box = made.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.68f, 0f);
            box.size = new Vector3(0.56f, 1.36f, 0.62f);
            KwHoverForward.OnColliders(made);
        }

        /// <summary>
        /// The ship's helm put at the wheel the rudder's steering gear is linked to (the rudder: the blade's parent); false
        /// when it has none, and the helm goes to the rudder's tiller as before.
        /// </summary>
        public static bool TakesHelm(Transform ship, ShipControlls helm, Transform controls, GameObject? blade)
        {
            KwRudderGearLook? look = blade != null && blade.transform.parent != null ? blade.transform.parent.GetComponent<KwRudderGearLook>() : null;
            KwShipWheel? at = look != null && look.Gear.Linked ? Nearest(ship, look.transform.TransformPoint(KwMesh.ToUnity(look.Gear.Wheel))) : null;
            if (at == null || at.wheel == null || at.stand == null)
                return false;
            controls.position = at.wheel.position;
            helm.m_attachPoint.SetPositionAndRotation(at.stand.position, at.stand.rotation);
            helm.m_attachAnimation = "";
            helm.m_detachOffset = new Vector3(0f, 0.2f, 0f);
            at.helm = helm;
            KwHelmsman.OnShip(helm, new KwWheelGrip(at.wheel, at.stand));
            return true;
        }

        // The ship's wheel standing at a point.
        private static KwShipWheel? Nearest(Transform ship, Vector3 point)
        {
            foreach (KwShipWheel wheel in ship.GetComponentsInChildren<KwShipWheel>(true))
            {
                if ((wheel.transform.position - point).sqrMagnitude < Near * Near)
                    return wheel;
            }
            return null;
        }

        private void Start() => ship = GetComponentInParent<Ship>();

        // The wheel turned as the ship's helm stands (the game's own rudder value, synced from the ship's owner).
        private void Update()
        {
            if (ship == null || wheel == null)
                return;
            float value = ship.GetRudderValue();
            if (value == shown)
                return;
            shown = value;
            wheel.localRotation = Quaternion.Euler(0f, 0f, -value * KwWheel.Turn);
        }

        public bool Interact(Humanoid user, bool hold, bool alt) => helm != null && helm.Interact(user, hold, alt);

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

        public string GetHoverText()
        {
            if (helm != null)
                return helm.GetHoverText();
            string why = KwRudderGearLook.Words(transform.position);
            return Localization.instance.Localize("Captain's wheel" + (why.Length > 0 ? why : "\n<color=#888888>No rudder on this ship for it to steer</color>"));
        }

        public string GetHoverName() => helm != null ? helm.GetHoverName() : "Captain's wheel";

        public float GetHoverOffset() => 0f;
    }
}
