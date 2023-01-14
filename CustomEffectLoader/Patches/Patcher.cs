using HarmonyLib;
using System.Reflection;

namespace CustomEffectLoader
{
    public static class Patcher
    {
        private const string HarmonyId = "boformer.CustomEffectLoader";

        private static bool patched = false;

        public static void PatchAll()
        {
            if (patched) return;;

            patched = true;
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            WorkshopAssetUploadPanelPatch.Apply(harmony);
        }

        public static void UnpatchAll()
        {
            if (!patched) return;

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId); 
            WorkshopAssetUploadPanelPatch.Revert(harmony);
            patched = false;
        }
    }

}
