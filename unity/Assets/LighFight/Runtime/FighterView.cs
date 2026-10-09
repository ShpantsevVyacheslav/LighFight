using UnityEngine;
using LighFight.Core;

namespace LighFight.Runtime
{
    // Procedural articulated stand-in; replace meshes without changing simulation timing.
    public sealed class FighterView : MonoBehaviour
    {
        private Transform hips, chest, head, armL, armR, foreL, foreR, legL, legR, shinL, shinR;
        private Material skin, cloth, dark;
        private float previousX, stride;

        public static FighterView Create(string name, Color outfit, bool longHair)
        {
            var view = new GameObject(name).AddComponent<FighterView>();
            view.Build(outfit, longHair);
            return view;
        }
        private Material Material(Color color)
        {
            var m = new Material(Shader.Find("Standard")); m.color = color;
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
            bool moving = Mathf.Abs(previousX-fighter.X) > .0001f;
            previousX = fighter.X; stride += moving ? delta*12 : delta*2;
            float gait = moving ? Mathf.Sin(stride)*25 : Mathf.Sin(stride)*2;
            transform.position = new Vector3(fighter.X,0,0);
            transform.rotation = Quaternion.Euler(0,facing>0?90:-90,0);
            hips.localPosition = new Vector3(0,fighter.Crouch?.70f:.92f,0);
            Rotate(hips,0);Rotate(chest,-5);Rotate(head,5);
            Rotate(armL,-58,0,-18);Rotate(armR,-65,0,20);
            Rotate(foreL,-75);Rotate(foreR,-70);
            Rotate(legL,18+gait,0,-7);Rotate(legR,-18-gait,0,7);
            Rotate(shinL,-15);Rotate(shinR,-15);
            if(fighter.Crouch){Rotate(legL,65);Rotate(legR,65);Rotate(shinL,-90);Rotate(shinR,-90);}
            if(fighter.Guard){Rotate(armL,-110,0,-10);Rotate(armR,-110,0,10);Rotate(foreL,-35);Rotate(foreR,-35);}
            if(fighter.Stun>0){Rotate(chest,18,0,8);Rotate(head,-15);}
            if(fighter.Health<=0){transform.rotation *= Quaternion.Euler(0,0,78);hips.localPosition=new Vector3(0,.20f,0);return;}
            if(!fighter.Attack.HasValue)return;
            var move=fighter.Attack.Value;
            float preparation=Mathf.Clamp01((float)fighter.Frame/move.Startup);
            float strike=fighter.Frame<move.Startup?0:1-Mathf.Clamp01((float)(fighter.Frame-move.Startup-move.Active)/move.Recovery);
            bool kick=move.Id.Contains("Kick")||move.Id=="sweep"||move.Id=="knee";
            float twist=move.Id=="spinKick"?360*preparation:(kick?12:25)*strike;
            Rotate(chest,-5,twist);
            if(kick)
            {
                float height=move.Level==Level.Low?35:move.Id=="knee"?95:110;
                Rotate(legR,-preparation*35-strike*height,0,move.Id=="spinKick"?-35:0);
                Rotate(shinR,-70*(1-strike));Rotate(armR,-70,0,40*strike);
                if(move.Id=="jumpKick")transform.position+=Vector3.up*Mathf.Sin(preparation*Mathf.PI)*.28f;
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
        private void OnDestroy(){Destroy(skin);Destroy(cloth);Destroy(dark);}
    }
}
