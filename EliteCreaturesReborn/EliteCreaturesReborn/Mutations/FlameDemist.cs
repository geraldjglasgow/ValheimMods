using System;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Util;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Flamebound's heat burns the mist off around it: the game's own demister field, the one a Wisplight's ball
    /// carries, rides the creature at its middle, so mist parts around it as it parts around a player holding a
    /// Wisplight, and the fire never hides inside a glowing cloud. The field is copied from the Wisplight's (its reach
    /// and its push on the mist); the game's mist does the rest. The weather's fog is one value for the whole world and
    /// is left alone. Purely cosmetic, on every client (never on a dedicated server, which draws nothing); while a
    /// Cloaked or Cloning creature hides, the field goes with it, or a moving hole in the mist would give it away.
    /// </summary>
    public sealed class FlameDemist : MonoBehaviour
    {
        private const string Wisplight = "Demister";

        /// <summary>The reach, in metres, should the game's Wisplight not be found.</summary>
        private const float FallbackReach = 6f;

        private static ParticleSystemForceField? _template;

        private GameObject? _field;
        private EliteController? _controller;
        private CloakBehaviour? _cloak;
        private Action? _step;

        private void Start() => Guard.Run("FlameDemist.Start", Build);

        private void Build()
        {
            Character character = GetComponent<Character>();
            _controller = GetComponent<EliteController>();
            _cloak = GetComponent<CloakBehaviour>();
            enabled = _cloak != null || GetComponent<CloneBehaviour>() != null; // only a hider needs watching
            if (character == null || _controller == null || Machine.Headless)
            {
                enabled = false;
                return;
            }
            _field = new GameObject("ecr_flame_demist");
            _field.transform.SetParent(transform, worldPositionStays: false);
            _field.transform.position = character.GetCenterPoint();
            Dress(_field.AddComponent<ParticleSystemForceField>());
            _field.AddComponent<Demister>(); // finds the field above as it wakes
        }

        // The Wisplight's own field, copied part by part; a plain one of the fallback reach should it be missing.
        private static void Dress(ParticleSystemForceField field)
        {
            ParticleSystemForceField? game = Template();
            if (game == null)
            {
                field.endRange = FallbackReach;
                return;
            }
            field.shape = game.shape;
            field.startRange = game.startRange;
            field.endRange = game.endRange;
            field.length = game.length;
            field.gravity = game.gravity;
            field.gravityFocus = game.gravityFocus;
            field.drag = game.drag;
            field.multiplyDragByParticleSize = game.multiplyDragByParticleSize;
            field.multiplyDragByParticleVelocity = game.multiplyDragByParticleVelocity;
            Steer(field, game);
        }

        private static void Steer(ParticleSystemForceField field, ParticleSystemForceField game)
        {
            field.directionX = game.directionX;
            field.directionY = game.directionY;
            field.directionZ = game.directionZ;
            field.rotationSpeed = game.rotationSpeed;
            field.rotationAttraction = game.rotationAttraction;
            field.rotationRandomness = game.rotationRandomness;
            field.vectorField = game.vectorField;
            field.vectorFieldSpeed = game.vectorFieldSpeed;
            field.vectorFieldAttraction = game.vectorFieldAttraction;
        }

        // The Wisplight's equip effect carries its ball, and the ball carries the field.
        private static ParticleSystemForceField? Template()
        {
            if (_template != null || ObjectDB.instance == null)
            {
                return _template;
            }
            GameObject? item = ObjectDB.instance.GetItemPrefab(Wisplight);
            ItemDrop? drop = item != null ? item.GetComponent<ItemDrop>() : null;
            SE_Demister? effect = drop != null ? drop.m_itemData.m_shared.m_equipStatusEffect as SE_Demister : null;
            GameObject? ball = effect != null ? effect.m_ballPrefab : null;
            _template = ball != null ? ball.GetComponentInChildren<ParticleSystemForceField>(true) : null;
            return _template;
        }

        private void Update() => Guard.Run("FlameDemist.Update", _step ??= Step);

        private void Step()
        {
            if (_field == null || !_controller!.View.IsValid())
            {
                return;
            }
            bool show = (_cloak == null || !_cloak.Hidden) && !CloneStore.Hiding(_controller.View.GetZDO());
            if (_field.activeSelf != show)
            {
                _field.SetActive(show);
            }
        }
    }
}
