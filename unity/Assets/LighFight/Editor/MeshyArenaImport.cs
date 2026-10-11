using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using LighFight.Runtime;
namespace LighFight.Editor
{
    /// <summary>Import a composed scene; retain authored placement and reject blocked combat floors.</summary>
    public static class MeshyArenaImport
    {
        public static readonly Bounds CombatVolume = new Bounds(new Vector3(0,1.1f,0),new Vector3(11,2.2f,4));
        [MenuItem("LighFight/Import selected Meshy arena")]
        public static void ImportSelected()
        {
            var source=Selection.activeObject as GameObject;
            if(!source || !AssetDatabase.Contains(source))throw new InvalidOperationException("Select an imported scene FBX or prefab. Name the playable flat floor FightFloor.");
            string path=AssetDatabase.GenerateUniqueAssetPath("Assets/LighFight/Art/ImportedArenas/"+source.name+".prefab");
            Selection.activeObject=Import(source,path);
        }
        public static GameObject Import(GameObject source,string path,int slot=-1)
        {
            if(!source || !AssetDatabase.Contains(source))throw new InvalidOperationException("Missing imported scene asset");
            var content=slot<0?null:AssetDatabase.LoadAssetAtPath<GameContent>("Assets/LighFight/Content/GameContent.asset");
            if(slot>=0 && (!content || slot>=content.arenas.Length))throw new InvalidOperationException("Invalid arena catalog slot");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var root=new GameObject(source.name+" arena");
            try
            {
                var scene=(GameObject)PrefabUtility.InstantiatePrefab(source);
                scene.transform.SetParent(root.transform,false);
                Validate(root);
                // Combat simulation owns movement limits; decorative physics cannot disagree with it.
                PrefabUtility.UnpackPrefabInstance(scene,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var collider in root.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
                root.AddComponent<OfficeArena>().UseSharedMaterials();
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);
                if(content){content.arenas[slot]=prefab;EditorUtility.SetDirty(content);}
                AssetDatabase.SaveAssets();
                Debug.Log("Imported Meshy arena with unchanged scene placement: "+path);
                return prefab;
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        public static void Validate(GameObject root) => ArenaLayoutValidator.Validate(root);
    }
}
