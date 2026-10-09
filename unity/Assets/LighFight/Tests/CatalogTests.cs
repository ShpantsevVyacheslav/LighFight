using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using LighFight.Runtime;
public class CatalogTests
{
    private sealed class Locator:IResourceLocator
    {
        private readonly string key;
        public Locator(string id,string key){LocatorId=id;this.key=key;}
        public string LocatorId {get;}
        public IEnumerable<object> Keys=>new object[]{key};
        public IEnumerable<IResourceLocation> AllLocations=>new[]{new ResourceLocationBase(key,LocatorId,"provider",typeof(GameContent))};
        public bool Locate(object request,Type type,out IList<IResourceLocation> locations)
        { locations=null;if(!Equals(request,key))return false;locations=new[]{new ResourceLocationBase(key,LocatorId,"provider",typeof(GameContent))};return true; }
    }
    [Test] public void NewCatalogReplacesOnlyPreviousFightCatalog()
    {
        var old=new Locator("old","lighfight-content");var current=new Locator("new","lighfight-content");var other=new Locator("other","other-game");
        Addressables.AddResourceLocator(old);Addressables.AddResourceLocator(current);Addressables.AddResourceLocator(other);
        try
        { ContentCatalogSelection.Activate(current);Assert.IsFalse(Addressables.ResourceLocators.Contains(old));Assert.IsTrue(Addressables.ResourceLocators.Contains(current));Assert.IsTrue(Addressables.ResourceLocators.Contains(other)); }
        finally {Addressables.RemoveResourceLocator(old);Addressables.RemoveResourceLocator(current);Addressables.RemoveResourceLocator(other);}
    }
}
