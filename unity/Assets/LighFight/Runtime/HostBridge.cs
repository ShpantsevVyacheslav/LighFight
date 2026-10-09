using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
namespace LighFight.Runtime {
[Serializable] public sealed class LaunchRequest {
 public int protocolVersion; public string sessionId; public string mode;
 public string fighterId; public string arenaId; public string locale;
}
[Serializable] public sealed class HostEvent {
 public int protocolVersion = 1; public string type; public string sessionId;
}
public sealed class HostBridge : MonoBehaviour {
 public event Action<LaunchRequest> LaunchRequested;
 public event Action ExitRequested;
 private string sessionId;
 public LaunchRequest CurrentLaunch {get; private set;}
#if UNITY_IOS && !UNITY_EDITOR
 [DllImport("__Internal")] private static extern void LighFightEmit(string json);
#endif
 void Awake() { gameObject.name = "LighFightHostBridge"; DontDestroyOnLoad(gameObject); }
 IEnumerator Start() { yield return null; Emit("ready"); }
 public void Launch(string json) {
  try {
   var r = JsonUtility.FromJson<LaunchRequest>(json);
   if (r == null || r.protocolVersion != 1 || string.IsNullOrWhiteSpace(r.sessionId) || r.mode != "bot")
    throw new ArgumentException("invalid_launch");
   sessionId = r.sessionId; CurrentLaunch = r; Time.timeScale = 1;
   LaunchRequested?.Invoke(r); Emit("launched");
  } catch (Exception e) { Debug.LogWarning("LighFight launch rejected: " + e.GetType().Name); Emit("failed"); }
 }
 public void Pause(string value) { Time.timeScale = value == "true" ? 0 : 1; }
 public void Exit(string ignored) { Time.timeScale = 0; ExitRequested?.Invoke(); Emit("exit"); }
 private void Emit(string type) {
  var json = JsonUtility.ToJson(new HostEvent { type = type, sessionId = sessionId });
#if UNITY_ANDROID && !UNITY_EDITOR
  try { using (var host = new AndroidJavaClass("com.lighchat.lighfight.LighFightPlugin")) host.CallStatic("emit", json); }
  catch (Exception e) { Debug.LogWarning("LighFight host unavailable: " + e.GetType().Name); }
#elif UNITY_IOS && !UNITY_EDITOR
  LighFightEmit(json);
#endif
 }
}
}
