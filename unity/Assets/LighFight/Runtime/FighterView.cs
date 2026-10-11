using UnityEngine;
using LighFight.Core;

namespace LighFight.Runtime
{
    // Procedural articulated stand-in; replace meshes without changing simulation timing.
    public sealed class FighterView : MonoBehaviour
    {
        [SerializeField] private Transform hips, chest, head, armL, armR, foreL, foreR, legL, legR, shinL, shinR;
        private Material skin, cloth, dark;
        [SerializeField] private Animator animator;
        [System.Serializable] public struct ContactPose { public string moveId; public float phase,start,end; }
        [SerializeField] private ContactPose[] contactPoses = new ContactPose[0];
        public ContactPose[] CopyContactPoses() => (ContactPose[])contactPoses.Clone();
        public void ConfigureAnimation(Animator value,ContactPose[] poses) { animator=value;contactPoses=poses;ownsMaterials=false; }
        private float previousZ;
        private float previousX, stride, motionHold, deathTime, reactionTime;
        private int previousHitSerial;
        private bool hasPosition, backing;
        private Transform[] blendBones;
        private Quaternion[] blendRotations;
        private Vector3[] blendPositions;
        private string previousMotion;
        private float blendElapsed, blendDuration;
        private void LateUpdate()
        {
            if(blendBones==null || blendElapsed>=blendDuration || blendDuration<=0)return;
            float weight=Mathf.SmoothStep(0,1,blendElapsed/blendDuration);
            for(int i=0;i<blendBones.Length;i++){if(blendBones[i]==animator.transform)continue;blendBones[i].localRotation=Quaternion.Slerp(blendRotations[i],blendBones[i].localRotation,weight);blendBones[i].localPosition=Vector3.Lerp(blendPositions[i],blendBones[i].localPosition,weight);}
        }
        [SerializeField] private bool ownsMaterials = true;
        public void UseSharedMaterials() => ownsMaterials = false;
        public bool IsReady => animator!=null ? (animator.runtimeAnimatorController!=null && animator.avatar!=null && animator.avatar.isValid) : (hips!=null && chest!=null && head!=null && armL!=null && armR!=null && foreL!=null && foreR!=null && legL!=null && legR!=null && shinL!=null && shinR!=null);

