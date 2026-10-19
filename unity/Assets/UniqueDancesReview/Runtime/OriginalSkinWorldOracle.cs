using System;using UnityEngine;
// QA-only independent GPU-equivalent linear skinning of unchanged original vertices.
// Avoids BakeMesh coordinate/scale ambiguity. No use from production pose evaluation.
public sealed class OriginalSkinWorldOracle {
 readonly SkinnedMeshRenderer skin;readonly Vector3[] vertices;readonly BoneWeight[] weights;readonly Matrix4x4[] binds,world;readonly Transform[] bones;readonly bool[] used;
 public int VertexCount=>vertices.Length;public int RenderedVertexCount{get;private set;}
 public OriginalSkinWorldOracle(SkinnedMeshRenderer renderer){skin=renderer;var mesh=skin.sharedMesh;if(mesh.blendShapeCount!=0)throw new NotSupportedException("QA oracle requires no active mesh morphs");using(var counts=mesh.GetBonesPerVertex()){for(int i=0;i<counts.Length;i++)if(counts[i]>4)throw new NotSupportedException("QA must preserve >4 source influences, not truncate them");}vertices=mesh.vertices;weights=mesh.boneWeights;binds=mesh.bindposes;bones=skin.bones;world=new Matrix4x4[bones.Length];if(weights.Length!=vertices.Length||binds.Length!=bones.Length)throw new InvalidOperationException("Original skin data mismatch");used=new bool[vertices.Length];foreach(int i in mesh.triangles)if(!used[i]){used[i]=true;RenderedVertexCount++;}}
 public void Refresh(){for(int i=0;i<bones.Length;i++)world[i]=bones[i].localToWorldMatrix*binds[i];}
 float Y(int bone,Vector3 p){var m=world[bone];return m.m10*p.x+m.m11*p.y+m.m12*p.z+m.m13;}
 public float Minimum(out int index){Refresh();float min=float.PositiveInfinity;index=-1;for(int i=0;i<vertices.Length;i++){if(!used[i])continue;var p=vertices[i];var w=weights[i];float y=Y(w.boneIndex0,p)*w.weight0+Y(w.boneIndex1,p)*w.weight1+Y(w.boneIndex2,p)*w.weight2+Y(w.boneIndex3,p)*w.weight3;if(y<min){min=y;index=i;}}return min;}
 public Vector3 VertexWorld(int i){var p=vertices[i];var w=weights[i];return world[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+world[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+world[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+world[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;}
 public Bounds Bounds(){Refresh();bool first=true;var result=new Bounds();for(int i=0;i<vertices.Length;i++){if(!used[i])continue;var p=VertexWorld(i);if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);}return result;}
}
