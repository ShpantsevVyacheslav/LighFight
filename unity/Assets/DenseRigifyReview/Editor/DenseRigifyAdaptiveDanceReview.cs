using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug=UnityEngine.Debug;
public static partial class DenseRigifyReimportReview
{
 static bool adaptiveCaptureOnly;
 public static void RunAdaptiveDanceCaptureReview(){adaptiveCaptureOnly=true;RunAdaptiveDanceReview();}
 public static void RunAdaptiveDanceReview(){if(pendingReview!=null)throw new Exception("Review active");Output=BaseOutput+(adaptiveCaptureOnly?"/adaptive-dance-bakespace-review-v1":"/adaptive-dance-unity-review-v1");pendingReview=AdaptiveDanceSteps();reviewTicks=0;EditorApplication.update+=PumpReview;EditorApplication.QueuePlayerLoopUpdate();}
 sealed class BakeScope:IDisposable
 {
  public readonly Bounds bounds;readonly Mesh mesh;
  public BakeScope(SkinnedMeshRenderer skin,double phase)
  {
   mesh=new Mesh();skin.BakeMesh(mesh,false);var without=mesh.vertices;skin.BakeMesh(mesh,true);var with=mesh.vertices;
   var original=skin.sharedMesh.vertices;var weights=ReadTypedWeights(skin.sharedMesh);var bones=skin.bones;var binds=skin.sharedMesh.bindposes;
   var matrices=new Matrix4x4[bones.Length];for(int i=0;i<bones.Length;i++)matrices[i]=bones[i].localToWorldMatrix*binds[i];
   if(without.Length!=original.Length||with.Length!=original.Length)throw new Exception("BakeMesh correspondence RED");
   var full=skin.localToWorldMatrix;var rigid=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
   var maximum=new double[4];var candidateBounds=new Bounds[5];
   for(int i=0;i<original.Length;i++)
   {
    var w=weights[i];var v=original[i];var expected=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
    if(i==0)candidateBounds[4]=new Bounds(expected,Vector3.zero);else candidateBounds[4].Encapsulate(expected);
    for(int mode=0;mode<4;mode++)
    {
     var point=(mode%2==0?full:rigid).MultiplyPoint3x4(mode<2?without[i]:with[i]);
     maximum[mode]=Math.Max(maximum[mode],Vector3.Distance(point,expected));
     if(i==0)candidateBounds[mode]=new Bounds(point,Vector3.zero);else candidateBounds[mode].Encapsulate(point);
    }
   }
   int qualifiedBake=-1;for(int i=0;i<4;i++)if(maximum[i]<=.0001){qualifiedBake=i;break;}
   // Camera authority is the independently source-qualified, complete shader-space LBS surface.
   // Bake candidates remain separate diagnostics; no scale guessed from a PNG.
   int selected=4;
   string path=Output+"/actual-bake-coordinate-contract.csv";if(!File.Exists(path))File.WriteAllText(path,"phase,vertices,lossyX,lossyY,lossyZ,falseFullMaxM,falseRigidMaxM,trueFullMaxM,trueRigidMaxM,qualifiedBakeConvention,cameraConvention4FullLBS,heightM,falseFullHeightM,falseRigidHeightM,trueFullHeightM,trueRigidHeightM"+Environment.NewLine);
   var scale=skin.transform.lossyScale;File.AppendAllText(path,string.Join(",",phase,original.Length,scale.x,scale.y,scale.z,maximum[0],maximum[1],maximum[2],maximum[3],qualifiedBake,selected,candidateBounds[selected].size.y,candidateBounds[0].size.y,candidateBounds[1].size.y,candidateBounds[2].size.y,candidateBounds[3].size.y)+Environment.NewLine);
   File.AppendAllText(Output+"/renderer-space-matrices.txt","phase="+phase+" localToWorld="+full+" rigid="+rigid+" rootBoneWorld="+(skin.rootBone?skin.rootBone.localToWorldMatrix:Matrix4x4.identity)+Environment.NewLine);
   bounds=candidateBounds[selected];
  }
  public void Dispose(){UnityEngine.Object.DestroyImmediate(mesh);}
 }
 static void SnapshotDanceImporterGetters()
 {
  var m=(ModelImporter)AssetImporter.GetAtPath(DanceModel);if(!m)throw new Exception("Actual Dance importer missing");
  File.WriteAllText(Output+"/actual-dance-importer.json",EditorJsonUtility.ToJson(m,true));
  File.WriteAllLines(Output+"/actual-dance-importer-getters.txt",new[]{
   "asset="+DanceModel,"GUID="+AssetDatabase.AssetPathToGUID(DanceModel),
   "policySHA="+StreamSHA(Root+"/Editor/DenseRigifyDanceImportPolicy.cs"),
   "skinWeights="+m.skinWeights,"maxBonesPerVertex="+m.maxBonesPerVertex,"minBoneWeight="+m.minBoneWeight.ToString("R",System.Globalization.CultureInfo.InvariantCulture),
   "meshCompression="+m.meshCompression,"meshOptimizationFlags="+m.meshOptimizationFlags,"weldVertices="+m.weldVertices,"isReadable="+m.isReadable,
   "animationType="+m.animationType,"animationCompression="+m.animationCompression,"resampleCurves="+m.resampleCurves,
   "importNormals="+m.importNormals,"importTangents="+m.importTangents,"bakeAxisConversion="+m.bakeAxisConversion,
   "Actual named influence preservation is separately proven in full-dance-unity-review-v1; metadata/getters alone do not prove or disprove missing weights."
  });
 }
 static IEnumerator AdaptiveDanceSteps()
 {
  Directory.CreateDirectory(Output);SnapshotDanceImporterGetters();
  const string keysPath=Root+"/AdaptiveDance/FullAuthorSourceKeys.bytes";
  const string keysSHA="296ea14ea4c70fc41d20f1e013d43cebd635daff06b61d3f881be2cf31ac67e4";
  if(StreamSHA("/tmp/lighfight-dense-adaptive-dance.nda")!=keysSHA)throw new Exception("Original adaptive key packet hash");
  if(!File.Exists(keysPath))File.Copy("/tmp/lighfight-dense-adaptive-dance.nda",keysPath);
  if(StreamSHA(keysPath)!=keysSHA)throw new Exception("Staged adaptive key packet hash");
  AssetDatabase.ImportAsset(keysPath,ImportAssetOptions.ForceSynchronousImport);
  var keysAsset=AssetDatabase.LoadAssetAtPath<TextAsset>(keysPath);if(!keysAsset)throw new Exception("Typed key data import failed");
  var importWatch=Stopwatch.StartNew();
  var previous=SceneManager.GetActiveScene();if(previous.isDirty&&!string.IsNullOrEmpty(previous.path))throw new Exception("Unexpected dirty saved scene; no discard authorized");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,string.IsNullOrEmpty(previous.path)&&SceneManager.sceneCount==1?NewSceneMode.Single:NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  GameObject actor=null,cameraGO=null;Mesh clone=null;Material artist=null,filmic=null,encoder=null;RenderTexture hdr=null,linear=null,png=null;
  try
  {
   var c=LoadConfig();c.model=Root+"/SailorOriginalDenseDEF-BoxAdaptiveTspace-v2.fbx";var source=Source.Read(c.oracle);actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DanceModel),scene);var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();skin.updateWhenOffscreen=true;skin.forceMatrixRecalculationPerRender=true;skin.lightProbeUsage=LightProbeUsage.Off;skin.reflectionProbeUsage=ReflectionProbeUsage.Off;QualitySettings.skinWeights=SkinWeights.Unlimited;
   var report=new List<string>();int[] restoredMap;clone=RestoreWeights(source,skin,report,out restoredMap);skin.sharedMesh=clone;var mapping=Geometry(source,skin,report);WriteBindTangentAttributes(skin,Output+"/unity-bind-tangent-attributes.bin");File.WriteAllLines(Output+"/full-preservation.txt",report);CapacityCheckpoint("after-full-restoration");
   var baseMaterial=CreateOriginalMaterial(c);artist=new Material(baseMaterial){name="Explicit artist review normal0.5"};artist.SetFloat("_BumpScale",.5f);skin.sharedMaterial=artist;
   var animator=actor.GetComponentInChildren<Animator>();animator.enabled=false;
   var bridge=actor.AddComponent<DenseAdaptivePoseBridge>();bridge.sourceKeys=keysAsset;bridge.target=skin;bridge.actorAnchor=actor.transform;
   var warm=bridge.Warmup();while(warm.MoveNext())yield return warm.Current;(warm as IDisposable)?.Dispose();
   if(!bridge.IsWarm||Math.Abs(bridge.AuthoredDuration-556.0/24)>1e-12)throw new Exception("Typed warmup/full authored duration");
   double authoredDuration=bridge.AuthoredDuration;importWatch.Stop();
   int residentBeforeCancel=DenseAdaptivePoseBridge.ResidentKeyPackets;var cancelGO=new GameObject("Cancelled adaptive warmup proof");
   try{var cancelled=cancelGO.AddComponent<DenseAdaptivePoseBridge>();cancelled.sourceKeys=keysAsset;cancelled.target=skin;cancelled.actorAnchor=actor.transform;bool coldRejected=false;try{cancelled.Apply(0);}catch(InvalidOperationException){coldRejected=true;}if(!coldRejected)throw new Exception("Cold Apply was accepted");var pending=cancelled.Warmup();if(!pending.MoveNext())throw new Exception("Cancellation fixture did not reach predecode yield");cancelled.CancelWarmup();if(pending.MoveNext()||cancelled.IsWarm)throw new Exception("Cancelled packet published/leaked");(pending as IDisposable)?.Dispose();cancelled.Dispose();if(DenseAdaptivePoseBridge.ResidentKeyPackets!=residentBeforeCancel)throw new Exception("Cancelled warmup changed other owner's lease");}finally{UnityEngine.Object.DestroyImmediate(cancelGO);}
   bridge.enabled=false;if(!bridge.IsWarm||DenseAdaptivePoseBridge.ResidentKeyPackets!=residentBeforeCancel)throw new Exception("Disable released actor-owned packet");bridge.enabled=true;
   File.WriteAllText(Output+"/typed-warmup-lifecycle.txt","PASS cancelled-beforedecode publishes0, doesnotreleaseotherowner; disable retainsresident; Applycold rejects bycontract; explicitdispose/actorDestroy release checked in finally.");

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
     if(adaptiveCaptureOnly&&!keyed)continue;
     bridge.Apply(seconds);
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
    File.WriteAllLines(Output+"/unity-original-pose-gate.csv",rows);File.WriteAllText(Output+"/ORIGINAL-POSE-PASS.txt","PASS5keyed/endpoints full994452 source vertices; independent holdouts evaluated="+(adaptiveCaptureOnly?0:7848)+"; maxBoundM="+worstBound+" maxBoneDeg="+worstAngle+" worstSample="+worstSample+". Finite holdouts, no continuous guarantee.");
   }
   // Root motion is external: the bridge may write named skin bones only.
   var rootPosition=actor.transform.position;var rootRotation=actor.transform.rotation;var rootScale=actor.transform.localScale;
   var baselineWorld=new Matrix4x4[skin.bones.Length];var rootRows=new List<string>{"phase,yaw,uniformScale,maxWorldJointM,maxNativeEquivalentJointM,maxAngleDeg"};
   foreach(double phase in snapshots)
   {
    actor.transform.SetPositionAndRotation(rootPosition,rootRotation);actor.transform.localScale=rootScale;bridge.Apply(phase*authoredDuration);
    var beforeRoot=actor.transform.localToWorldMatrix;for(int i=0;i<baselineWorld.Length;i++)baselineWorld[i]=skin.bones[i].localToWorldMatrix;
    foreach(float yaw in new[]{-90f,90f,170f})foreach(float uniformScale in new[]{.7f,1f,1.8f})
    {
     actor.transform.SetPositionAndRotation(rootPosition+new Vector3(2.1f,-.3f,1.7f),Quaternion.AngleAxis(yaw,Vector3.up)*rootRotation);actor.transform.localScale=rootScale*uniformScale;
     bridge.Apply(phase*authoredDuration);var delta=actor.transform.localToWorldMatrix*beforeRoot.inverse;double joint=0,angle=0;
     for(int i=0;i<baselineWorld.Length;i++){var expected=delta*baselineWorld[i];joint=Math.Max(joint,Vector3.Distance(skin.bones[i].position,expected.GetColumn(3)));angle=Math.Max(angle,Angle(skin.bones[i].rotation,expected.rotation));}
     rootRows.Add(string.Join(",",phase,yaw,uniformScale,joint,joint/uniformScale,angle));
     if(joint/uniformScale>.0001||angle>.01)throw new Exception("External root yaw/scale transport RED");
    }
   }
   File.WriteAllLines(Output+"/root-coordinate-gate.csv",rootRows);
   actor.transform.SetPositionAndRotation(rootPosition,rootRotation);actor.transform.localScale=rootScale;
   var sampleTicks=new long[128];for(int i=0;i<8;i++)bridge.Apply(i*authoredDuration/7);
   var allocationMethod=typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public);
   Func<long> allocationCounter=allocationMethod==null?null:(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),allocationMethod);
   if(allocationCounter!=null)allocationCounter();long allocationBefore=allocationCounter==null?0:allocationCounter();
   for(int i=0;i<sampleTicks.Length;i++){long start=Stopwatch.GetTimestamp();bridge.Apply(i*authoredDuration/127);sampleTicks[i]=Stopwatch.GetTimestamp()-start;}
   long allocationDelta=allocationCounter==null?-1:allocationCounter()-allocationBefore;Array.Sort(sampleTicks);double tickMs=1000.0/Stopwatch.Frequency;
   File.WriteAllText(Output+"/warm-bridge-profile.txt","128changedposes wallms p50="+sampleTicks[64]*tickMs+" p95="+sampleTicks[121]*tickMs+" max="+sampleTicks[127]*tickMs+" managedAlloc="+allocationDelta+"; hostEditoronly, notphone or quietCPU. Explicit manualbridge noUpdate/LateUpdate, inactivezero evaluatorcalls byconstruction.");
   if(allocationDelta>0)throw new Exception("Warm bridge allocated managed bytes");
   // Exact adaptive source evaluator, with real engine ticks before each rendered pose.
   foreach(double phase in snapshots)
   {
    bridge.Apply(phase*authoredDuration);yield return null;yield return null;bridge.Apply(phase*authoredDuration);yield return null;
    var matrices=skin.bones.Select((b,i)=>b.localToWorldMatrix*bindposes[i]).ToArray();
    using(var baked=new BakeScope(skin,phase)){fullMotion=baked.bounds;}
    foreach(int side in new[]{1,-1})foreach(bool neck in new[]{false,true})
    {
     var center=fullMotion.center;if(neck){var shoulders=skin.bones.Where(b=>b.name=="DEF-shoulder.L"||b.name=="DEF-shoulder.R").ToArray();center=(shoulders[0].position+shoulders[1].position)*.5f+Vector3.up*.055f;}camera.orthographicSize=neck?fullMotion.size.y*.205f:Mathf.Max(fullMotion.extents.x,fullMotion.extents.y)*1.2f;camera.transform.position=center+Vector3.forward*(4*side);camera.transform.LookAt(center);camera.ResetProjectionMatrix();string name="ACTUAL-AdaptiveKeys-Dance-"+phase.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"-normal0.50-"+side+"-"+(neck?"neck":"fullbody");StudioPNG(camera,hdr,linear,png,filmic,encoder,Output+"/"+name+".png");if(!neck)MaskAgreement(skin,camera,hdr,vertices,weights,matrices,phase,side);
    }
   }
   File.WriteAllText(Output+"/runtime-review-budget.txt","Original2535Float64 adaptive keys bytes="+new FileInfo(keysPath).Length+" setupWallMs="+importWatch.Elapsed.TotalMilliseconds+" typedDuration="+authoredDuration+" no AnimationClip dependency on1536FPS review transport; no production promotion.");
   File.WriteAllText(Output+"/COMPLETE.txt","Actual Unity adaptive-key source/FK bridge; original poses evaluated="+(adaptiveCaptureOnly?5:7853)+"; complete source-qualified shader-LBS-framed captures complete. Native source frame only; gameplay-scale/grounding/lifecycle/player remains separate.");
   Debug.Log("AGENT_RESULT adaptive source keys Unity gate complete");

  }
  finally
  {
   if(cameraGO){cameraGO.GetComponent<Camera>().targetTexture=null;UnityEngine.Object.DestroyImmediate(cameraGO);}if(actor){var owner=actor.GetComponent<DenseAdaptivePoseBridge>();int before=DenseAdaptivePoseBridge.ResidentKeyPackets;if(owner){bool owned=owner.IsWarm;owner.Dispose();if(DenseAdaptivePoseBridge.ResidentKeyPackets!=before-(owned?1:0))throw new Exception("Explicit ownerdispose didnotrelease exactlyone packet");}UnityEngine.Object.DestroyImmediate(actor);}if(clone)UnityEngine.Object.DestroyImmediate(clone);foreach(var m in new[]{artist,filmic,encoder})if(m)UnityEngine.Object.DestroyImmediate(m);foreach(var r in new[]{hdr,linear,png})if(r){r.Release();UnityEngine.Object.DestroyImmediate(r);}if(previous.IsValid()&&previous.isLoaded){SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}else{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
  }
 }
}
