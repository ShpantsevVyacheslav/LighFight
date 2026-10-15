using NUnit.Framework;
using UnityEngine;
using LighFight.Runtime;
public class PauseActivationLeaseTests {
 GameObject first,second,cameraObject;Camera camera;
 [SetUp]public void SetUp(){first=new GameObject("visible");second=new GameObject("hidden roster opponent");second.SetActive(false);cameraObject=new GameObject("scene");camera=cameraObject.AddComponent<Camera>();}
 [TearDown]public void TearDown(){Object.DestroyImmediate(first);Object.DestroyImmediate(second);Object.DestroyImmediate(cameraObject);}
 [Test]public void StopsActorsAndSceneCamera(){using(var lease=new PauseActivationLease(camera,new[]{first,second})){Assert.False(first.activeSelf);Assert.False(second.activeSelf);Assert.False(camera.enabled);}}
 [Test]public void ResumeRestoresOnlyPreviouslyActiveObjects(){using(var lease=new PauseActivationLease(camera,new[]{first,second})){}Assert.True(first.activeSelf);Assert.False(second.activeSelf);Assert.True(camera.enabled);}
 [Test]public void DisabledCameraRemainsDisabled(){camera.enabled=false;using(var lease=new PauseActivationLease(camera,new[]{first,second})){}Assert.False(camera.enabled);}
 [Test]public void FailedScopeStillRestores(){try{using(var lease=new PauseActivationLease(camera,new[]{first,second})){throw new System.InvalidOperationException();}}catch(System.InvalidOperationException){}Assert.True(first.activeSelf);Assert.False(second.activeSelf);Assert.True(camera.enabled);}
 [Test]public void DisposalIsIdempotentAndDoesNotOverwriteLaterOwner(){var lease=new PauseActivationLease(camera,new[]{first,second});lease.Dispose();first.SetActive(false);camera.enabled=false;lease.Dispose();Assert.False(first.activeSelf);Assert.False(camera.enabled);}
 [Test]public void DestroyedActorDoesNotPreventOtherRestoration(){var lease=new PauseActivationLease(camera,new[]{first,second});Object.DestroyImmediate(first);lease.Dispose();Assert.False(second.activeSelf);Assert.True(camera.enabled);}
}
