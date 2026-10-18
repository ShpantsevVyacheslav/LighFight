using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Read-only source diagnostics: no importer/source/material-factor mutation.
public static partial class DenseRigifyReimportReview
{
    public static void RunNormalReview()
    {
        if(pendingReview!=null)throw new Exception("Review already running");
        Output=BaseOutput+"/normal-tangent-review-v1";pendingReview=NormalReviewSteps();reviewTicks=0;
        EditorApplication.update+=PumpReview;EditorApplication.QueuePlayerLoopUpdate();
    }
    static IEnumerator NormalReviewSteps()
    {
        Directory.CreateDirectory(Output);var c=LoadConfig();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(c.normal);var importer=(TextureImporter)AssetImporter.GetAtPath(c.normal);
        File.WriteAllLines(Output+"/actual-normal-import.txt",new[]{
            "Source remains unchanged; diagnostic only, no visual acceptance.",
            "type="+importer.textureType+" sRGB="+importer.sRGBTexture+" convert="+importer.convertToNormalmap+" flipGreen="+importer.flipGreenChannel,
            "format="+texture.format+" graphicsFormat="+texture.graphicsFormat+" dimensions="+texture.width+"x"+texture.height+" compression="+importer.textureCompression,
            "filter="+texture.filterMode+" aniso="+texture.anisoLevel+" mipCount="+texture.mipmapCount+" sourceStrength="+c.normalScale,
            "GPU="+SystemInfo.graphicsDeviceName+" api="+SystemInfo.graphicsDeviceType+" colorSpace="+QualitySettings.activeColorSpace});
        if(importer.textureType!=TextureImporterType.NormalMap||importer.sRGBTexture||importer.convertToNormalmap||importer.flipGreenChannel||c.normalScale!=1)throw new Exception("Normal source import contract RED");
        var raw=new Texture2D(2,2,TextureFormat.RGBA32,false,true);var shader=Shader.Find("DenseRigifyReview/NormalGPUProbe");
        if(!shader||!shader.isSupported)throw new Exception("Normal probe unsupported");
        var material=new Material(shader);material.SetTexture("_NormalMap",texture);ShaderUtil.CompilePass(material,0,true);
        var rt=new RenderTexture(1,1,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);rt.Create();
        try
        {
            if(!ImageConversion.LoadImage(raw,File.ReadAllBytes(c.normal),false)||raw.width!=texture.width||raw.height!=texture.height)throw new Exception("Raw PNG decode/dimensions failed");
            yield return null;yield return null;
            var samples=new List<Vector2>{new Vector2(.1f,.1f),new Vector2(.25f,.75f),new Vector2(.5f,.5f),new Vector2(.9f,.8f)};
            // Add strongest XY witnesses from a fixed4096-texel lattice; source pixels, no shader-driven cherry-picking.
            var candidates=new List<Tuple<float,Vector2>>();
            for(int y=32;y<raw.height;y+=64)for(int x=32;x<raw.width;x+=64){var p=raw.GetPixel(x,y);candidates.Add(Tuple.Create((p.r-.5f)*(p.r-.5f)+(p.g-.5f)*(p.g-.5f),new Vector2((x+.5f)/raw.width,(y+.5f)/raw.height)));}
            samples.AddRange(candidates.OrderByDescending(a=>a.Item1).Take(8).Select(a=>a.Item2));
            var rows=new List<string>{"u,v,mode,sourceR,sourceG,sourceB,sourceA,gpuR,gpuG,gpuB,gpuA,blenderNormalizedX,blenderNormalizedY,blenderNormalizedZ,cpuXYReconstructX,cpuXYReconstructY,cpuXYReconstructZ,gpuVsBlenderDegrees,gpuVsXYDegrees,tick"};
            foreach(var requested in samples)
            {
                int x=Mathf.Clamp((int)(requested.x*raw.width),0,raw.width-1),y=Mathf.Clamp((int)(requested.y*raw.height),0,raw.height-1);
                var uv=new Vector2((x+.5f)/raw.width,(y+.5f)/raw.height);var source=raw.GetPixel(x,y);
                var rgb=new Vector3(source.r*2-1,source.g*2-1,source.b*2-1).normalized;
                var xy=new Vector3(source.r*2-1,source.g*2-1,0);xy.z=Mathf.Sqrt(Mathf.Max(0,1-xy.x*xy.x-xy.y*xy.y));
                for(int mode=0;mode<4;mode++)
                {
                    material.SetVector("_ProbeUV",new Vector4(uv.x,uv.y,0,0));material.SetFloat("_Mode",mode);
                    var old=RenderTexture.active;Graphics.Blit(Texture2D.blackTexture,rt,material);RenderTexture.active=old;
                    var request=AsyncGPUReadback.Request(rt,0,TextureFormat.RGBAFloat);double deadline=EditorApplication.timeSinceStartup+15;
                    while(!request.done){if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Normal GPU fence timeout");yield return null;}
                    if(request.hasError)throw new Exception("Normal GPU readback failed");var value=request.GetData<Color>()[0];var decoded=new Vector3(value.r,value.g,value.b);
                    rows.Add(string.Join(",",uv.x,uv.y,mode,source.r,source.g,source.b,source.a,value.r,value.g,value.b,value.a,rgb.x,rgb.y,rgb.z,xy.x,xy.y,xy.z,mode==1||mode==2?Vector3.Angle(decoded,rgb):-1,mode==1||mode==2?Vector3.Angle(decoded,xy):-1,reviewTicks));
                    File.WriteAllLines(Output+"/gpu-normal-decode.csv",rows);yield return null;
                }
            }
        }
        finally{rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(raw);}
        // Bind-space attributes, before Animator playback. Raw source/rest buffers unchanged.
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(c.model);var skin=prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);var mesh=skin.sharedMesh;
        WriteBindTangentAttributes(skin,Output+"/unity-bind-tangent-attributes.bin");
        File.WriteAllLines(Output+"/basis-contract.txt",new[]{"DTB1 little-endian: intvertexCount, vertexCount*12float32(position3 normal3 tangentXYZW4 UV2), intindexCount, int32indices,16float32rowmajor rendererLocalToWorld.","Imported normals retained. Mikk tangents are derived/imported review attributes, not claimed source tangent preservation.","Compare independent Blender loop basis through C=ReflectX*B. Vector normals/tangents transform by C; bitangent is cross(N,T)*sign; reflection requires sign reversal (detC=-1). No hand-tuned green flip.","This diagnostic does not change source or prove final rendered material acceptance."});
        Debug.Log("AGENT_RESULT normal GPU decode and bind tangent diagnostic captured; inspect evidence; no acceptance");
    }
    static void WriteBindTangentAttributes(SkinnedMeshRenderer skin,string path)
    {
        var mesh=skin.sharedMesh;var positions=mesh.vertices;var normals=mesh.normals;var tangents=mesh.tangents;var uv0=mesh.uv;
        if(positions.Length!=normals.Length||positions.Length!=tangents.Length||positions.Length!=uv0.Length)throw new Exception("Complete tangent basis unavailable");
        using(var writer=new BinaryWriter(File.Create(path)))
        {
            writer.Write(new byte[]{(byte)'D',(byte)'T',(byte)'B',(byte)'1'});writer.Write(positions.Length);
            for(int i=0;i<positions.Length;i++){var p=positions[i];var n=normals[i];var t=tangents[i];var u=uv0[i];foreach(float f in new[]{p.x,p.y,p.z,n.x,n.y,n.z,t.x,t.y,t.z,t.w,u.x,u.y})writer.Write(f);}
            var faces=mesh.triangles;writer.Write(faces.Length);foreach(int index in faces)writer.Write(index);
            for(int row=0;row<4;row++)for(int col=0;col<4;col++)writer.Write(skin.transform.localToWorldMatrix[row,col]);
        }
    }

}
