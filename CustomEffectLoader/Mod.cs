using Harmony;
using ICities;
using UnityEngine;

namespace CustomEffectLoader
{
    public class Mod : IUserMod
    {
        public string Name => "Custom Effect Loader";

        public string Description => "Allows asset creators to add custom light effects to their assets";

        private const string HarmonyId = "boformer.CustomEffectLoader";

        private HarmonyInstance _harmony;

        public void OnEnabled()
        {
            AssetEffectLoader.Ensure();

            if(_harmony == null)
            {
                _harmony = HarmonyInstance.Create(HarmonyId);
                _harmony.PatchAll(GetType().Assembly);
            }
        }

        public void OnDisabled()
        {
            AssetEffectLoader.Uninstall();

            if(_harmony != null)
            {
                _harmony.UnpatchAll(HarmonyId);
                _harmony = null;
            }

            foreach(var effect in EffectCollection.Effects)
            {
                if(effect is LightEffect)
                {
                    if(((LightEffect)effect).m_rotationSpeed != 0f)
                    {
                        Debug.Log(effect.name);
                    }
                }
            }
        }
    }
}
