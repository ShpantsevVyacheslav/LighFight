using System;
using UnityEngine;
// Offline/reference-only checks, callable explicitly by the future named diagnostic.
public static class CurrentSkinningBufferLayoutChecks {
 public static void Run(){
  var expected=new[]{new Vector3(.19f,-.23f,1.31f),new Vector3(-1.7f,.32f,.41f),new Vector3(.77f,1.03f,-.29f),new Vector3(-.61f,-.43f,2.17f)};
  var bytes=new byte[expected.Length*32];for(int i=0;i<bytes.Length;i+=4)Put(bytes,i,float.NaN);
  for(int i=0;i<expected.Length;i++){Put(bytes,i*32+4,expected[i].x);Put(bytes,i*32+8,expected[i].y);Put(bytes,i*32+12,expected[i].z);}
  var found=CurrentSkinningBufferReadback.PositionCandidates(bytes,expected,1e-7f);
  if(found.Count!=1||found[0].stride!=32||found[0].offset!=4)throw new Exception("Known layout not uniquely recovered");
  var wrong=(Vector3[])expected.Clone();wrong[2].x+=.125f;
  if(CurrentSkinningBufferReadback.PositionCandidates(bytes,wrong,1e-7f).Count!=0)throw new Exception("Wrong-pose layout falsely accepted");
  Array.Resize(ref bytes,20);
  if(CurrentSkinningBufferReadback.PositionCandidates(bytes,expected,1e-7f).Count!=0)throw new Exception("Truncated layout falsely accepted");
 }
 static void Put(byte[] bytes,int n,float v){Buffer.BlockCopy(BitConverter.GetBytes(v),0,bytes,n,4);}
}
