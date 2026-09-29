using UnityEngine;

namespace EliteCreaturesPack.Headsman
{
    /// <summary>
    /// The Executioner's owner's part in its attacks, by the clip's own time: in the rear strike it turns the creature
    /// itself half round as the legs come round under it (0.95 to 1.35 s), the game's AI held off meanwhile so it does
    /// not turn it back towards its target (<see cref="Turning"/>, <see cref="HeadsmanAiPatch"/>); in the ground scrape it
    /// sends the shockwave (<see cref="HeadsmanWave"/>). The turn reaches the other peers as any turn does, through the
    /// creature's synced transform.
    /// </summary>
    public sealed class HeadsmanBrain : MonoBehaviour
    {
        private ZNetView nview = null!;
        private Character character = null!;
        private Animator animator = null!;
        private HeadsmanRig rig = null!;
        private HeadsmanWave wave = null!;
        private HeadsmanMove? current;
        private float turned;

        /// <summary>Whether the rear strike is playing: the AI waits it out.</summary>
        public bool Turning => current == HeadsmanMoves.Rear;

        private void Awake()
        {
            (nview, character, rig) = (GetComponent<ZNetView>(), GetComponent<Character>(), GetComponent<HeadsmanRig>());
            animator = GetComponentInChildren<Animator>();
            wave = new HeadsmanWave(character);
            HeadsmanAiPatch.Watch(GetComponent<MonsterAI>(), () => Turning);
        }

        private void OnDestroy() => HeadsmanAiPatch.Forget(GetComponent<MonsterAI>());

        private void Update()
        {
            if (nview == null || !nview.IsValid() || !nview.IsOwner() || animator == null || !rig.enabled)
            {
                return;
            }
            var (move, time) = HeadsmanClock.Read(animator);
            if (move != current)
            {
                (current, turned) = (move, 0f);
            }
            if (move == HeadsmanMoves.Rear)
            {
                Turn(time);
            }
            wave.Step(move, time, rig.Edge);
        }

        /// <summary>By what the rear strike's own turn has grown since the last frame, and the creature looks that way.</summary>
        private void Turn(float time)
        {
            float want = HeadsmanMoves.RootYaw(time);
            transform.rotation = Quaternion.AngleAxis(want - turned, Vector3.up) * transform.rotation;
            turned = want;
            character.SetLookDir(transform.forward);
        }
    }
}
