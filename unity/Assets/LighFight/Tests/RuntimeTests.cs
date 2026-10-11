using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using LighFight.Runtime;
using LighFight.Core;
public class RuntimeTests
{
    static int hostExitCount;
    static void CountHostExit()=>hostExitCount++;
    [Test]
    public void FightCameraKeepsBothBodiesInsideHorizontalView()
    {
        foreach(float aspect in new[]{16f/9,19.5f/9,.5f})foreach(float separation in new[]{1f,2.8f,8f})
        {
            float distance=Prototype.FitCameraDistance(-separation/2,separation/2,aspect);
            float visible=2*distance*Mathf.Tan(21*Mathf.Deg2Rad)*aspect;
            Assert.GreaterOrEqual(visible,separation+2.19f);
        }
        Assert.Less(Prototype.FitCameraDistance(-1.4f,1.4f,16f/9),4f,"Nearby fighters should fill the screen");
    }
    [Test]
    public void ArtistSpawnIsIdleAndRetreatUsesBackstep()
    {
        var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-4.prefab");var obj=Object.Instantiate(prefab);
        try
        {
            var view=obj.GetComponent<FighterView>();var animator=obj.GetComponent<Animator>();var fighter=new Fighter{X=-1.4f};
            view.Pose(fighter,1,0);animator.Update(0);Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"Spawning is not walking");
            fighter.X-=.045f;view.Pose(fighter,1,1f/60);animator.Update(0);Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("WalkBack"),"Retreat must use the backward step");
            fighter.X+=.045f;view.Pose(fighter,1,1f/60);animator.Update(0);Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"));
        }
        finally{Object.DestroyImmediate(obj);}
    }
    [Test]
    public void ArtistGuardProtectsChestAndJabExtendsTowardOpponent()
    {
        foreach(int id in new[]{3,4})
        {
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-"+id+".prefab");var obj=Object.Instantiate(prefab);
            try
            {
                var view=obj.GetComponent<FighterView>();var animator=obj.GetComponent<Animator>();var fighter=new Fighter{Guard=true};
                view.Pose(fighter,1,0);animator.Update(0);
                var bones=obj.GetComponentsInChildren<Transform>();var left=System.Array.Find(bones,t=>t.name=="LeftHand");var right=System.Array.Find(bones,t=>t.name=="RightHand");
                Assert.Greater(Mathf.Min(left.position.y,right.position.y),1.2f,"Guard hands below chest on fighter "+id);
                float guard=Mathf.Max(left.position.x,right.position.x);fighter.Guard=false;fighter.Start(0);fighter.Frame=Moves.All[0].Startup;
                view.Pose(fighter,1,0);animator.Update(0);Assert.Greater(Mathf.Max(left.position.x,right.position.x),guard+.1f,"Jab does not reach forward on fighter "+id);
            }
            finally{Object.DestroyImmediate(obj);}
        }
    }
    [Test]
    public void ArtistJabUsesActualContactAndMissesOutsideItsTrajectory()
    {
        var prefabA=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-4.prefab");
        var prefabB=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-3.prefab");
        foreach(bool dodge in new[]{false,true})
        {
            var a=Object.Instantiate(prefabA);var b=Object.Instantiate(prefabB);
            try
            {
                var m=new Match();m.A.X=0;m.B.X=.85f;
                var contacts=new PhysicalContacts(m,a.GetComponent<FighterView>(),b.GetComponent<FighterView>());
                m.Attack(m.A,0);if(dodge)m.B.Z=1;
                for(int tick=0;tick<Moves.All[0].Duration;tick++)m.Step();
                if(dodge)Assert.AreEqual(150,m.B.Health,"A sidestep out of the locked jab path must miss");
                else Assert.Less(m.B.Health,150,"Close-range jab should connect with animated body");
            }
            finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
        }
    }
    [TestCase(0,1)] [TestCase(0,1,2,5)] public void ArtistConfirmedChainUsesActualContacts(params int[] sequence)
    {
        var a=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-4.prefab"));
        var b=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-3.prefab"));
        try{var m=new Match();m.A.X=0;m.B.X=.85f;new PhysicalContacts(m,a.GetComponent<FighterView>(),b.GetComponent<FighterView>());foreach(int index in sequence){Assert.IsTrue(m.Attack(m.A,index),Moves.All[index].Id);while(m.A.Frame<m.A.Attack.Value.Startup+m.A.Attack.Value.Active)m.Step();}Assert.AreEqual(sequence.Length,m.B.HitSerial);int damage=0;for(int i=0;i<sequence.Length;i++)damage+=Combos.Damage(Moves.All[sequence[i]].Damage,i+1);Assert.AreEqual(150-damage,m.B.Health);}
        finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
    }
    [Test] public void ArtistReactionDoesNotAccumulateWhenPoseIsSampledAgain()
    {
        var a=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-4.prefab"));
        try{var view=a.GetComponent<FighterView>();var fighter=new Fighter{Stun=18,ReactionDuration=18,ReactionTicks=4,LastHitLevel=Level.Mid};var other=new Fighter{X=1};view.PoseTowards(fighter,other,0);var spine=System.Array.Find(a.GetComponentsInChildren<Transform>(),t=>t.name=="Spine");var rotation=spine.localRotation;for(int i=0;i<10;i++)view.SampleContactPose(fighter,other);Assert.Less(Quaternion.Angle(rotation,spine.localRotation),.05f);}
        finally{Object.DestroyImmediate(a);}
    }
    [Test] public void AuditEveryArtistStrikeAtPlayableDistances()
    {
        var lines=new System.Collections.Generic.List<string>{"fighter,move,closestHit,farthestHit"};
        foreach(int artist in new[]{3,4})foreach(var move in Moves.All)
        {
            float closest=-1,farthest=-1;
            foreach(float distance in new[]{.60f,.70f,.85f,1f,1.2f,1.4f,1.6f,1.8f,2f,2.4f,4f})
            {
                var a=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-"+artist+".prefab"));
                var b=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-"+(artist==3?4:3)+".prefab"));
                try{var m=new Match();m.A.X=0;m.B.X=distance;new PhysicalContacts(m,a.GetComponent<FighterView>(),b.GetComponent<FighterView>());m.Attack(m.A,System.Array.IndexOf(Moves.All,move));for(int tick=0;tick<move.Duration;tick++)m.Step();if(distance==4)Assert.AreEqual(150,m.B.Health,artist+" "+move.Id+" must miss at 4m");if(m.B.HitSerial>0){if(closest<0)closest=distance;farthest=distance;}}
                finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
            }
            Assert.GreaterOrEqual(closest,.6f,artist+" "+move.Id+" must have a playable contact distance");lines.Add(artist+","+move.Id+","+closest.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+farthest.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        var path=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../.tools/reports/strike-audit.csv"));System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));System.IO.File.WriteAllLines(path,lines);
    }
    [UnityTest]
    public IEnumerator EmbeddedBattleUsesArtistRigsAndReturnsOnce()
    {
        EditorSceneManager.OpenScene("Assets/LighFight/Scenes/HostFightReview.unity");
        yield return new EnterPlayMode();yield return null;yield return null;
        var game=Object.FindFirstObjectByType<Prototype>();var host=Object.FindFirstObjectByType<HostBridge>();
        host.Launch("{\"protocolVersion\":1,\"sessionId\":\"fight-review\",\"mode\":\"bot\",\"locale\":\"ru\"}");
        yield return null;
        Assert.AreEqual(1,Object.FindObjectsByType<FighterView>(FindObjectsSortMode.None).Length,"Selection must hide the opponent");
        Assert.Less(Object.FindFirstObjectByType<Camera>().rect.width,1,"Selection uses central model viewport");
        game.BeginFight();yield return null;yield return null;
        Assert.AreEqual(new Rect(0,0,1,1),Object.FindFirstObjectByType<Camera>().rect,"Battle must restore full viewport");
        var views=Object.FindObjectsByType<FighterView>(FindObjectsSortMode.None);Assert.AreEqual(2,views.Length);
        foreach(var view in views)
        {
            var animator=view.GetComponent<Animator>();Assert.NotNull(animator);
            foreach(var move in Moves.All)Assert.IsTrue(animator.HasState(0,Animator.StringToHash("Base Layer."+move.Id)),move.Id);
        }
        hostExitCount=0;host.ExitRequested+=CountHostExit;host.Exit("");host.Exit("");Assert.AreEqual(1,hostExitCount);
        host.Launch("{\"protocolVersion\":1,\"sessionId\":\"fight-relaunch\",\"mode\":\"bot\"}");host.Exit("");Assert.AreEqual(2,hostExitCount);
        host.ExitRequested-=CountHostExit;Time.timeScale=1;Object.Destroy(game.gameObject);Object.Destroy(host.gameObject);
        yield return null;yield return null;yield return new ExitPlayMode();
    }
    [UnityTest]
    public IEnumerator EmbeddedArtReviewWaitsForHostAndReturnsThroughBridge()
    {
        EditorSceneManager.OpenScene("Assets/LighFight/Scenes/HostArtReview.unity");
        yield return new EnterPlayMode();
        yield return null;yield return null;
        var review=Object.FindFirstObjectByType<MeshyArtReview>();
        var host=Object.FindFirstObjectByType<HostBridge>();
        Assert.IsTrue(review.embeddedHost);
        Assert.IsFalse(review.HasAcceptedHostLaunch);
        float waitingTime=review.ReviewTime;
        yield return null;yield return null;
        Assert.AreEqual(waitingTime,review.ReviewTime,"Animation must wait for host launch");
        host.Launch("{\"protocolVersion\":1,\"sessionId\":\"review-session\",\"mode\":\"bot\",\"locale\":\"ru\"}");
        Assert.IsTrue(review.HasAcceptedHostLaunch);
        Assert.IsTrue(review.candidate.gameObject.activeSelf);
        Assert.IsFalse(review.fighter.gameObject.activeSelf);
        yield return null;yield return null;
        Assert.Greater(review.ReviewTime,0);
        hostExitCount=0;host.ExitRequested+=CountHostExit;
        review.ReturnToHost();review.ReturnToHost();
        Assert.AreEqual(1,hostExitCount,"Return emits exactly one native exit");
        Assert.IsFalse(review.HasAcceptedHostLaunch);
        Assert.AreEqual("review-session",host.CurrentLaunch.sessionId);
        Time.timeScale=1;
        Object.Destroy(host.gameObject);
        yield return null;
        yield return new ExitPlayMode();
    }
    [UnityTest]
    public IEnumerator PrototypeOwnsAndDestroysItsWorld()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var root=new GameObject("Test battle");root.AddComponent<Prototype>();
        yield return null;yield return null;
        Assert.AreEqual(2,Object.FindObjectsByType<FighterView>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length);
        Object.Destroy(root);yield return null;yield return null;
        Assert.AreEqual(0,Object.FindObjectsByType<FighterView>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length);
        Assert.AreEqual(0,Object.FindObjectsByType<OfficeArena>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length);
        Assert.AreEqual(0,Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length);
        yield return new ExitPlayMode();
    }
    [UnityTest]
    public IEnumerator LaunchBeforeContentLoadKeepsLanguage()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        yield return new EnterPlayMode();
        var hostObject=new GameObject("Test host");var host=hostObject.AddComponent<HostBridge>();
        host.Launch("{\"protocolVersion\":1,\"sessionId\":\"test-session\",\"mode\":\"bot\",\"locale\":\"en\"}");
        var root=new GameObject("Downloaded battle");var game=root.AddComponent<Prototype>();
        yield return null;yield return null;
        Assert.AreEqual("LighFight — choose fighter and arena",game.Title);
        Object.Destroy(root);Object.Destroy(hostObject);yield return null;yield return null;
        yield return new ExitPlayMode();
    }
    [Test]
    public void ActiveFrontKickPointsTowardOpponent()
    {
        var view=FighterView.Create("Test rig",Color.cyan,false);
        var fighter=new Fighter { X=-1 };fighter.Start(4);fighter.Frame=20;
        view.Pose(fighter,1,0);
        var thigh=view.transform.Find("Hips/Right hip");
        Assert.Greater(thigh.TransformDirection(Vector3.down).x,.8f);
        Object.DestroyImmediate(view.gameObject);
    }
    [Test] public void WalkDoesNotFlickerBetweenSimulationTicks()
    {
        var view=FighterView.Create("120Hz rig",Color.cyan,false);var fighter=new Fighter { X=-1 };
        view.Pose(fighter,1,1f/120);fighter.X+=.045f;view.Pose(fighter,1,1f/120);
        var thigh=view.transform.Find("Hips/Right hip");var before=thigh.localRotation;
        view.Pose(fighter,1,1f/120);
        Assert.Less(Quaternion.Angle(before,thigh.localRotation),3f);
        Object.DestroyImmediate(view.gameObject);
    }
}

public class ReactionPoseTests
{
 [Test] public void HeadBodyLegAndBlockHaveDistinctResponsesAndSettle(){var f=new Fighter{Stun=18,ReactionDuration=18,ReactionTicks=4};f.LastHitLevel=Level.High;var high=FighterView.ReactionAngles(f);f.LastHitLevel=Level.Mid;var mid=FighterView.ReactionAngles(f);f.LastHitLevel=Level.Low;var low=FighterView.ReactionAngles(f);Assert.Less(high.x,0);Assert.Greater(mid.x,10);Assert.Less(low.z,-5);f.LastBlocked=true;Assert.Less(FighterView.ReactionAngles(f).magnitude,low.magnitude);f.Stun=0;Assert.AreEqual(Vector3.zero,FighterView.ReactionAngles(f));}
}
