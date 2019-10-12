using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ColossalFramework.Packaging;
using ColossalFramework.UI;
using Harmony;
using ICities;
using UnityEngine;

namespace CustomEffectLoader
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

    // Loading in asset editor
    [HarmonyPatch(typeof(LoadAssetPanel), "OnLoad")]
    public static class OnLoadPatch
    {
        public static void Postfix(LoadAssetPanel __instance, UIListBox ___m_SaveList)
        {
            try
            {
                // Taken from LoadAssetPanel.OnLoad
                var selectedIndex = ___m_SaveList.selectedIndex;
                var getListingMetaDataMethod = typeof(LoadSavePanelBase<CustomAssetMetaData>).GetMethod(
                    "GetListingMetaData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var listingMetaData = (CustomAssetMetaData)getListingMetaDataMethod.Invoke(__instance, new object[] { selectedIndex });


                // Taken from LoadingManager.LoadCustomContent
                if (listingMetaData.userDataRef != null)
                {
                    AssetDataWrapper.UserAssetData userAssetData = listingMetaData.userDataRef.Instantiate() as AssetDataWrapper.UserAssetData;
                    if (userAssetData == null)
                    {
                        userAssetData = new AssetDataWrapper.UserAssetData();
                    }
                    AssetData.OnAssetLoadedImpl(listingMetaData.name, ToolsModifierControl.toolController.m_editPrefabInfo, userAssetData.Data);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    public class AssetData : AssetDataExtensionBase
    {
        private const string DataKey = "CustomEffectLoader";

        private static Dictionary<PropInfo, NamedEffects<PropInfo.Effect>> m_propEffects = new Dictionary<PropInfo, NamedEffects<PropInfo.Effect>>();
        private static Dictionary<VehicleInfo, NamedEffects<VehicleInfo.Effect>> m_vehicleEffects = new Dictionary<VehicleInfo, NamedEffects<VehicleInfo.Effect>>();

        private struct NamedEffects<T> where T : struct
        {
            public string m_assetName;
            public T[] m_effects;
        }

        public override void OnReleased()
        {
            base.OnReleased();
            m_propEffects.Clear();
            m_vehicleEffects.Clear();
        }

        #region Asset Saving
        public static void OnPreSaveAsset(string assetName)
        {
            m_propEffects.Clear();
            m_vehicleEffects.Clear();

            // strip custom effects on save to make asset work without the mod
            var prefab = ToolsModifierControl.toolController.m_editPrefabInfo;
            if (prefab is PropInfo propPrefab)
            {
                StripCustomEffects($"{assetName}_Data", propPrefab);

                if (propPrefab.m_variations != null)
                {
                    for (int v = 0; v < propPrefab.m_variations.Length; v++)
                    {
                        PropInfo.Variation variation = propPrefab.m_variations[v];
                        StripCustomEffects($"{assetName}_variation_${v}", variation.m_prop);
                    }
                }
            }
            else if (prefab is VehicleInfo vehiclePrefab)
            {
                StripCustomEffects($"{assetName}_Data", vehiclePrefab);

                if (vehiclePrefab.m_trailers != null && vehiclePrefab.m_trailers.Length > 0)
                {
                    var trailer = vehiclePrefab.m_trailers[0];
                    var trailerAssetName = GetTrailerAssetName(trailer);
                    StripCustomEffects(trailerAssetName, trailer.m_info);
                }
            }
        }

        private static void StripCustomEffects(string assetName, PropInfo prefab)
        {
            if (prefab == null || prefab.m_effects == null) return;

            foreach (var propEffect in prefab.m_effects)
            {
                if (AssetEffectLoader.instance.IsCustomEffect(propEffect.m_effect))
                {
                    m_propEffects[prefab] = new NamedEffects<PropInfo.Effect> {
                        m_assetName = assetName,
                        m_effects = prefab.m_effects
                    };
                    prefab.m_effects = new PropInfo.Effect[0];
                    Debug.Log($"{assetName}: Stripped custom prop effects!");
                    break;
                }
            }
        }

        private static void StripCustomEffects(string assetName, VehicleInfo prefab)
        {
            if (prefab == null || prefab.m_effects == null) return;

            foreach (var vehicleEffect in prefab.m_effects)
            {
                if (AssetEffectLoader.instance.IsCustomEffect(vehicleEffect.m_effect))
                {
                    m_vehicleEffects[prefab] = new NamedEffects<VehicleInfo.Effect>
                    {
                        m_assetName = assetName,
                        m_effects = prefab.m_effects
                    };
                    prefab.m_effects = new VehicleInfo.Effect[0];
                    Debug.Log($"{assetName}: Stripped custom vehicle effects!");
                    break;
                }
            }
        }

        private static string GetTrailerAssetName(VehicleInfo.VehicleTrailer trailer)
        {
            var trailerObject = trailer.m_info.gameObject;
            var num = trailerObject.name.LastIndexOf(".");
            return ((num >= 0) ? trailerObject.name.Remove(0, num + 1) : trailerObject.name);
        }

        public override void OnAssetSaved(string name, object asset, out Dictionary<string, byte[]> userData)
        {
            if(m_propEffects.Count == 0 && m_vehicleEffects.Count == 0)
            {
                userData = null;
                return;
            }

            Debug.Log($"Saving custom effects for {name}");

            using (var stream = new MemoryStream())
            {
                using (var writer = new PackageWriter(stream))
                {
                    writer.Write(m_propEffects.Count);
                    foreach (var value in m_propEffects.Values)
                    {
                        writer.Write(value.m_assetName);
                        writer.Write(value.m_effects.Length);

                        foreach (var propEffect in value.m_effects)
                        {
                            writer.Write(propEffect.m_effect.name);
                            writer.Write(propEffect.m_position);
                            writer.Write(propEffect.m_direction);
                        }

                        Debug.Log($"Saved {value.m_effects.Length} effects for {value.m_assetName}");
                    }

                    writer.Write(m_vehicleEffects.Count);
                    foreach (var value in m_vehicleEffects.Values)
                    {
                        writer.Write(value.m_assetName);
                        writer.Write(value.m_effects.Length);

                        foreach (var vehicleEffect in value.m_effects)
                        {
                            writer.Write(vehicleEffect.m_effect.name);
                            writer.Write((int)vehicleEffect.m_vehicleFlagsRequired);
                            writer.Write((int)vehicleEffect.m_vehicleFlagsForbidden);
                            writer.Write((int)vehicleEffect.m_parkedFlagsRequired);
                            writer.Write((int)vehicleEffect.m_parkedFlagsForbidden);
                        }

                        Debug.Log($"Saved {value.m_effects.Length} effects for {value.m_assetName}");
                    }
                }

                userData = new Dictionary<string, byte[]>
                {
                    { DataKey, stream.ToArray() }
                };
            }

            // Reapply stripped effects!
            foreach (var pair in m_propEffects)
            {
                pair.Key.m_effects = pair.Value.m_effects;
            }
            foreach (var pair in m_vehicleEffects)
            {
                pair.Key.m_effects = pair.Value.m_effects;
            }

            m_propEffects.Clear();
            m_vehicleEffects.Clear();
        }
        #endregion

        #region Asset Loading
        public override void OnAssetLoaded(string name, object asset, Dictionary<string, byte[]> userData)
        {
            OnAssetLoadedImpl(name, asset, userData);
        }

        public static void OnAssetLoadedImpl(string name, object asset, Dictionary<string, byte[]> userData)
        {
            if (!userData.TryGetValue(DataKey, out var bytes))
            {
                return;
            }

            Debug.Log($"Found custom effect data for {name}");

            var propEffectsDict = new Dictionary<string, PropInfo.Effect[]>();
            var vehicleEffectsDict = new Dictionary<string, VehicleInfo.Effect[]>();

            using (var stream = new MemoryStream(bytes))
            {
                using (var reader = new PackageReader(stream))
                {
                    var propEffectsDictCount = reader.ReadInt32();
                    for (var p = 0; p < propEffectsDictCount; p++)
                    {
                        var prefabName = reader.ReadString();
                        var effectCount = reader.ReadInt32();

                        var propEffects = new List<PropInfo.Effect>(effectCount);
                        for (var e = 0; e < effectCount; e++)
                        {
                            var effectName = reader.ReadString();

                            var propEffect = new PropInfo.Effect
                            {
                                m_effect = EffectCollection.FindEffect(effectName),
                                m_position = reader.ReadVector3(),
                                m_direction = reader.ReadVector3()
                            };

                            if (propEffect.m_effect != null)
                            {
                                propEffects.Add(propEffect);
                            }
                            else
                            {
                                Debug.LogError($"Effect \"{effectName}\" not found while loading {prefabName}!");
                            }
                        }

                        propEffectsDict[prefabName] = propEffects.ToArray();
                    }

                    var vehicleEffectsDictCount = reader.ReadInt32();
                    for (var v = 0; v < vehicleEffectsDictCount; v++)
                    {
                        var prefabName = reader.ReadString();
                        var effectCount = reader.ReadInt32();

                        var propEffects = new List<VehicleInfo.Effect>(effectCount);
                        for (var e = 0; e < effectCount; e++)
                        {
                            var effectName = reader.ReadString();

                            var vehicleEffect = new VehicleInfo.Effect
                            {
                                m_effect = EffectCollection.FindEffect(effectName),
                                m_vehicleFlagsRequired = (Vehicle.Flags)reader.ReadInt32(),
                                m_vehicleFlagsForbidden = (Vehicle.Flags)reader.ReadInt32(),
                                m_parkedFlagsRequired = (VehicleParked.Flags)reader.ReadInt32(),
                                m_parkedFlagsForbidden = (VehicleParked.Flags)reader.ReadInt32()
                            };

                            if (vehicleEffect.m_effect != null)
                            {
                                propEffects.Add(vehicleEffect);
                            }
                            else
                            {
                                Debug.LogError($"Effect \"{effectName}\" not found while loading {prefabName}!");
                            }
                        }

                        vehicleEffectsDict[prefabName] = propEffects.ToArray();
                    }
                }
            }

            if (asset is PropInfo propPrefab)
            {
                ApplyCustomEffects(propPrefab, propEffectsDict);

                if (propPrefab.m_variations != null)
                {
                    foreach (var variation in propPrefab.m_variations)
                    {
                        ApplyCustomEffects(variation.m_prop, propEffectsDict);
                    }
                }
            }
            else if (asset is VehicleInfo vehiclePrefab)
            {
                ApplyCustomEffects(vehiclePrefab, vehicleEffectsDict);

                if (vehiclePrefab.m_trailers != null)
                {
                    foreach (var trailer in vehiclePrefab.m_trailers)
                    {
                        ApplyCustomEffects(trailer.m_info, vehicleEffectsDict);
                    }
                }
            }
        }

        private static void ApplyCustomEffects(PropInfo prefab, Dictionary<string, PropInfo.Effect[]> propEffectsDict)
        {
            var prefabSaveName = GetPrefabSaveName(prefab.name);

            if(propEffectsDict.TryGetValue(prefabSaveName, out var propEffects))
            {
                prefab.m_effects = propEffects;
                ReinitializePrefab(prefab);
                Debug.Log($"Applied {propEffects.Length} effects to {prefab.name}");
            }
        }

        private static void ReinitializePrefab(PropInfo prefab)
        {
            // Taken from PropInfo.InitializePrefab()
            prefab.RefreshLevelOfDetail();
            prefab.m_effectLayer = -1;
            if (prefab.m_effects != null)
            {
                prefab.m_hasEffects = (prefab.m_effects.Length != 0);
                for (int i = 0; i < prefab.m_effects.Length; i++)
                {
                    if (prefab.m_effects[i].m_effect != null)
                    {
                        prefab.m_effects[i].m_effect.InitializeEffect();
                        int num = prefab.m_effects[i].m_effect.GroupLayer();
                        if (num != -1)
                        {
                            prefab.m_effectLayer = num;
                        }
                    }
                }
            }
            else
            {
                prefab.m_hasEffects = false;
            }
        }

        private static void ApplyCustomEffects(VehicleInfo prefab, Dictionary<string, VehicleInfo.Effect[]> vehicleEffectsDict)
        {
            var prefabSaveName = GetPrefabSaveName(prefab.name);

            if (vehicleEffectsDict.TryGetValue(prefabSaveName, out var propEffects))
            {
                prefab.m_effects = propEffects;
                ReinitializePrefab(prefab);
                Debug.Log($"Applied {propEffects.Length} effects to {prefab.name}");
            }
        }

        private static void ReinitializePrefab(VehicleInfo prefab)
        {
            // Taken from VehicleInfo.InitializePrefab()
            if (prefab.m_effects != null)
            {
                for (int j = 0; j < prefab.m_effects.Length; j++)
                {
                    if (prefab.m_effects[j].m_effect != null)
                    {
                        prefab.m_effects[j].m_effect.InitializeEffect();
                    }
                }
            }
        }

        private static string GetPrefabSaveName(string prefabName)
        {
            var match = Regex.Match(prefabName, @"^.+?\.(.+)$");
            return match.Success ? match.Groups[1].Value : prefabName;
        }
        #endregion
    }
}
