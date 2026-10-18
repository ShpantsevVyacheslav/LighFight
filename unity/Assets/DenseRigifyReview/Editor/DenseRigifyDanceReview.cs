using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug=UnityEngine.Debug;

public static partial class DenseRigifyReimportReview
{
 const string DanceModel=Root+"/SailorOriginalDenseDEF-DanceAdaptiveTspace-v1.fbx";
 const string DanceSHA="ea4a6c4f99658cb583a7ee9846fd9a5e401784afdf5c39c9a2a5debab37efa85";
 public static void RunFullDanceReview(){if(pendingReview!=null)throw new Exception("Review active");Output=BaseOutput+"/full-dance-unity-review-v1";pendingReview=FullDanceSteps();reviewTicks=0;EditorApplication.update+=PumpReview;EditorApplication.QueuePlayerLoopUpdate();}
 static string StreamSHA(string path){using(var s=File.OpenRead(path))using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant();}
 static void CapacityCheckpoint(string stage)
 {
  using(var p=Process.GetCurrentProcess()){p.Refresh();long available=new DriveInfo(Path.GetPathRoot(Path.GetFullPath("."))).AvailableFreeSpace;File.AppendAllText(Output+"/capacity.csv",stage+","+p.WorkingSet64+","+p.PrivateMemorySize64+","+available+","+EditorApplication.timeSinceStartup+Environment.NewLine);if(available<2L*1024*1024*1024||p.WorkingSet64>10L*1024*1024*1024)throw new Exception("Review capacity guard: preserve source and exit clean, no acceptance");}
 }
 sealed class DanceOracle : IDisposable
 {
  public BinaryReader reader;public int bones,count,keyed;public double duration,maxWeightSum;public string[] names;public double[] corners,meshWorld,bind;public bool[] active;
  public DanceOracle(string file)
  {
   reader=new BinaryReader(File.OpenRead(file));if(reader.ReadUInt32()!=0x31434e44)throw new Exception("DNC1 oracle");bones=reader.ReadInt32();count=reader.ReadInt32();keyed=reader.ReadInt32();duration=reader.ReadDouble();maxWeightSum=reader.ReadDouble();names=new string[bones];for(int i=0;i<bones;i++)names[i]=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));corners=ReadD(reader,bones*8*4);active=new bool[bones];for(int i=0;i<bones;i++)active[i]=reader.ReadByte()!=0;meshWorld=ReadD(reader,16);bind=ReadD(reader,bones*16);
  }
  public void Dispose(){reader.Dispose();}
 }
 static double[] ReadD(BinaryReader reader,int n){var a=new double[n];for(int i=0;i<n;i++)a[i]=reader.ReadDouble();return a;}
 static double[] D(Matrix4x4 m){var a=new double[16];for(int r=0;r<4;r++)for(int c=0;c<4;c++)a[r*4+c]=m[r,c];return a;}
 static void MulD(double[] a,int ai,double[] b,int bi,double[] output,int oi){for(int r=0;r<4;r++)for(int c=0;c<4;c++){double v=0;for(int k=0;k<4;k++)v+=a[ai+r*4+k]*b[bi+k*4+c];output[oi+r*4+c]=v;}}
 static Matrix4x4 F(double[] a,int offset){var m=new Matrix4x4();for(int r=0;r<4;r++)for(int c=0;c<4;c++)m[r,c]=(float)a[offset+r*4+c];return m;}
 static double Component(double[] a,int offset,int row,double x,double y,double z){int k=offset+row*4;return a[k]*x+a[k+1]*y+a[k+2]*z+a[k+3];}
 static void CWorld(double[] input,int offset,double[] output,int target){for(int c=0;c<4;c++){output[target+c]=-input[offset+c];output[target+4+c]=input[offset+8+c];output[target+8+c]=-input[offset+4+c];output[target+12+c]=input[offset+12+c];}}
 static IEnumerator FullDanceSteps()
 {
  Directory.CreateDirectory(Output);File.WriteAllText(Output+"/WAITING-FOR-STAGED-MODEL.txt","First-import policy compiled. Stage only verified SHA file atomically now.");
  double deadline=EditorApplication.timeSinceStartup+180;while(!File.Exists(DanceModel)){if(EditorApplication.timeSinceStartup>deadline)throw new Exception("No staged Dance source; no import");yield return null;}
  if(StreamSHA(DanceModel)!=DanceSHA)throw new Exception("Unqualified Dance FBX hash");
  CapacityCheckpoint("before-import");var importWatch=Stopwatch.StartNew();AssetDatabase.ImportAsset(DanceModel,ImportAssetOptions.ForceSynchronousImport);importWatch.Stop();CapacityCheckpoint("after-import");
  var importer=(ModelImporter)AssetImporter.GetAtPath(DanceModel);
  if(importer.animationCompression!=ModelImporterAnimationCompression.Off||importer.importTangents!=ModelImporterTangents.Import||importer.meshCompression!=ModelImporterMeshCompression.Off||importer.meshOptimizationFlags!=0||importer.maxBonesPerVertex!=4)throw new Exception("First-import source preservation policy not applied");
  var clip=AssetDatabase.LoadAllAssetsAtPath(DanceModel).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).Single();
  if(clip.isLooping||Math.Abs(clip.length-556.0/24)>.00001)throw new Exception("Full authored duration/nonloop endpoint contract RED");
  var previous=SceneManager.GetActiveScene();if(previous.isDirty&&!string.IsNullOrEmpty(previous.path))throw new Exception("Unexpected dirty saved scene; no discard authorized");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,string.IsNullOrEmpty(previous.path)&&SceneManager.sceneCount==1?NewSceneMode.Single:NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  GameObject actor=null,cameraGO=null;Mesh clone=null;Material artist=null,filmic=null,encoder=null;RenderTexture hdr=null,linear=null,png=null;
  try
  {
   var c=LoadConfig();c.model=DanceModel;var source=Source.Read(c.oracle);actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DanceModel),scene);var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();skin.updateWhenOffscreen=true;skin.forceMatrixRecalculationPerRender=true;skin.lightProbeUsage=LightProbeUsage.Off;skin.reflectionProbeUsage=ReflectionProbeUsage.Off;QualitySettings.skinWeights=SkinWeights.Unlimited;
   var report=new List<string>();int[] restoredMap;clone=RestoreWeights(source,skin,report,out restoredMap);skin.sharedMesh=clone;var mapping=Geometry(source,skin,report);WriteBindTangentAttributes(skin,Output+"/unity-bind-tangent-attributes.bin");File.WriteAllLines(Output+"/full-preservation.txt",report);CapacityCheckpoint("after-full-restoration");
   var baseMaterial=CreateOriginalMaterial(c);artist=new Material(baseMaterial){name="Explicit artist review normal0.5"};artist.SetFloat("_BumpScale",.5f);skin.sharedMaterial=artist;
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"/FullDanceReview.controller");if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(Root+"/FullDanceReview.controller");var sm=controller.layers[0].stateMachine;var state=sm.states.FirstOrDefault(s=>s.state.name=="Dance").state;if(!state)state=sm.AddState("Dance");state.motion=clip;sm.defaultState=state;EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
   var animator=actor.GetComponentInChildren<Animator>();animator.runtimeAnimatorController=controller;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;animator.speed=0;animator.Rebind();
   var lighting=Resources.Load<LighFight.Runtime.SelectionStudioLightingConfig>("SelectionStudio/PreviewLighting");if(!lighting||!lighting.IsValid)throw new Exception("Qualified persistent studio unavailable");RenderSettings.ambientMode=AmbientMode.Custom;RenderSettings.ambientProbe=lighting.DiffuseProbe;RenderSettings.ambientIntensity=1;RenderSettings.customReflectionTexture=lighting.prefilteredReflection;RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.reflectionIntensity=1;RenderSettings.skybox=null;RenderSettings.sun=null;
   cameraGO=new GameObject("Actual Generic fullDance qualified studio camera");var camera=cameraGO.AddComponent<Camera>();camera.scene=scene;camera.enabled=false;camera.allowHDR=true;camera.orthographic=true;camera.aspect=1;camera.nearClipPlane=.01f;camera.farClipPlane=30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.18f,.21f);
   hdr=new RenderTexture(1024,1024,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);linear=new RenderTexture(1024,1024,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);png=new RenderTexture(1024,1024,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);hdr.Create();linear.Create();png.Create();camera.targetTexture=hdr;
   filmic=new Material(Resources.Load<Shader>("Shaders/FittedFilmic"));filmic.SetFloat("_Exposure",1);encoder=new Material(Shader.Find("DenseRigifyReview/ExplicitSRGBEncode"));ShaderUtil.CompilePass(artist,0,true);ShaderUtil.CompilePass(filmic,0,true);ShaderUtil.CompilePass(encoder,0,true);yield return null;yield return null;TransferSentinel(filmic,encoder);
   var vertices=clone.vertices;var weights=ReadTypedWeights(clone);var bindposes=clone.bindposes;var fullMotion=new Bounds();bool hasBounds=false;var snapshots=new List<double>();
   using(var oracle=new DanceOracle("/tmp/lighfight-dense-dance-original.dnc"))
   {
    if(oracle.bones!=136||oracle.count!=7853||oracle.keyed!=5)throw new Exception("Incomplete original source oracle");
    int[] cacheForSource=source.names.Select(n=>Array.IndexOf(oracle.names,n)).ToArray();int[] skinForCache=oracle.names.Select(n=>Array.FindIndex(skin.bones,b=>b.name==n)).ToArray();if(cacheForSource.Any(i=>i<0)||skinForCache.Any(i=>i<0))throw new Exception("Named bone correspondence lost");
    var transport=skin.transform.worldToLocalMatrix*BasisMatrix*source.meshWorld;var transportD=D(transport);var bindRaw=new double[oracle.bones*16];
    for(int b=0;b<oracle.bones;b++)MulD(D(bindposes[skinForCache[b]]),0,transportD,0,bindRaw,b*16);
    double deltaX=0,deltaY=0,deltaZ=0,maxWeightDelta=0;
    for(int i=0;i<mapping.Length;i++)
    {
     var v=vertices[mapping[i]];double x=source.positions[i*3],y=source.positions[i*3+1],z=source.positions[i*3+2];deltaX=Math.Max(deltaX,Math.Abs(v.x-Component(transportD,0,0,x,y,z)));deltaY=Math.Max(deltaY,Math.Abs(v.y-Component(transportD,0,1,x,y,z)));deltaZ=Math.Max(deltaZ,Math.Abs(v.z-Component(transportD,0,2,x,y,z)));
     var w=weights[mapping[i]];int[] bs={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] ws={w.weight0,w.weight1,w.weight2,w.weight3};double delta=0;for(int k=0;k<4;k++){int original=source.joints[i*4+k];if(source.weights[i*4+k]<=0)continue;int slot=Array.FindIndex(bs,b=>skin.bones[b].name==source.names[original]);if(slot<0)throw new Exception("Missing original typed influence");delta+=Math.Abs(ws[slot]-source.weights[i*4+k]);}maxWeightDelta=Math.Max(maxWeightDelta,delta);
    }
    File.WriteAllText(Output+"/import-numerical-transport.txt","All-source mapped vertex local deltaXYZ="+deltaX+","+deltaY+","+deltaZ+"; max named sumAbsWeightDelta="+maxWeightDelta+"; no influence deletion. Conservative perpose position/weight/float roundoff added to source interval-corner bound.");
    var pose= new double[oracle.bones*16];var truthSkin=new double[oracle.bones*16];var expectedRaw=new double[oracle.bones*16];var actualRaw=new double[oracle.bones*16];var actualWorld=new double[16];
    var rows=new List<string>{"sample,phase,seconds,keyed,sourceCornerBoundM,importWeightPositionRoundoffBoundM,totalBoundM,boneJointMaxM,boneAngleMaxDeg,keyedFullSurfaceMaxM,keyedFullSurfaceRmsM"};double worstBound=0,worstAngle=0;int worstSample=-1;
    for(int sample=0;sample<oracle.count;sample++)
    {
     double phase=oracle.reader.ReadDouble(),seconds=oracle.reader.ReadDouble();bool keyed=oracle.reader.ReadByte()!=0;for(int i=0;i<pose.Length;i++)pose[i]=oracle.reader.ReadDouble();for(int i=0;i<truthSkin.Length;i++)truthSkin[i]=oracle.reader.ReadDouble();
     float normalized=keyed?(float)phase:(float)(seconds/clip.length);animator.Play(Animator.StringToHash("Dance"),0,normalized);animator.Update(0);
     double bound=0,worldMagnitude=0,positionBound=0,joint=0,angle=0;
     for(int b=0;b<oracle.bones;b++)
     {
      var bone=skin.bones[skinForCache[b]];var world=bone.localToWorldMatrix;for(int r=0;r<4;r++)for(int q=0;q<4;q++)actualWorld[r*4+q]=world[r,q];MulD(actualWorld,0,bindRaw,b*16,actualRaw,b*16);CWorld(truthSkin,b*16,expectedRaw,b*16);
      int sourceBone=Array.IndexOf(source.names,oracle.names[b]);var expected=BasisMatrix*F(pose,b*16)*BasisMatrix.inverse*reviewBoneConventions[sourceBone];joint=Math.Max(joint,Vector3.Distance(bone.position,expected.GetColumn(3)));angle=Math.Max(angle,Angle(bone.rotation,expected.rotation));
      if(!oracle.active[b])continue;
      var skinMatrix=world*bindposes[skinForCache[b]];double px=Math.Abs(skinMatrix.m00)*deltaX+Math.Abs(skinMatrix.m01)*deltaY+Math.Abs(skinMatrix.m02)*deltaZ,py=Math.Abs(skinMatrix.m10)*deltaX+Math.Abs(skinMatrix.m11)*deltaY+Math.Abs(skinMatrix.m12)*deltaZ,pz=Math.Abs(skinMatrix.m20)*deltaX+Math.Abs(skinMatrix.m21)*deltaY+Math.Abs(skinMatrix.m22)*deltaZ;positionBound=Math.Max(positionBound,Math.Sqrt(px*px+py*py+pz*pz));
      for(int corner=0;corner<8;corner++)
      {
       int ci=(b*8+corner)*4;double x=oracle.corners[ci],y=oracle.corners[ci+1],z=oracle.corners[ci+2];double dx=0,dy=0,dz=0,ax=0,ay=0,az=0;
       ax=Component(actualRaw,b*16,0,x,y,z);ay=Component(actualRaw,b*16,1,x,y,z);az=Component(actualRaw,b*16,2,x,y,z);dx=ax-Component(expectedRaw,b*16,0,x,y,z);dy=ay-Component(expectedRaw,b*16,1,x,y,z);dz=az-Component(expectedRaw,b*16,2,x,y,z);bound=Math.Max(bound,Math.Sqrt(dx*dx+dy*dy+dz*dz));worldMagnitude=Math.Max(worldMagnitude,Math.Sqrt(ax*ax+ay*ay+az*az));var v=new Vector3((float)ax,(float)ay,(float)az);if(!hasBounds){fullMotion=new Bounds(v,Vector3.zero);hasBounds=true;}else fullMotion.Encapsulate(v);
      }
     }
     bound*=oracle.maxWeightSum;double numeric=positionBound*oracle.maxWeightSum+worldMagnitude*maxWeightDelta+worldMagnitude*((64.0/16777216)/(1-64.0/16777216));double total=bound+numeric;
     double surface=0,rms=0;
     if(keyed)
     {
      var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*bindposes[i]).ToArray();double sum=0;
      for(int i=0;i<mapping.Length;i++)
      {
       int j=mapping[i];var w=weights[j];var v=vertices[j];var a=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;double x=source.positions[i*3],y=source.positions[i*3+1],z=source.positions[i*3+2],ex=0,ey=0,ez=0;
       for(int k=0;k<4;k++){double weight=source.weights[i*4+k];if(weight<=0)continue;int b=cacheForSource[source.joints[i*4+k]];ex+=Component(expectedRaw,b*16,0,x,y,z)*weight;ey+=Component(expectedRaw,b*16,1,x,y,z)*weight;ez+=Component(expectedRaw,b*16,2,x,y,z)*weight;}
       double error=(a.x-ex)*(a.x-ex)+(a.y-ey)*(a.y-ey)+(a.z-ez)*(a.z-ez);surface=Math.Max(surface,Math.Sqrt(error));sum+=error;
      }
      rms=Math.Sqrt(sum/mapping.Length);snapshots.Add(phase);
     }
     rows.Add(string.Join(",",sample,phase,seconds,keyed,bound,numeric,total,joint,angle,surface,rms));if(total>worstBound){worstBound=total;worstSample=sample;}worstAngle=Math.Max(worstAngle,angle);
     if(total>.0001||angle>.01||surface>.0001){File.WriteAllLines(Output+"/unity-original-pose-gate.csv",rows);throw new Exception("FullDance source gate RED sample="+sample+" phase="+phase+" totalM="+total+" boneDeg="+angle+" fullSurfaceM="+surface);}
     if(sample%64==0){File.WriteAllLines(Output+"/unity-original-pose-gate.csv",rows);CapacityCheckpoint("pose-"+sample);yield return null;}
    }
    File.WriteAllLines(Output+"/unity-original-pose-gate.csv",rows);File.WriteAllText(Output+"/ORIGINAL-POSE-PASS.txt","PASS5keyed/endpoints full994452 source vertices +7848 independent original-source holdouts; maxBoundM="+worstBound+" maxBoneDeg="+worstAngle+" worstSample="+worstSample+". Finite holdouts, no continuous guarantee.");
   }
   // Actual Generic Animator playback, not frozen manual bone matrices. Source endpoints and
   // all independent holdouts qualified above. Real engine ticks fence each rendered pose.
   foreach(double phase in snapshots)
   {
    animator.Play(Animator.StringToHash("Dance"),0,(float)phase);animator.Update(0);yield return null;yield return null;animator.Play(Animator.StringToHash("Dance"),0,(float)phase);animator.Update(0);yield return null;
    var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*bindposes[i]).ToArray();
    foreach(int side in new[]{1,-1})foreach(bool neck in new[]{false,true})
    {
     var center=fullMotion.center;if(neck){var shoulders=skin.bones.Where(b=>b.name=="DEF-shoulder.L"||b.name=="DEF-shoulder.R").ToArray();center=(shoulders[0].position+shoulders[1].position)*.5f+Vector3.up*.055f;}camera.orthographicSize=neck?1.96f*.205f:Mathf.Max(fullMotion.extents.x,fullMotion.extents.y)*1.2f;camera.transform.position=center+Vector3.forward*(4*side);camera.transform.LookAt(center);camera.ResetProjectionMatrix();string name="ACTUAL-Generic-Dance-"+phase.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"-normal0.50-"+side+"-"+(neck?"neck":"fullbody");StudioPNG(camera,hdr,linear,png,filmic,encoder,Output+"/"+name+".png");if(!neck)MaskAgreement(skin,camera,hdr,vertices,weights,matrices,phase,side);
    }
   }
   CapacityCheckpoint("before-clip-stats");long keys=0;var bindings=AnimationUtility.GetCurveBindings(clip);for(int i=0;i<bindings.Length;i++){var curve=AnimationUtility.GetEditorCurve(clip,bindings[i]);if(curve!=null)keys+=curve.length;if(i%32==0){GC.Collect();GC.WaitForPendingFinalizers();CapacityCheckpoint("curve-"+i);yield return null;}}
   var budget=new List<string>{"Review FBX445878204bytes; derived1536FPS uniform transport, NOT shipping payload.","clip="+clip.name+" duration="+clip.length+" looping="+clip.isLooping+" runtimeBytes="+Profiler.GetRuntimeMemorySizeLong(clip)+" bindings="+bindings.Length+" keys="+keys,"restoredMeshBytes="+Profiler.GetRuntimeMemorySizeLong(clone)+" vertices="+clone.vertexCount+" triangles="+clone.triangles.Length/3,"sourceImporterMeshBytes="+Profiler.GetRuntimeMemorySizeLong(AssetDatabase.LoadAssetAtPath<GameObject>(DanceModel).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh)+" (instance bound to restored mesh; FBX dependency remains review-only controller, not standalone prefab)","importWallMs="+importWatch.Elapsed.TotalMilliseconds};foreach(string path in new[]{c.baseColor,c.normal,c.roughness,c.metallic,AssetDatabase.GetAssetPath(baseMaterial.GetTexture("_MetallicGlossMap"))}){var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);budget.Add(path+" "+t.width+"x"+t.height+" runtimeBytes="+Profiler.GetRuntimeMemorySizeLong(t));}File.WriteAllLines(Output+"/runtime-review-budget.txt",budget);
   File.WriteAllText(Output+"/COMPLETE.txt","Full source preservation +5endpoint/keyed fullsurface +7848 original offkeys +actual Generic Animator studio captures complete. Review transport only; no production catalog/mobile payload/model skin-quality acceptance. Artist normal0.5 explicit, original source1 retained. Raw source-floor not silently offset; actual FighterView/grounding/lifecycle/player remains next.");Debug.Log("AGENT_RESULT fullDance Unity import/source pose/capture review complete; no catalog promotion");
  }
  finally
  {
   if(cameraGO){cameraGO.GetComponent<Camera>().targetTexture=null;UnityEngine.Object.DestroyImmediate(cameraGO);}if(actor)UnityEngine.Object.DestroyImmediate(actor);if(clone)UnityEngine.Object.DestroyImmediate(clone);foreach(var m in new[]{artist,filmic,encoder})if(m)UnityEngine.Object.DestroyImmediate(m);foreach(var r in new[]{hdr,linear,png})if(r){r.Release();UnityEngine.Object.DestroyImmediate(r);}if(previous.IsValid()&&previous.isLoaded){SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}else{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
  }
 }
}
