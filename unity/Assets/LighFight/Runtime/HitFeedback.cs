using System.Collections.Generic;
using UnityEngine;
using LighFight.Core;
namespace LighFight.Runtime
{
    public sealed class HitFeedback : MonoBehaviour
    {
        private sealed class Spark { public Transform body; public Vector3 velocity; public float life; }
        private readonly List<Spark> sparks=new List<Spark>();
        private Material glow;
        private AudioSource source;
        private AudioClip impact, block;
        private int serialA,serialB;
        private Match previous;
        public float Shake { get; private set; }
        private void Awake()
        {
            glow=new Material(Shader.Find("Unlit/Color"));glow.color=new Color(1,.72f,.21f);
            source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=.30f;
            impact=Sound("Impact",90,.12f);block=Sound("Block",260,.07f);
        }
        private static AudioClip Sound(string name,float frequency,float seconds)
        {
            const int rate=22050;var data=new float[(int)(rate*seconds)];var random=new System.Random(8);
            for(int i=0;i<data.Length;i++){float decay=1-(float)i/data.Length;data[i]=(Mathf.Sin(2*Mathf.PI*frequency*i/rate)*.6f+((float)random.NextDouble()*2-1)*.4f)*decay*decay;}
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        public void Observe(Match match)
        {
            if(previous!=match){previous=match;serialA=serialB=0;}
            if(match.A.HitSerial!=serialA){serialA=match.A.HitSerial;Play(match.A);}
            if(match.B.HitSerial!=serialB){serialB=match.B.HitSerial;Play(match.B);}
        }
        private void Play(Fighter fighter)
        {
            source.PlayOneShot(fighter.LastBlocked?block:impact);Shake=fighter.LastBlocked?.025f:.07f;
            for(int i=0;i<8;i++)
            {
                var obj=GameObject.CreatePrimitive(PrimitiveType.Sphere);obj.transform.SetParent(transform,false);
                obj.transform.position=new Vector3(fighter.X,1.2f,-.15f);obj.transform.localScale=Vector3.one*.055f;
                obj.GetComponent<Renderer>().sharedMaterial=glow;Destroy(obj.GetComponent<Collider>());
                sparks.Add(new Spark{body=obj.transform,velocity=new Vector3(Mathf.Sin(i*2.3f)*2,Mathf.Cos(i*1.7f)*2,.1f),life=.22f});
            }
        }
        private void Update()
        {
            Shake=Mathf.Max(0,Shake-Time.deltaTime*.4f);
            for(int i=sparks.Count-1;i>=0;i--)
            {
                var s=sparks[i];s.life-=Time.deltaTime;
                if(s.life<=0){Destroy(s.body.gameObject);sparks.RemoveAt(i);continue;}
                s.body.position+=s.velocity*Time.deltaTime;s.body.localScale=Vector3.one*s.life*.25f;
            }
        }
        private void OnDestroy(){Destroy(impact);Destroy(block);Destroy(glow);}
    }
}
