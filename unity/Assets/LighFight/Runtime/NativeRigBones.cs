using System;
namespace LighFight.Runtime {
 public static class NativeRigBones {
  public static string CanonicalName(string name){
   string key=name.Substring(name.LastIndexOf(':')+1).Replace("_","").Replace(" ","");
   string side=key.StartsWith("Left",StringComparison.Ordinal)?"Left":key.StartsWith("Right",StringComparison.Ordinal)?"Right":null;
   if(side==null)return key=="Chest"?"Spine1":key=="UpperChest"?"Spine2":key;
   string part=key.Substring(side.Length);
   switch(part){
    case "UpperArm":return side+"Arm";
    case "LowerArm":return side+"ForeArm";
    case "UpperLeg":return side+"UpLeg";
    case "LowerLeg":return side+"Leg";
    case "Toes":return side+"ToeBase";
   }
   foreach(string finger in new[]{"Thumb","Index","Middle","Ring","Pinky","Little"}){
    if(!part.StartsWith(finger,StringComparison.Ordinal))continue;
    string segment=part.Substring(finger.Length);int joint=segment=="Proximal"?1:segment=="Intermediate"?2:segment=="Distal"?3:0;
    if(joint>0)return side+"Hand"+(finger=="Little"?"Pinky":finger)+joint;
   }
   return key;
  }
 }
}
