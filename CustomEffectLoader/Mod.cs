using CitiesHarmony.API;
using ICities;

namespace CustomEffectLoader
{
    public class Mod : IUserMod
    {
        public string Name => "Custom Effect Loader";

        public string Description => "Allows asset creators to add custom light effects to their assets";

        public void OnEnabled()
        {
            AssetEffectLoader.Ensure();
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
        }

        public void OnDisabled()
        {
            AssetEffectLoader.Uninstall();
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
        }
    }
}
