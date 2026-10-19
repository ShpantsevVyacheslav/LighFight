using System;
using UnityEngine;
// Bounded next-window harness: caller owns warmed actual actor, lighting and EOF fences.
public static class DenseFiniteBoundsGpuProbe
{
 public static readonly double[] ExtremeTimes={0,2.625,4.5583333333333336,5.0625,5.6333333333333337,6,12.291666666666666,556.0/24};
 public static void FitFullLandscapeCanvas(DenseFiniteDanceBounds provider,Camera camera,int width,int height){
  if(width<=height||height<=0)throw new ArgumentException("Actual full landscape render canvas required");
  Bounds b;if(!provider.TryGetBounds(camera.worldToCameraMatrix,out b))throw new InvalidOperationException("Explicit actorAnchor required");
  camera.aspect=(float)width/height;camera.orthographic=true;
  // Move only within image plane, then refit once in the new camera basis.
  camera.transform.position+=camera.transform.right*b.center.x+camera.transform.up*b.center.y;
  provider.TryGetBounds(camera.worldToCameraMatrix,out b);
  camera.orthographicSize=DenseFiniteDanceBounds.FullCanvasOrthographicSize(b,camera.aspect);
 }
 public static int AssertOpaqueMaskInside(Texture2D mask,int marginPixels=2){
  // Review readback only; GPU rendering must use an isolated white actor/black background.
  var pixels=mask.GetPixels32();int count=0;
  for(int y=0;y<mask.height;y++)for(int x=0;x<mask.width;x++){var p=pixels[y*mask.width+x];if(p.r<128)continue;count++;if(x<marginPixels||y<marginPixels||x>=mask.width-marginPixels||y>=mask.height-marginPixels)throw new Exception("Actual GPU silhouette clips full landscape canvas");}
  if(count<100)throw new Exception("Empty/unqualified GPU silhouette");return count;
 }
}
