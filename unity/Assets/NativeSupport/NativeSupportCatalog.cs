using UnityEngine;
public sealed class NativeSupportCatalog:ScriptableObject {
 public string character,meshPreservationHash;
 public int vertexCount,triangleCount;
 public NativeSupportOverlay.Entry[] entries;
 public AnimationClip[] carriers;
 public CandidateFootGrounding.Point[] footPoints,bodyPoints;
 public Vector3 hipBindAnchor;
 public float nativeMaxCorrection;
}
