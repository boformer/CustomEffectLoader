using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace CustomEffectLoader.Patches
{
    [HarmonyPatch(typeof(LightEffect), "GetBlinkVector")]
    public static class GetBlinkVectorPatch
    {
        public static bool Prefix(LightEffect.BlinkType type, ref Vector4 __result)
        {
            if(type < 0)
            {
                int index = -(int)type - 1;
                __result = AssetEffectLoader.customBlinkTypes.Count > index 
                    ? AssetEffectLoader.customBlinkTypes[index].blinkVector 
                    : new Vector4(0f, -1f, 2f, 1f);
                return false;
            }
            return true;
        }
    }
}
