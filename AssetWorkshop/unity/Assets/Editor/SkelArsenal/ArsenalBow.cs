using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Workshop.SkelArsenal
{
    /// <summary>
    /// The bow skeleton's string and arrow for the showcase (the game's skeleton bow has a fixed string and shows no
    /// arrow): the string is two thin boxes from each tip to its nock point, which is the drawing hand while the hand is
    /// behind the string's rest line and the rest line's middle otherwise; while the skeleton aims (the game's bow_idle
    /// state) an arrow is nocked at that point, pointing through the bow's rest; when attack_bow begins it is loosed
    /// and flies straight on for <see cref="Flight"/> seconds. The tips and the rest come from the bow's build
    /// (assets/ecp_skel_bow/out/ecp_skel_bow_points.json, Unity axes in the bow's own frame).
    /// </summary>
    public sealed class ArsenalBow
    {
        public const float Speed = 30f, Flight = 0.6f;

        [Serializable] private sealed class Points { public float[] tip_top, tip_bottom, rest, string_rest; }
        [Serializable] private sealed class PointSet { public Points unity; }

        private readonly Transform bow, hand;
        private readonly Vector3 top, bottom, rest, stringRest;
        private readonly Transform upper, lower, arrow;
        private Vector3 flyFrom, flyDirection;
        private float flight = -1f;
        private bool nocked;

        /// <summary>The farthest the hand drew the string behind its rest line, metres; and the frame of the loose.</summary>
        public float Draw { get; private set; }
        public bool JustLoosed { get; private set; }

        public ArsenalBow(GameObject bowWeapon, Transform drawingHand, GameObject arrowPrefab, string pointsFile)
        {
            bow = bowWeapon.transform;
            hand = drawingHand;
            Points p = JsonUtility.FromJson<PointSet>(File.ReadAllText(pointsFile)).unity;
            (top, bottom, rest, stringRest) = (V(p.tip_top), V(p.tip_bottom), V(p.rest), V(p.string_rest));
            upper = StringHalf("bow_string_upper");
            lower = StringHalf("bow_string_lower");
            arrow = UnityEngine.Object.Instantiate(arrowPrefab).transform;
            arrow.name = "ecp_skel_arrow_nocked";
        }

        public IEnumerable<Renderer> Renderers =>
            new[] { upper.GetComponent<Renderer>(), lower.GetComponent<Renderer>() }.Concat(arrow.GetComponentsInChildren<Renderer>());

        /// <summary>Call after each Animator update.</summary>
        public void Update(Animator animator, float dt)
        {
            bool aiming = Is(animator, "bow_idle") && !Is(animator, "attack_bow", next: true);
            bool loosing = Is(animator, "attack_bow") || Is(animator, "attack_bow", next: true);
            Vector3 nock = Nock(aiming);
            Stretch(upper, bow.TransformPoint(top), nock);
            Stretch(lower, nock, bow.TransformPoint(bottom));
            JustLoosed = nocked && loosing;
            if (JustLoosed)
                Loose();
            nocked = aiming;
            if (aiming)
                Aim(nock);
            else
                Fly(dt);
        }

        private Vector3 Nock(bool aiming)
        {
            Vector3 middle = bow.TransformPoint(stringRest);
            Vector3 back = (middle - bow.TransformPoint(rest)).normalized;
            float draw = Vector3.Dot(hand.position - middle, back);
            if (!aiming || draw <= 0f)
                return middle;
            Draw = Mathf.Max(Draw, draw);
            return hand.position;
        }

        private void Aim(Vector3 nock)
        {
            arrow.gameObject.SetActive(true);
            arrow.SetPositionAndRotation(nock, Quaternion.LookRotation(bow.TransformPoint(rest) - nock, bow.TransformDirection(Vector3.up)));
        }

        private void Loose()
        {
            flyFrom = arrow.position;
            flyDirection = arrow.forward;
            flight = 0f;
        }

        private void Fly(float dt)
        {
            if (flight < 0f || flight > Flight)
            {
                arrow.gameObject.SetActive(false);
                return;
            }
            arrow.gameObject.SetActive(true);
            arrow.position = flyFrom + flyDirection * Speed * flight;
            flight += dt;
        }

        private static bool Is(Animator animator, string state, bool next = false) =>
            next ? animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(state)
                 : animator.GetCurrentAnimatorStateInfo(0).IsName(state) || (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(state));

        private static Transform StringHalf(string name)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            var material = new Material(Shader.Find("Standard")) { name = "bow_string", color = new Color(0.22f, 0.17f, 0.11f) };
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box.transform;
        }

        private static void Stretch(Transform half, Vector3 from, Vector3 to)
        {
            half.SetPositionAndRotation((from + to) / 2f, Quaternion.LookRotation(to - from));
            half.localScale = new Vector3(0.007f, 0.007f, (to - from).magnitude);
        }

        private static Vector3 V(float[] a) => new Vector3(a[0], a[1], a[2]);

        public static string PointsFile() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "assets", "ecp_skel_bow", "out", "ecp_skel_bow_points.json"));
    }
}
