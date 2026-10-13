using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using LighFight.Runtime;
using LighFight.Core;
public class RuntimeTests
{
    [Test] public void IslandIdleDoesNotShrugUpperArmsAboveItsNeck(){
        var actor=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-3.prefab"));
        try{var view=actor.GetComponent<FighterView>();view.Pose(new Fighter(),1,0);
            foreach(float seconds in new[]{0f,.2f,.4f,.6f,.8f,1f,1.2f,1.4f}){
                Assert.IsTrue(view.PreviewNativeMotion("Idle",seconds));var neck=view.BonePoint("Neck",Vector3.zero);
                foreach(string arm in new[]{"LeftArm","RightArm"})Assert.Less(view.BonePoint(arm,Vector3.zero).y,neck.y-.015f,"Shrugged "+arm+" at "+seconds);
            }
        }finally{Object.DestroyImmediate(actor);}
    }
    [TestCase(0)][TestCase(1)][TestCase(2)]
    public void GraphicsOptionsNeverEnableGameplayShadows(int quality)
    {
        var ints=new[]{"lf.swipes","lf.graphics","lf.fps","lf.sound","lf.shake","lf.left","lf.attackButton.0","lf.attackButton.1","lf.attackButton.2","lf.attackButton.3"};
        var floats=new[]{"lf.controlScale","lf.controlOpacity"};
        var present=new System.Collections.Generic.HashSet<string>();var values=new System.Collections.Generic.Dictionary<string,float>();
        foreach(var key in ints){if(PlayerPrefs.HasKey(key))present.Add(key);values[key]=PlayerPrefs.GetInt(key);}
        foreach(var key in floats){if(PlayerPrefs.HasKey(key))present.Add(key);values[key]=PlayerPrefs.GetFloat(key);}
        var shadows=QualitySettings.shadows;var aa=QualitySettings.antiAliasing;var weights=QualitySettings.skinWeights;var lod=QualitySettings.lodBias;var mip=QualitySettings.globalTextureMipmapLimit;var fps=Application.targetFrameRate;var root=new GameObject("Shadow options regression");
        try{
            var host=root.AddComponent<Prototype>();var light=new GameObject("Authored shadow light").AddComponent<Light>();light.transform.SetParent(root.transform);light.shadows=LightShadows.Soft;
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            typeof(Prototype).GetField("graphicsQuality",flags).SetValue(host,quality);QualitySettings.shadows=ShadowQuality.All;
            typeof(Prototype).GetMethod("ApplyOptions",flags).Invoke(host,null);
            Assert.AreEqual(ShadowQuality.Disable,QualitySettings.shadows,"Quality tier restored shadows");
            Assert.AreEqual(LightShadows.None,light.shadows,"Scene light still emits shadows");
        }finally{
            Object.DestroyImmediate(root);QualitySettings.shadows=shadows;QualitySettings.antiAliasing=aa;QualitySettings.skinWeights=weights;QualitySettings.lodBias=lod;QualitySettings.globalTextureMipmapLimit=mip;Application.targetFrameRate=fps;
            foreach(var key in ints)if(present.Contains(key))PlayerPrefs.SetInt(key,(int)values[key]);else PlayerPrefs.DeleteKey(key);
            foreach(var key in floats)if(present.Contains(key))PlayerPrefs.SetFloat(key,values[key]);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
        }
    }
    static int hostExitCount;
    static void CountHostExit()=>hostExitCount++;
    [Test]
    public void AttackControlsKeepTheirGapsAtEverySupportedSizeAndHandedness()
    {
        foreach(float scale in new[]{.8f,1f,1.25f})foreach(bool mirrored in new[]{false,true}) {
            for(int i=0;i<4;i++) {
                var rect=Prototype.MainAttackRect(i,scale,mirrored);
                Assert.GreaterOrEqual(rect.xMin,0);Assert.LessOrEqual(rect.xMax,960);Assert.LessOrEqual(rect.yMax,540);
                Assert.IsTrue(Prototype.ControlContains(rect,rect.center,true));
                Assert.IsFalse(Prototype.ControlContains(rect,new Vector2(rect.xMin+1,rect.yMin+1),true),"Invisible corners must not trigger round buttons");
                for(int j=i+1;j<4;j++)Assert.IsFalse(rect.Overlaps(Prototype.MainAttackRect(j,scale,mirrored)),"Expanded attack controls overlap");
                Assert.That(rect.center.x+Prototype.MainAttackRect(i,scale,!mirrored).center.x,Is.EqualTo(960).Within(.001f));
            }
        }
    }
    [Test]
    public void MaximumPreviewUsesActualPhonePixelsAndAspect()
    {
        var size=Prototype.SelectionPreviewSize(new Vector2(2868,1320),2);
        Assert.AreEqual(new Vector2Int(1285,924),size);
        Assert.That((float)size.x/size.y,Is.EqualTo((430f*2868/960)/(378f*1320/540)).Within(.001f),"Character render target must match its physical screen rectangle");
        var low=Prototype.SelectionPreviewSize(new Vector2(2868,1320),0);
        Assert.Less(low.x,size.x);Assert.Less(low.y,size.y);
        var capped=Prototype.SelectionPreviewSize(Vector2.one*10000,2);Assert.AreEqual(2048,capped.y);Assert.That((float)capped.x/capped.y,Is.EqualTo((430f/960)/(378f/540)).Within(.001f));
    }
    [Test]
    public void JoystickMatchesItsCircleAndKeepsSensitivityWhenResized()
    {
        foreach(float scale in new[]{.8f,1f,1.25f})
        {
            var area=new Rect(30,355,160*scale,160*scale);
            Assert.IsFalse(Prototype.StickContains(area,area.min),"Invisible square corners must not capture a thumb");
            Assert.IsTrue(Prototype.StickContains(area,area.center+Vector2.right*area.width*.49f));
            Assert.AreEqual(Vector2.zero,Prototype.NormalizeStick(area,area.center));
            var input=Prototype.NormalizeStick(area,area.center+new Vector2(30,-30)*scale);
            Assert.That(input.x,Is.EqualTo(.5f).Within(.0001f));Assert.That(input.y,Is.EqualTo(.5f).Within(.0001f));
            Assert.That(Prototype.NormalizeStick(area,area.center+Vector2.one*1000).magnitude,Is.EqualTo(1).Within(.0001f));
        }
    }
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
                var bones=obj.GetComponentsInChildren<Transform>();var left=System.Array.Find(bones,t=>t.name=="LeftHand"||t.name.EndsWith(":LeftHand"));var right=System.Array.Find(bones,t=>t.name=="RightHand"||t.name.EndsWith(":RightHand"));
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
    [Test] public void PracticeSpacingResetPreservesDefenceAndClearsAttack()
    {
        var root=new GameObject("Practice reset test");
        try{var game=root.AddComponent<Prototype>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(Prototype).GetField("training",flags).SetValue(game,true);var sessionField=typeof(Prototype).GetField("session",flags);var old=new FightSession(true);old.SetDummyDefence(2);old.Match.A.X=4;old.Match.A.Z=1;old.Match.Attack(old.Match.A,0);sessionField.SetValue(game,old);typeof(Prototype).GetMethod("Reset",flags).Invoke(game,null);var current=(FightSession)sessionField.GetValue(game);Assert.AreNotSame(old,current);Assert.AreEqual(DummyDefence.LowGuard,current.Defence);Assert.AreEqual(-1.4f,current.Match.A.X);Assert.AreEqual(0,current.Match.A.Z);Assert.IsNull(current.Match.A.Attack);current.Step();Assert.IsTrue(current.Match.B.Crouch);Assert.IsTrue(current.Match.B.Guard);}
        finally{Object.DestroyImmediate(root);}
    }
    [TestCase(0,0,StrikeOutcome.Hit)] [TestCase(1,0,StrikeOutcome.Blocked)] [TestCase(2,5,StrikeOutcome.Blocked)]
    public void ArtistDummyDefenceUsesPhysicalContact(int defence,int move,StrikeOutcome expected)
    {
        var a=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-4.prefab"));var b=Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LighFight/Art/Fighters/fighter-3.prefab"));
        try{var s=new FightSession(true);s.Match.A.X=0;s.Match.B.X=.7f;s.SetDummyDefence(defence);new PhysicalContacts(s.Match,a.GetComponent<FighterView>(),b.GetComponent<FighterView>());s.QueueAttack(true,move);for(int i=0;i<Moves.All[move].Duration;i++)s.Step();Assert.AreEqual(expected,s.Match.A.Outcome);Assert.AreEqual(1,s.Match.B.HitSerial);Assert.AreEqual(150,s.Match.B.Health);}
        finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
    }
    [Test] public void AuditEveryArtistStrikeAtPlayableDistances()
    {
        var lines=new System.Collections.Generic.List<string>{"fighter,move,closestHit,farthestHit"};
        var failures=new System.Collections.Generic.List<string>();
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
            if(closest<.6f)failures.Add(artist+" "+move.Id+" must have a playable contact distance");lines.Add(artist+","+move.Id+","+closest.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+farthest.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        var path=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../.tools/reports/strike-audit.csv"));System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));System.IO.File.WriteAllLines(path,lines);
        Assert.IsEmpty(failures,string.Join("; ",failures));
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
        var previewCamera=System.Array.Find(game.GetComponentsInChildren<Camera>(),c=>c.cullingMask!=0);
        Assert.NotNull(previewCamera,"Selection must own a camera that renders the fighter");
        if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)Assert.NotNull(previewCamera.targetTexture,"Selection composites the character over the designed backdrop");
        Assert.AreEqual(CameraClearFlags.SolidColor,previewCamera.clearFlags);
        Assert.AreEqual(0,previewCamera.backgroundColor.a);
        game.BeginFight();yield return null;yield return null;
        Assert.IsNull(previewCamera.targetTexture,"Battle must render directly to the screen");
        Assert.AreEqual(new Rect(0,0,1,1),previewCamera.rect,"Battle must restore full viewport");
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
        int destroyFrame=Time.frameCount;Object.Destroy(root);
        // Editor test-runner yields can resume within the same player frame.
        // Wait for Unity's deferred destruction, rather than counting editor ticks.
        float destructionDeadline=Time.realtimeSinceStartup+3;
        while(root!=null&&Time.realtimeSinceStartup<destructionDeadline)yield return null;
        Assert.IsTrue(root==null,"Unity did not complete deferred destruction within 3 seconds");
        var remaining=Object.FindObjectsByType<FighterView>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Assert.AreEqual(0,remaining.Length,$"Destroy frame {destroyFrame}, assertion frame {Time.frameCount}; "+string.Join("; ",System.Array.ConvertAll(remaining,v=>v.name+" parent="+(v.transform.parent?v.transform.parent.name:"none"))));
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
        var bridgeField=typeof(Prototype).GetField("bridge",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        float deadline=Time.realtimeSinceStartup+3;
        while(bridgeField.GetValue(game)==null&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.AreSame(host,bridgeField.GetValue(game),"Prototype did not finish its startup or attached to another host");
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
