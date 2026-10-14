using System;
using UnityEngine;
namespace LighFight.Runtime
{
    public sealed class GameContent : ScriptableObject
    {
        [Serializable] public struct FighterMetadata
        {
            public string id,nameRu,nameEn;
            public Texture2D portrait;
        }
        public FighterMetadata[] fighterMetadata=new FighterMetadata[0];
        // Inline catalogs remain supported for local reviews/tests. Delivery catalogs contain addresses only.
        public GameObject[] fighters;
        public GameObject[] arenas;
        public bool streamed;
        public string[] fighterAddresses=new string[0],arenaAddresses=new string[0];
        public int FighterCount=>streamed?(fighterAddresses?.Length??0):(fighters?.Length??0);
        public int ArenaCount=>streamed?(arenaAddresses?.Length??0):(arenas?.Length??0);
        public bool ValidateCatalog(out string error)
        {
            error=null;
            if(FighterCount<1||ArenaCount<1){error="empty_content";return false;}
            if(streamed){
                if((fighters?.Length??0)!=0||(arenas?.Length??0)!=0){error="stream_catalog_has_inline_assets";return false;}
                foreach(var key in fighterAddresses)if(string.IsNullOrWhiteSpace(key)){error="invalid_fighter_address";return false;}
                foreach(var key in arenaAddresses)if(string.IsNullOrWhiteSpace(key)){error="invalid_arena_address";return false;}
            }
            else{
                foreach(var fighter in fighters)if(!fighter||!fighter.GetComponent<FighterView>()||!fighter.GetComponent<FighterView>().IsReady){error="invalid_fighter";return false;}
                foreach(var arena in arenas)if(!arena||!arena.GetComponent<OfficeArena>()){error="invalid_arena";return false;}
            }
            return true;
        }
    }
}
