﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ColossalFramework.Packaging;
using Harmony;
using UnityEngine;

namespace CustomEffectLoader
{
    /// <summary>
    /// Patch for the asset upload process. Add an extra tag for assets with custom animations
    /// </summary>
    public static class WorkshopAssetUploadPanelPatch
    {
        private const string AssetWorkshopTag = "Custom Effects";

        public static void Apply(HarmonyInstance harmony)
        {
            var prefix = typeof(WorkshopAssetUploadPanelPatch).GetMethod("UpdateItemPrefix");
            harmony.Patch(OriginalMethod, new HarmonyMethod(prefix), null, null);
            Debug.Log("WorkshopAssetUploadPanelPatch applied");
        }

        public static void Revert(HarmonyInstance harmony)
        {
            harmony.Unpatch(OriginalMethod, HarmonyPatchType.Prefix);
        }

        private static MethodInfo OriginalMethod => typeof(WorkshopAssetUploadPanel).GetMethod("UpdateItem", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void UpdateItemPrefix(Package.Asset ___m_TargetAsset, string ___m_ContentPath, ref string[] ___m_Tags)
        {
            if (___m_TargetAsset.type == UserAssetType.CustomAssetMetaData)
            {
                var effectsDefinitionFile = Path.Combine(___m_ContentPath, AssetEffectLoader.EffectsDefinitionFileName);
                if (File.Exists(effectsDefinitionFile))
                {
                    if (!___m_Tags.Contains(AssetWorkshopTag))
                    {
                        var tagList = new List<string>(___m_Tags);
                        tagList.Add(AssetWorkshopTag);
                        ___m_Tags = tagList.ToArray();
                    }
                }
                else
                {
                    if (___m_Tags.Contains(AssetWorkshopTag))
                    {
                        var tagList = new List<string>(___m_Tags);
                        tagList.Remove(AssetWorkshopTag);
                        ___m_Tags = tagList.ToArray();
                    }
                }
            }

        }
    }
}