        public static FighterView Create(string name, Color outfit, bool longHair)
        {
            var view = new GameObject(name).AddComponent<FighterView>();
            view.Build(outfit, longHair);
            return view;
        }
        private Material Material(Color color)
        {
            var m = new Material(Shader.Find("LighFight/OfficeLit")); m.color = color;
            m.SetFloat("_Glossiness", .26f); return m;
        }
        private Transform Joint(string name, Transform parent, Vector3 position)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t;
        }
        private void Mesh(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(type); obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material; if(Application.isPlaying)Destroy(obj.GetComponent<Collider>());else DestroyImmediate(obj.GetComponent<Collider>());
        }
        private void Limb(Transform joint, float length, float width, Material material)
            => Mesh(joint, PrimitiveType.Capsule, new Vector3(0,-length/2,0), new Vector3(width,length/2,width),material);
        private void Build(Color outfit, bool longHair)
        {
            skin = Material(new Color(.74f,.52f,.38f)); cloth = Material(outfit); dark = Material(new Color(.045f,.045f,.055f));
            hips = Joint("Hips",transform,new Vector3(0,.92f,0));
            Mesh(hips,PrimitiveType.Sphere,Vector3.zero,new Vector3(.43f,.28f,.29f),dark);
            chest = Joint("Chest",hips,new Vector3(0,.22f,0));
            Mesh(chest,PrimitiveType.Capsule,new Vector3(0,.16f,0),new Vector3(.48f,.29f,.30f),cloth);
            head = Joint("Head",chest,new Vector3(0,.54f,0));
            Mesh(head,PrimitiveType.Sphere,Vector3.zero,new Vector3(.27f,.34f,.28f),skin);
            Mesh(head,PrimitiveType.Sphere,new Vector3(0,.12f,-.025f),new Vector3(.285f,.14f,.285f),dark);
            if(longHair) Mesh(head,PrimitiveType.Capsule,new Vector3(0,-.07f,-.13f),new Vector3(.22f,.25f,.13f),dark);
            Mesh(head,PrimitiveType.Sphere,new Vector3(0,-.02f,.145f),new Vector3(.065f,.085f,.065f),skin);
            for(int side=-1;side<=1;side+=2)
                Mesh(head,PrimitiveType.Sphere,new Vector3(side*.067f,.035f,.125f),new Vector3(.035f,.025f,.022f),dark);
            armL = Joint("Left shoulder",chest,new Vector3(-.27f,.34f,0));
            armR = Joint("Right shoulder",chest,new Vector3(.27f,.34f,0));
            Limb(armL,.30f,.16f,cloth); Limb(armR,.30f,.16f,cloth);
            foreL = Joint("Left elbow",armL,new Vector3(0,-.30f,0));
            foreR = Joint("Right elbow",armR,new Vector3(0,-.30f,0));
            Limb(foreL,.29f,.115f,skin); Limb(foreR,.29f,.115f,skin);
            Mesh(foreL,PrimitiveType.Sphere,new Vector3(0,-.30f,0),Vector3.one*.15f,dark);
            Mesh(foreR,PrimitiveType.Sphere,new Vector3(0,-.30f,0),Vector3.one*.15f,dark);
            legL = Joint("Left hip",hips,new Vector3(-.14f,-.06f,0));
            legR = Joint("Right hip",hips,new Vector3(.14f,-.06f,0));
            Limb(legL,.43f,.19f,dark); Limb(legR,.43f,.19f,dark);
            shinL = Joint("Left knee",legL,new Vector3(0,-.43f,0));
            shinR = Joint("Right knee",legR,new Vector3(0,-.43f,0));
            Limb(shinL,.39f,.15f,dark); Limb(shinR,.39f,.15f,dark);
            Mesh(shinL,PrimitiveType.Cube,new Vector3(0,-.36f,.065f),new Vector3(.17f,.12f,.31f),dark);
            Mesh(shinR,PrimitiveType.Cube,new Vector3(0,-.36f,.065f),new Vector3(.17f,.12f,.31f),dark);
        }
        private static void Rotate(Transform joint, float x, float y=0, float z=0)
            => joint.localRotation = Quaternion.Euler(x,y,z);
        public void Pose(Fighter fighter, float facing, float delta)
        {
            if(!hasPosition){previousX=fighter.X;previousZ=fighter.Z;hasPosition=true;if(animator!=null){animator.applyRootMotion=false;animator.speed=0;animator.Rebind();animator.Update(0);}}
            if(Mathf.Abs(previousX-fighter.X)+Mathf.Abs(previousZ-fighter.Z)>.0001f){motionHold=.06f;backing=(fighter.X-previousX)*facing+(fighter.Z-previousZ)*fighter.FacingZ<0;}else motionHold=Mathf.Max(0,motionHold-delta);
            bool moving = motionHold>0;
            previousX = fighter.X; previousZ=fighter.Z; stride += moving ? delta*12 : delta*2;
            float gait = moving ? Mathf.Sin(stride)*25 : Mathf.Sin(stride)*2;
            transform.position = new Vector3(fighter.X,0,fighter.Z);
            transform.rotation = Quaternion.Euler(0,facing>0?90:-90,0);
            if(animator!=null)
            {
                deathTime=fighter.Health<=0?deathTime+delta:0;
                if(previousHitSerial!=fighter.HitSerial){previousHitSerial=fighter.HitSerial;reactionTime=0;}else reactionTime+=delta;
                animator.applyRootMotion=false;animator.speed=0;
                string motion=fighter.Health<=0?"Fall":fighter.DownTicks>Match.GetUpTicks?"Fall":fighter.DownTicks>0?"GetUp":fighter.Stun>0?(fighter.LastBlocked?"Guard":"Hit"):fighter.Attack.HasValue?fighter.Attack.Value.Id:fighter.Guard?"Guard":fighter.Crouch?"Crouch":moving?(backing?"WalkBack":"Walk"):"Idle";
                if(motion!=previousMotion)
                {
                    if(blendBones==null){blendBones=animator.GetComponentsInChildren<Transform>();blendRotations=new Quaternion[blendBones.Length];blendPositions=new Vector3[blendBones.Length];}
                    for(int i=0;i<blendBones.Length;i++){blendRotations[i]=blendBones[i].localRotation;blendPositions[i]=blendBones[i].localPosition;}
                    blendElapsed=0;blendDuration=previousMotion==null?0:motion=="Hit"||motion=="Fall"?.04f:fighter.Attack.HasValue?.06f:.10f;previousMotion=motion;
                }
                blendElapsed+=delta;
                int state=Animator.StringToHash("Base Layer."+motion);
                if(animator.HasState(0,state))
                {
                    float phase=fighter.Attack.HasValue?(float)fighter.Frame/fighter.Attack.Value.Duration:fighter.DownTicks>Match.GetUpTicks?1-(fighter.DownTicks-Match.GetUpTicks)/(float)(Match.KnockdownTicks-Match.GetUpTicks):fighter.DownTicks>0?1-fighter.DownTicks/(float)Match.GetUpTicks:Mathf.Repeat(stride*.1f,1);
                    if(fighter.Health<=0)phase=Mathf.Clamp01(deathTime);
                    if(motion=="Hit")phase=Mathf.Clamp01((float)fighter.ReactionTicks/Mathf.Max(1,fighter.ReactionDuration));
                    if(motion=="Idle" || motion=="Guard")phase=0;
                    if(motion=="Crouch" && !moving)phase=.3f;
                    if(fighter.Attack.HasValue)
                    {
                        var contact=System.Array.Find(contactPoses,p=>p.moveId==motion);
                        if(contact.phase>0)
                        {
                            float end=contact.end>contact.start?contact.end:1;
                            float normalized=(contact.phase-contact.start)/(end-contact.start);
                            phase=Mathf.Lerp(contact.start,end,MotionTiming.Phase(fighter.Attack.Value,fighter.Frame,normalized));
                        }
                    }
                    animator.Play(state,0,phase);
                }
                animator.Update(0);ApplyArtistReaction(fighter);
                return;
            }
            hips.localPosition = new Vector3(0,fighter.Crouch?.70f:.92f,0);
            Rotate(hips,0);Rotate(chest,-5);Rotate(head,5);
            Rotate(armL,-58,0,-18);Rotate(armR,-65,0,20);
            Rotate(foreL,-75);Rotate(foreR,-70);
            Rotate(legL,18+gait,0,-7);Rotate(legR,-18-gait,0,7);
            Rotate(shinL,-15);Rotate(shinR,-15);
            if(fighter.Crouch){Rotate(legL,65);Rotate(legR,65);Rotate(shinL,-90);Rotate(shinR,-90);}
            if(fighter.Guard){Rotate(armL,-110,0,-10);Rotate(armR,-110,0,10);Rotate(foreL,-35);Rotate(foreR,-35);}
            if(fighter.Stun>0){var r=ReactionAngles(fighter);Rotate(chest,r.x,0,r.z);Rotate(head,fighter.LastHitLevel==Level.High?-r.x:0);}
            if(fighter.Health<=0 || fighter.DownTicks>0){float fall=fighter.Health<=0?1:Mathf.Clamp01(fighter.DownTicks/16f);transform.rotation *= Quaternion.Euler(0,0,78*fall);hips.localPosition=new Vector3(0,Mathf.Lerp(.92f,.20f,fall),0);return;}
            if(!fighter.Attack.HasValue)return;
            var move=fighter.Attack.Value;
            float preparation=Mathf.Clamp01((float)fighter.Frame/move.Startup);
            float strike=fighter.Frame<move.Startup?0:1-Mathf.Clamp01((float)(fighter.Frame-move.Startup-move.Active)/move.Recovery);
            bool kick=move.Id.Contains("Kick")||move.Id=="sweep"||move.Id=="knee";
            float twist=move.Id=="spinKick"?360*preparation:(kick?12:25)*strike;
            Rotate(chest,-5,twist);
            if(kick)
            {
                float height=move.Level==Level.Low?35:move.Level==Level.High?115:95;
                Rotate(legR,-preparation*35*(1-strike)-strike*height,0,move.Id=="spinKick"?-35:0);
                Rotate(shinR,move.Id=="knee"?-95:-70*(1-strike));Rotate(armR,-70,0,40*strike);
                if(move.Id=="jumpKick")transform.position+=Vector3.up*Mathf.Sin(Mathf.Clamp01((float)fighter.Frame/move.Duration)*Mathf.PI)*.35f;
            }
            else
            {
                bool left=move.Id=="jab"||move.Id=="hook"||move.Id=="body";
                Transform arm=left?armL:armR, fore=left?foreL:foreR;
                float angle=move.Id=="uppercut"?-155:move.Id=="body"?-75:-95;
                Rotate(arm,Mathf.Lerp(-45,angle,strike),0,move.Id=="hook"?-45*strike:0);
                Rotate(fore,Mathf.Lerp(-85,move.Id=="elbow"?-100:-8,strike));
            }
        }
        public static Vector3 ReactionAngles(Fighter fighter)
        {
            if(fighter.Stun<=0 || fighter.DownTicks>0 || fighter.Health<=0)return Vector3.zero;
            float phase=Mathf.Clamp01((float)fighter.ReactionTicks/Mathf.Max(1,fighter.ReactionDuration));
            float envelope=phase<.25f?Mathf.Lerp(.35f,1,phase/.25f):1-Mathf.SmoothStep(0,1,(phase-.25f)/.75f);
            float strength=envelope*(fighter.LastCounter?1.2f:1);
            if(fighter.LastBlocked)return new Vector3(-4,0,2)*strength;
            return (fighter.LastHitLevel==Level.High?new Vector3(-9,0,6):fighter.LastHitLevel==Level.Mid?new Vector3(18,0,3):new Vector3(7,0,-10))*strength;
        }
        private void ApplyArtistReaction(Fighter fighter)
        {
            var r=ReactionAngles(fighter);if(r.sqrMagnitude==0)return;
            if(contactBones==null)BonePoint("Head",Vector3.zero);
            string pivot=fighter.LastHitLevel==Level.Low?"Hips":"Spine";
            if(contactBones.TryGetValue(pivot,out var joint))joint.localRotation*=Quaternion.Euler(r);
            if(!fighter.LastBlocked && fighter.LastHitLevel==Level.High && contactBones.TryGetValue("Head",out var headBone))headBone.localRotation*=Quaternion.Euler(-12*r.magnitude/11f,0,0);
        }
        public void PoseTowards(Fighter fighter,Fighter other,float delta)
        {
            if(!fighter.Attack.HasValue){float distance=Match.Distance(fighter,other);if(distance>.0001f){fighter.FacingX=(other.X-fighter.X)/distance;fighter.FacingZ=(other.Z-fighter.Z)/distance;}}
            Pose(fighter,fighter.FacingX,delta);
            if(fighter.Health>0 && fighter.DownTicks==0){var direction=fighter.Attack.HasValue?new Vector3(fighter.FacingX,0,fighter.FacingZ):new Vector3(other.X-fighter.X,0,other.Z-fighter.Z);if(direction.sqrMagnitude>.00001f)transform.rotation=Quaternion.LookRotation(direction);}
        }
        public Vector3 BonePoint(string name,Vector3 fallback)
        {
            if(contactBones==null){contactBones=new System.Collections.Generic.Dictionary<string,Transform>();foreach(var t in GetComponentsInChildren<Transform>())if(!contactBones.ContainsKey(t.name))contactBones.Add(t.name,t);}
            if(contactBones.TryGetValue(name,out var bone))return bone.position;
            if(animator!=null)return new Vector3(float.NaN,float.NaN,float.NaN);
            switch(name){case "LeftHand":return foreL.TransformPoint(0,-.30f,0);case "RightHand":return foreR.TransformPoint(0,-.30f,0);case "LeftForeArm":return foreL.position;case "RightForeArm":return foreR.position;case "LeftToeBase":case "LeftFoot":return shinL.TransformPoint(0,-.36f,.065f);case "RightToeBase":case "RightFoot":return shinR.TransformPoint(0,-.36f,.065f);case "LeftLeg":return shinL.position;case "RightLeg":return shinR.position;case "Head":return head.position;case "Spine":return chest.TransformPoint(0,.16f,0);case "Hips":return hips.position;}
            return transform.TransformPoint(fallback);
        }
        private System.Collections.Generic.Dictionary<string,Transform> contactBones;
        public void SampleContactPose(Fighter fighter,Fighter other)
        {
            PoseTowards(fighter,other,0);if(animator!=null){if(fighter.Attack.HasValue && fighter.Frame>=fighter.Attack.Value.Startup && fighter.Frame<fighter.Attack.Value.Startup+fighter.Attack.Value.Active)blendElapsed=blendDuration;}
        }
        private void OnDestroy(){if(ownsMaterials){Destroy(skin);Destroy(cloth);Destroy(dark);}}
    }
}
