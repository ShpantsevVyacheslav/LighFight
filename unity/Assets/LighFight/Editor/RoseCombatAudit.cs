using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LighFight.Core;
using LighFight.Runtime;
namespace LighFight.Editor
{
    public static class RoseCombatAudit
    {
        static void CaptureBattle()
        {
            var prototype=UnityEngine.Object.FindFirstObjectByType<Prototype>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(Prototype).GetMethod("Start",flags).Invoke(prototype,null);
            var session=(FightSession)typeof(Prototype).GetField("session",flags).GetValue(prototype);
            var move=Moves.All.Single(m=>m.Id=="frontKick");session.Match.A.Attack=move;session.Match.A.Frame=move.Startup;
            var player=(FighterView)typeof(Prototype).GetField("player",flags).GetValue(prototype);player.Pose(session.Match.A,1,0);player.GetComponent<Animator>().Update(0);
            foreach(var view in UnityEngine.Object.FindObjectsByType<FighterView>(FindObjectsSortMode.None))
            {
                var animator=view.GetComponent<Animator>();if(animator==null)continue;animator.Update(0);view.Pose(view==player?session.Match.A:new Fighter{X=session.Match.B.X,Guard=true},view==player?1:-1,0);animator.Update(0);view.GetComponent<JointCorrectives>()?.Apply();
                var skin=view.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();skin.BakeMesh(baked);var display=new GameObject("Frozen combat pose");display.transform.SetParent(skin.transform,false);display.AddComponent<MeshFilter>().sharedMesh=baked;display.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
            }
            var camera=UnityEngine.Object.FindFirstObjectByType<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;
            var rt=new RenderTexture(1200,700,24);camera.targetTexture=rt;camera.transform.position=new Vector3(0,1.6f,-Prototype.FitCameraDistance(session.Match.A.X,session.Match.B.X,1200f/700));camera.transform.LookAt(new Vector3(0,1,0));camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1200,700);image.ReadPixels(new Rect(0,0,1200,700),0,0);image.Apply();File.WriteAllBytes("../art/previews/rose-motion/battle.png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
            EditorSceneManager.OpenScene("Assets/LighFight/Scenes/HostFightReview.unity");
        }
        public static void Run()
        {
            VerifyCharacter(4);VerifyCharacter(3);HostFightReviewSetup.Create();CaptureBattle();
        }
        static void VerifyCharacter(int fighterId)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-"+fighterId+".prefab");
            var obj=UnityEngine.Object.Instantiate(prefab);var view=obj.GetComponent<FighterView>();var animator=obj.GetComponent<Animator>();var skin=obj.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();int frames=0;
            var handBones=new[]{"LeftHand","RightHand"}.Select(name=>Array.FindIndex(skin.bones,b=>b.name==name)).ToArray();
            var weights=skin.sharedMesh.boneWeights;
            var handVertices=handBones.Select(index=>Enumerable.Range(0,weights.Length).Where(i=>weights[i].boneIndex0==index && weights[i].weight0>.99f).ToArray()).ToArray();
            if(handVertices.Any(indices=>indices.Length<100))throw new Exception("Missing rigid fist vertices "+fighterId);
            try
            {
                foreach(var move in Moves.All)
                {
                    if(!animator.HasState(0,Animator.StringToHash("Base Layer."+move.Id)))throw new Exception("Missing attack state "+move.Id);
                    var fighter=new Fighter{Attack=move};
                    for(int frame=0;frame<=move.Duration;frame++)
                    {
                        fighter.Frame=frame;view.Pose(fighter,1,1f/60);animator.Update(0);obj.GetComponent<JointCorrectives>()?.Apply();skin.BakeMesh(baked);var posedVertices=baked.vertices;
                        if(posedVertices.Any(v=>float.IsNaN(v.sqrMagnitude)||float.IsInfinity(v.sqrMagnitude)) || baked.bounds.size.magnitude>4)throw new Exception("Invalid combat deformation "+move.Id+" "+frame);
                        for(int hand=0;hand<2;hand++)
                        {
                            Vector3 center=Vector3.zero;foreach(int i in handVertices[hand])center+=posedVertices[i];center/=handVertices[hand].Length;
                            if(Vector3.Distance(skin.transform.TransformPoint(center),skin.bones[handBones[hand]].position)>.25f)throw new Exception("Detached fist "+fighterId+" "+move.Id+" "+frame);
                        }
                        if(!animator.GetCurrentAnimatorStateInfo(0).IsName(move.Id))throw new Exception("Attack unreachable "+move.Id);
                        frames++;
                    }
                }
                foreach(var state in new[]{"Idle","Guard","Walk","WalkBack","Crouch","Fall","GetUp","Hit","Dodge"})if(!animator.HasState(0,Animator.StringToHash("Base Layer."+state)))throw new Exception("Missing support state "+state);
                File.WriteAllText("../art/source/motion-expansion/combat-view-"+fighterId+"-audit.txt",$"25 reachable attacks; {frames} combat poses with finite bounded skin; 9 support states present. Editor validation, not an iPhone test.");Debug.Log("Combat audit passed: fighter "+fighterId+" "+frames+" frames");
            }
            finally{UnityEngine.Object.DestroyImmediate(obj);UnityEngine.Object.DestroyImmediate(baked);}

        }
    }
}
