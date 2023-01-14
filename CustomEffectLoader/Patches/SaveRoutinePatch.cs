using HarmonyLib;

namespace CustomEffectLoader.Patches
{
    // Whole point of this logic is to strip custom effects from assets on save and reattach them on load.
    // That way the asset remains compatible even when the mod is disabled!

    // Saving in asset editor
    [HarmonyPatch(typeof(SaveAssetPanel), "SaveRoutine")]
    public static class SaveRoutinePatch
    {
        public static void Prefix(string mapName)
        {
            AssetData.OnPreSaveAsset(mapName);
        }
    }
}
