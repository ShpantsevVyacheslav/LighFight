using UnityEngine;
namespace LighFight.Runtime
{
    public sealed class GameContent : ScriptableObject
    {
        [System.Serializable] public struct FighterMetadata
        {
            public string id,nameRu,nameEn;
            public Texture2D portrait;
        }
        public FighterMetadata[] fighterMetadata=new FighterMetadata[0];
        public GameObject[] fighters;
        public GameObject[] arenas;
    }
}
