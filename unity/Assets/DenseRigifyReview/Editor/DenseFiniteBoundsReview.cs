using System;
using UnityEditor;
using UnityEngine;
// No automatic scene mutation or engine job. Pure transformed-corner gate; GPU job remains separate.
public static class DenseFiniteBoundsReview
{
 [MenuItem("LighFight/Review/Validate Finite Bounds Math")]
 public static void ValidateMath(){
  var native=new Bounds(new Vector3(-.03f,.6f,.09f),new Vector3(1.93f,1.30f,1.48f));
  foreach(float yaw in new[]{-90f,90f,170f})foreach(float scale in new[]{.7f,1f,1.8f}){
   var m=Matrix4x4.TRS(new Vector3(2.1f,-.3f,1.7f),Quaternion.Euler(0,yaw,0),Vector3.one*scale);
   var b=DenseFiniteDanceBounds.TransformCorners(native,m);
   for(int i=0;i<101;i++){float u=i/100f;var p=m.MultiplyPoint3x4(Vector3.Lerp(native.min,native.max,u));var lo=b.min-Vector3.one*1e-5f;var hi=b.max+Vector3.one*1e-5f;if(p.x<lo.x||p.y<lo.y||p.z<lo.z||p.x>hi.x||p.y>hi.y||p.z>hi.z)throw new Exception("Transformed finite bounds RED");}
   foreach(float aspect in new[]{16f/9f,19.5f/9f,4f/3f}){float size=DenseFiniteDanceBounds.FullCanvasOrthographicSize(b,aspect);if(size<b.extents.y||size*aspect<b.extents.x)throw new Exception("Landscape full canvas RED");}
  }
  Debug.Log("Finite bounds math PASS; GPU/steady Dance framing not tested by this method");
 }
}
