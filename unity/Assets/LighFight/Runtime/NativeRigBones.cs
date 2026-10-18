using System;
namespace LighFight.Runtime {
 public static class NativeRigBones {
  public static string CanonicalName(string name){
   string raw=name.Substring(name.LastIndexOf(':')+1);
   string deform=RigifyDeformName(raw);if(deform!=null)return deform;
   string key=raw.Replace("_","").Replace(" ","");
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
  // Exact DEF joints only: bend/twist subdivisions and ORG/CTRL bones must never
  // steal the anatomical joint's alias when the contact dictionary is populated.
  private static string RigifyDeformName(string raw){
   switch(raw){
    case "DEF-spine":return "Hips";
    case "DEF-spine.001":return "Spine";
    case "DEF-spine.002":return "Spine1";
    case "DEF-spine.003":return "Spine2";
    case "DEF-spine.005":return "Neck";
    case "DEF-spine.006":return "Head";
   }
   if(!raw.StartsWith("DEF-",StringComparison.Ordinal))return null;
   string side=raw.EndsWith(".L",StringComparison.Ordinal)?"Left":raw.EndsWith(".R",StringComparison.Ordinal)?"Right":null;
   if(side==null)return null;
   string part=raw.Substring(4,raw.Length-6);
   switch(part){
    case "shoulder":return side+"Shoulder";
    case "upper_arm":return side+"Arm";
    case "forearm":return side+"ForeArm";
    case "hand":return side+"Hand";
    case "thigh":return side+"UpLeg";
    case "shin":return side+"Leg";
    case "foot":return side+"Foot";
    case "toe":return side+"ToeBase";
   }
   if(part.Length<4 || part[part.Length-3]!='.' || part[part.Length-2]!='0')return null;
   char joint=part[part.Length-1];if(joint<'1'||joint>'3')return null;
   string finger=part.Substring(0,part.Length-3),canonical;
   switch(finger){case "thumb":canonical="Thumb";break;case "f_index":canonical="Index";break;case "f_middle":canonical="Middle";break;case "f_ring":canonical="Ring";break;case "f_pinky":canonical="Pinky";break;default:return null;}
   return side+"Hand"+canonical+joint;
  }
 }
}
