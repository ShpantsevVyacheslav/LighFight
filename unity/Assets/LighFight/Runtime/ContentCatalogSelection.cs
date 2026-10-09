using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
namespace LighFight.Runtime
{
    public static class ContentCatalogSelection
    {
        // Addressables merges matching keys across locators. Remove older LighFight
        // catalogs so an update cannot resolve the previous GameContent asset.
        public static void Activate(IResourceLocator current)
        {
            foreach(var locator in Addressables.ResourceLocators.ToArray())
                if(locator!=current && locator.Locate("lighfight-content",typeof(GameContent),out _))
                    Addressables.RemoveResourceLocator(locator);
        }
    }
}
