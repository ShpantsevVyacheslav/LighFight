# Rigify contact aliases —18October2026

The new DEF-only dense skeleton's names were not recognized by NativeRigBones; animated BonePoint returnedNaN rather than authorizing synthetic contact. Added exact anatomical aliases for root/spine/head, shoulders/arms/hands, thighs/shins/feet/toes and explicit finger segments. Twist/bend subdivisions and ORG/control bones retain noncanonical names so they cannot replace the anatomical joint by traversal order. Existing Tripo/Mixamo aliases are unchanged.

Portable console reproduced RED on DEF-spine→Hips before the change and passed19cases after. Actual Unity EditMode NativeRigBonesTests passed19/19, including BonePoint resolving real synthetic transforms, subdivision collision rejection and missing-joint NaN behavior. XML/log under `.tools/autonomous-20261018/rigify-contact/`.

This makes the new naming contract available; it does not certify full dense-actor combat, model deformation, contact geometry calibration or the future gameplay height wrapper. Dense actor remains outside the shipping catalog.
