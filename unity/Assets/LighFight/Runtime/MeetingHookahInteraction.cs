using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LighFight.Runtime {
 // Static seated grip -> one small hose mesh and a bounded puff emitter. No live skeleton evaluation.
 public sealed class MeetingHookahInteraction:MonoBehaviour {
  public Transform outlet;
  public Material hoseMaterial,mouthpieceMaterial,smokeMaterial;
  GameObject tube,wood,tip,puff;Mesh hose,puffMesh,cylinderShape;Vector3[] puffVertices;Color32[] puffColors;float nextPuff;
  readonly List<Material> owned=new List<Material>();Texture2D puffTexture;
  static void DestroyOwned(Object obj){if(!obj)return;if(Application.isPlaying)Destroy(obj);else DestroyImmediate(obj);}
  Material MaterialFor(Color color,float smoothness){var shader=Shader.Find("Standard");if(!shader)shader=Shader.Find("Unlit/Color");var m=new Material(shader);m.color=color;if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smoothness);owned.Add(m);return m;}
  Mesh CylinderShape(){
   const int sides=12;var v=new Vector3[sides*2+2];var triangles=new List<int>();
   for(int i=0;i<sides;i++){float angle=i*2*Mathf.PI/sides;v[i]=new Vector3(Mathf.Cos(angle)*.5f,-1,Mathf.Sin(angle)*.5f);v[i+sides]=v[i]+Vector3.up*2;int j=(i+1)%sides;triangles.AddRange(new[]{i,i+sides,j,j,i+sides,j+sides,2*sides,i,j,2*sides+1,j+sides,i+sides});}
   v[2*sides]=Vector3.down;v[2*sides+1]=Vector3.up;var mesh=new Mesh{name="Spectator mouthpiece cylinder"};mesh.vertices=v;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }
  GameObject Cylinder(string name,Material material){
   // Runtime primitives would request a stripped CapsuleCollider before immediately removing it.
   var o=new GameObject(name);o.transform.SetParent(transform,false);if(!cylinderShape)cylinderShape=CylinderShape();o.AddComponent<MeshFilter>().sharedMesh=cylinderShape;var r=o.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;return o;
  }
  static void PlaceCylinder(Transform part,Vector3 a,Vector3 b,float diameter){part.position=(a+b)*.5f;part.rotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);part.localScale=new Vector3(diameter,Vector3.Distance(a,b)*.5f,diameter);}
  public void SetUser(SeatedSpectatorPose user){
   if(!user||!user.holdingMouthpiece||!user.mouthpieceGrip||!user.mouth||!outlet){if(tube)tube.SetActive(false);if(wood)wood.SetActive(false);if(tip)tip.SetActive(false);if(puff)puff.SetActive(false);return;}
   if(!hoseMaterial)hoseMaterial=MaterialFor(new Color(.025f,.025f,.028f),.25f);
   if(!mouthpieceMaterial)mouthpieceMaterial=MaterialFor(new Color(.44f,.27f,.12f),.35f);
   if(!wood)wood=Cylinder("Wood mouthpiece",mouthpieceMaterial);if(!tip)tip=Cylinder("Metal mouthpiece tip",MaterialFor(new Color(.65f,.67f,.69f),.85f));wood.SetActive(true);tip.SetActive(true);
   var mouth=user.mouth.position;var direction=(user.mouthpieceGrip.position-mouth).normalized;if(direction.sqrMagnitude<.5f)direction=user.transform.right;
   var metalEnd=mouth+direction*.028f;var back=mouth+direction*.21f;PlaceCylinder(tip.transform,mouth,metalEnd,.012f);PlaceCylinder(wood.transform,metalEnd,back,.016f);
   if(!tube){tube=new GameObject("Interaction hose");tube.transform.SetParent(transform,false);tube.AddComponent<MeshFilter>();var r=tube.AddComponent<MeshRenderer>();r.sharedMaterial=hoseMaterial;r.shadowCastingMode=ShadowCastingMode.Off;}
   tube.SetActive(true);if(hose)DestroyOwned(hose);hose=BuildHose(transform.InverseTransformPoint(outlet.position),transform.InverseTransformPoint(back));tube.GetComponent<MeshFilter>().sharedMesh=hose;
   if(!puff)CreatePuff();puff.SetActive(true);puff.transform.position=mouth;puff.transform.rotation=Quaternion.identity;nextPuff=0;
  }
  Mesh BuildHose(Vector3 a,Vector3 b){
   const int segments=24,sides=8;var points=new Vector3[(segments+1)*sides];var uv=new Vector2[points.Length];var indices=new int[segments*sides*6];var control=(a+b)*.5f+Vector3.down*.15f;
   for(int i=0;i<=segments;i++){float t=(float)i/segments;var p=(1-t)*(1-t)*a+2*(1-t)*t*control+t*t*b;var tangent=(2*(1-t)*(control-a)+2*t*(b-control)).normalized;var axis=Mathf.Abs(Vector3.Dot(tangent,Vector3.up))>.9f?Vector3.right:Vector3.up;var u=Vector3.Cross(tangent,axis).normalized;var v=Vector3.Cross(tangent,u).normalized;
    for(int j=0;j<sides;j++){float angle=j*2*Mathf.PI/sides;points[i*sides+j]=p+(Mathf.Cos(angle)*u+Mathf.Sin(angle)*v)*.006f;uv[i*sides+j]=new Vector2((float)j/sides,t);if(i==segments)continue;int n=(i*sides+j)*6,k=i*sides+j,next=i*sides+(j+1)%sides;indices[n]=k;indices[n+1]=next;indices[n+2]=k+sides;indices[n+3]=next;indices[n+4]=next+sides;indices[n+5]=k+sides;}
   }
   var mesh=new Mesh{name="Seated hookah hose"};mesh.vertices=points;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }
  void CreatePuff(){
   puff=new GameObject("Small spectator puff");puff.transform.SetParent(transform,false);puff.SetActive(false);
   puffTexture=new Texture2D(32,32,TextureFormat.RGBA32,false);var pixels=new Color[32*32];for(int y=0;y<32;y++)for(int x=0;x<32;x++){float radius=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;pixels[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-radius),2));}puffTexture.SetPixels(pixels);puffTexture.Apply(false,true);
   var shader=Shader.Find("Sprites/Default");if(!shader)shader=Shader.Find("Unlit/Transparent");var material=smokeMaterial?new Material(smokeMaterial):new Material(shader);material.mainTexture=puffTexture;material.renderQueue=3000;owned.Add(material);
   puffVertices=new Vector3[16];puffColors=new Color32[16];var uv=new Vector2[16];var indices=new int[24];for(int i=0;i<4;i++){int n=i*4;uv[n]=Vector2.zero;uv[n+1]=Vector2.right;uv[n+2]=Vector2.one;uv[n+3]=Vector2.up;int k=i*6;indices[k]=n;indices[k+1]=n+2;indices[k+2]=n+1;indices[k+3]=n;indices[k+4]=n+3;indices[k+5]=n+2;}
   puffMesh=new Mesh{name="Four small spectator puffs"};puffMesh.MarkDynamic();puffMesh.vertices=puffVertices;puffMesh.uv=uv;puffMesh.colors32=puffColors;puffMesh.triangles=indices;puffMesh.bounds=new Bounds(new Vector3(0,.15f,0),Vector3.one*.5f);puff.AddComponent<MeshFilter>().sharedMesh=puffMesh;var renderer=puff.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
  }
  void Update(){
   if(!puff||!puff.activeInHierarchy||Time.unscaledTime<nextPuff)return;nextPuff=Time.unscaledTime+.083f;SampleSmoke(Time.time,Camera.main);
  }
  public void SampleSmoke(float seconds,Camera camera){
   if(!puff||!puff.activeInHierarchy)return;if(camera)puff.transform.rotation=camera.transform.rotation;
   for(int i=0;i<4;i++){float t=Mathf.Repeat(seconds*.48f+i*.25f,1);var center=new Vector3(Mathf.Sin(t*5+i)*.018f,t*.18f,0);float size=Mathf.Lerp(.025f,.065f,t);byte alpha=(byte)(Mathf.Sin(t*Mathf.PI)*30);int n=i*4;puffVertices[n]=center+new Vector3(-size,-size,0);puffVertices[n+1]=center+new Vector3(size,-size,0);puffVertices[n+2]=center+new Vector3(size,size,0);puffVertices[n+3]=center+new Vector3(-size,size,0);for(int j=0;j<4;j++)puffColors[n+j]=new Color32(225,228,232,alpha);}
   puffMesh.vertices=puffVertices;puffMesh.colors32=puffColors;
  }
  void OnDisable(){SetUser(null);}
  void OnDestroy(){if(cylinderShape)DestroyOwned(cylinderShape);if(hose)DestroyOwned(hose);if(puffMesh)DestroyOwned(puffMesh);if(puffTexture)DestroyOwned(puffTexture);foreach(var material in owned)if(material)DestroyOwned(material);}
 }
}
