using HarmonyLib;

namespace CustomEffectLoader
{
    [HarmonyPatch(typeof(LoadingManager), "MetaDataLoaded")]
    public static class MetaDataLoadedPatch
    {
        public static void Postfix()
        {
            AssetEffectLoader.instance.OnPostMetaDataReady();
        }
    }
}
