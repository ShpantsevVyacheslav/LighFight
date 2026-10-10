using System;
using UnityEngine;

namespace LighFight.Runtime
{
    // Pose-dependent local sculpt corrections on top of ordinary Unity skinning.
    public sealed class JointCorrectives : MonoBehaviour
    {
        public float[] referenceAngles = { 120, 120, 45 };
        SkinnedMeshRenderer skin;
        Transform[] parent, joint, end;
        int[] shapes;
        int calf;
        void Initialize()
        {
            skin=GetComponentInChildren<SkinnedMeshRenderer>(true);
            var bones=GetComponentsInChildren<Transform>(true);
            string[] parents={"LeftArm","RightArm","LeftUpLeg"};
            string[] joints={"LeftForeArm","RightForeArm","LeftLeg"};
            string[] ends={"LeftHand","RightHand","LeftFoot"};
            string[] names={"LeftElbow","RightElbow","LeftKnee"};
            parent=new Transform[3];joint=new Transform[3];end=new Transform[3];shapes=new int[3];
            for(int i=0;i<3;i++)
            {
                parent[i]=Array.Find(bones,b=>b.name==parents[i]);joint[i]=Array.Find(bones,b=>b.name==joints[i]);end[i]=Array.Find(bones,b=>b.name==ends[i]);
                shapes[i]=FindShape(names[i]);
            }
            calf=FindShape("LeftCalfContour");
        }
        int FindShape(string name)
        {
            for(int i=0;i<skin.sharedMesh.blendShapeCount;i++)if(skin.sharedMesh.GetBlendShapeName(i).Contains(name))return i;
            return -1;
        }
        public void Apply()
        {
            if(!enabled)return;
            if(skin==null)Initialize();
            // Re-resolve shapes because the audit deliberately replaces the candidate mesh.
            if(skin.sharedMesh.blendShapeCount==0)return;
            for(int i=0;i<3;i++)
            {
                if(shapes[i]<0 || parent[i]==null || joint[i]==null || end[i]==null)continue;
                float bend=180-Vector3.Angle(parent[i].position-joint[i].position,end[i].position-joint[i].position);
                skin.SetBlendShapeWeight(shapes[i],100*Mathf.Clamp01(bend/Mathf.Max(1,referenceAngles[i])));
            }
            if(calf>=0)skin.SetBlendShapeWeight(calf,100);
        }
    }
}
