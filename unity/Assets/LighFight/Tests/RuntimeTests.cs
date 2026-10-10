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
        Assert.AreEqual(2,Object.FindObjectsByType<FighterView>(FindObjectsSortMode.None).Length);
        Object.Destroy(root);yield return null;yield return null;
        Assert.AreEqual(0,Object.FindObjectsByType<FighterView>(FindObjectsSortMode.None).Length);
        Assert.AreEqual(0,Object.FindObjectsByType<OfficeArena>(FindObjectsSortMode.None).Length);
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
