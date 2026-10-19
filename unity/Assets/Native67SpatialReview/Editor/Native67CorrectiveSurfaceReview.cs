using System;using System.IO;using UnityEngine;
// Offline snapshot experiment. No runtime component or production asset mutation.
public static class Native67CorrectiveSurfaceReview {
 [Serializable]public class Proof {public float maxDisplacementM,sourcePoseMismatchM;public string shading="Geometric area-normal rotation applied to existing seam normal/tangent frame; approximation, not exact deformation Jacobian.";}
 static Vector3[] Read(string file,int n){var b=File.ReadAllBytes(file);if(b.Length!=n*12)throw new Exception("Corrective packet size");var f=new float[n*3];Buffer.BlockCopy(b,0,f,0,b.Length);var a=new Vector3[n];for(int i=0;i<n;i++)a[i]=new Vector3(f[i*3],f[i*3+1],f[i*3+2]);return a;}
 public static Proof Apply(Mesh mesh,Vector3[] p,Vector3[] normals,Vector4[] tangents,int[] ids,Matrix4x4 world,string sourceFile,string correctionFile,int n){var source=Read(sourceFile,n);var target=Read(correctionFile,n);var pr=new Proof();var oldNormals=new Vector3[n];var newNormals=new Vector3[n];var triangles=mesh.triangles;
 for(int v=0;v<p.Length;v++)pr.sourcePoseMismatchM=Math.Max(pr.sourcePoseMismatchM,Vector3.Distance(world.MultiplyPoint3x4(p[v]),source[ids[v]]));if(pr.sourcePoseMismatchM>1e-6f)throw new Exception("Corrective source pose mismatch");
 for(int i=0;i<n;i++){pr.maxDisplacementM=Math.Max(pr.maxDisplacementM,Vector3.Distance(source[i],target[i]));if(!float.IsFinite(target[i].x)||!float.IsFinite(target[i].y)||!float.IsFinite(target[i].z))throw new Exception("Nonfinite correction");}if(pr.maxDisplacementM>.00801f)throw new Exception("Corrective displacement cap");
 for(int j=0;j<triangles.Length;j+=3){int a=ids[triangles[j]],b=ids[triangles[j+1]],c=ids[triangles[j+2]];var old=Vector3.Cross(source[b]-source[a],source[c]-source[a]);var next=Vector3.Cross(target[b]-target[a],target[c]-target[a]);oldNormals[a]+=old;oldNormals[b]+=old;oldNormals[c]+=old;newNormals[a]+=next;newNormals[b]+=next;newNormals[c]+=next;}
 var rotations=new Quaternion[n];for(int i=0;i<n;i++)rotations[i]=(oldNormals[i].sqrMagnitude>1e-22f&&newNormals[i].sqrMagnitude>1e-22f)?Quaternion.FromToRotation(oldNormals[i],newNormals[i]):Quaternion.identity;
 var inv=world.inverse;for(int v=0;v<p.Length;v++){int id=ids[v];p[v]=inv.MultiplyPoint3x4(target[id]);var q=rotations[id];normals[v]=inv.MultiplyVector(q*world.MultiplyVector(normals[v])).normalized;var t=inv.MultiplyVector(q*world.MultiplyVector((Vector3)tangents[v]));t=(t-normals[v]*Vector3.Dot(t,normals[v])).normalized;tangents[v]=new Vector4(t.x,t.y,t.z,tangents[v].w);}return pr;
 }
}
