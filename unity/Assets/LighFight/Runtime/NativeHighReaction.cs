using System;
using System.Collections.Generic;
using UnityEngine;
using LighFight.Core;

namespace LighFight.Runtime
{
    // Optional per-prefab reaction selection. Keeps the original body-hit clip and simulation timings.
    [DisallowMultipleComponent]
    public sealed class NativeHighReaction : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip headHit;
        [SerializeField] private string bodyClipNameContains = "Hit_Reaction_to_Waist";
        [SerializeField, Range(0, 1)] private float headPhaseStart;
        [SerializeField, Range(0, 1)] private float headPhaseEnd = 1;
        public string provenance;

        private AnimatorOverrideController ownedOverride;
        private List<KeyValuePair<AnimationClip, AnimationClip>> overrides;
        private AnimationClip bodyKey, bodyValue;
        private int bodyIndex;
        private bool active;
        public bool HeadActive => active;

        public void Configure(Animator target, AnimationClip source, string sourceProvenance)
        {
            animator = target;
            headHit = source;
            provenance = sourceProvenance;
        }

        private void Initialize()
        {
            if (!animator || !animator.avatar || !animator.avatar.isHuman || !animator.avatar.isValid)
                throw new InvalidOperationException("A valid target Human Avatar is required.");
            if (!headHit || !headHit.isHumanMotion)
                throw new InvalidOperationException("Head reaction must be a post-Humanoid Human clip.");
            if (headPhaseStart < 0 || headPhaseEnd > 1 || headPhaseEnd <= headPhaseStart)
                throw new InvalidOperationException("Invalid source reaction phase window.");

            ownedOverride = new AnimatorOverrideController(animator.runtimeAnimatorController);
            overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(ownedOverride.overridesCount);
            ownedOverride.GetOverrides(overrides);
            bodyIndex = -1;
            for (int i = 0; i < overrides.Count; i++)
            {
                if (!overrides[i].Key.name.Contains(bodyClipNameContains)) continue;
                if (bodyIndex >= 0) throw new InvalidOperationException("Ambiguous body reaction clip.");
                bodyIndex = i;
                bodyKey = overrides[i].Key;
                bodyValue = overrides[i].Value ? overrides[i].Value : bodyKey;
            }
            if (bodyIndex < 0) throw new InvalidOperationException("Body reaction slot not found.");
            animator.runtimeAnimatorController = ownedOverride;
        }

        // Call from FighterView.Pose before Animator.Play/Update, so physics and rendering use the same clip.
        public void SelectFor(Fighter fighter)
        {
            if (!ownedOverride) Initialize();
            bool useHead = fighter.Stun > 0 && !fighter.LastBlocked && fighter.LastHitLevel == Level.High;
            if (useHead == active) return;
            active = useHead;
            overrides[bodyIndex] = new KeyValuePair<AnimationClip, AnimationClip>(bodyKey, active ? headHit : bodyValue);
            ownedOverride.ApplyOverrides(overrides);
        }

        public float SourcePhase(float normalizedReactionPhase)
        {
            return active ? Mathf.Lerp(headPhaseStart, headPhaseEnd, Mathf.Clamp01(normalizedReactionPhase))
                          : normalizedReactionPhase;
        }

        private void OnDestroy()
        {
            if (ownedOverride) Destroy(ownedOverride);
        }
    }
}
