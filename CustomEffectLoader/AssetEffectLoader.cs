using ColossalFramework.Packaging;
using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

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

    public class AssetEffectLoading : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            AssetEffectLoader.instance.OnLevelLoaded();
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();

            AssetEffectLoader.instance.OnLevelUnloading();
        }
    }
       
    public class AssetEffectLoader : ModSingleton<AssetEffectLoader>
    {
        public const string EffectsDefinitionFileName = "EffectsDefinition.xml";

        private GameObject _prefabCollection;

        private readonly List<EffectInfo> _effects = new List<EffectInfo>();

        private readonly List<string> _assetErrors = new List<string>();

        #region Lifecycle
        public void Awake()
        {
            Initialize();
          
            #if DEBUG
            DumpExampleDefFile();
            #endif
        }

        public void OnDestroy()
        {
            Reset();
        }

        public void OnPostMetaDataReady()
        {
            Initialize();

            LoadEffects();
        }

        public void OnLevelLoaded()
        {
            MaybeShowAssetErrorsModal();
        }

        public void OnLevelUnloading()
        {
            Reset();
        }
        #endregion

        private void Initialize()
        {
            if(_prefabCollection == null)
            {
                _prefabCollection = new GameObject("EffectPrefabCollection");
                _prefabCollection.transform.parent = transform;
                _prefabCollection.SetActive(false);
            }
        }
        
        private void Reset()
        {
            EffectCollection.DestroyEffects(_effects.ToArray());
            _effects.Clear();

            _assetErrors.Clear();

            if (_prefabCollection != null)
            {
                Destroy(_prefabCollection);
                _prefabCollection = null;
            }
        }

        private void LoadEffects()
        {
            try
            {
                var checkedPaths = new List<string>();

                var packages = PackageManager.allPackages.Where((Package p) => p.FilterAssets(UserAssetType.CustomAssetMetaData).Any()).ToArray();
                foreach(var package in packages)
                {
                    if (package == null) continue;

                    var crpPath = package.packagePath;
                    if (crpPath == null) continue;

                    var effectsDefPath = Path.Combine(Path.GetDirectoryName(crpPath) ?? "", EffectsDefinitionFileName);

                    // skip files which were already parsed
                    if (checkedPaths.Contains(effectsDefPath)) continue;
                    checkedPaths.Add(effectsDefPath);

                    if (!File.Exists(effectsDefPath)) continue;

                    EffectsDefinition effectsDef = null;

                    var xmlSerializer = new XmlSerializer(typeof(EffectsDefinition));
                    try
                    {
                        using (var streamReader = new StreamReader(effectsDefPath))
                        {
                            effectsDef = xmlSerializer.Deserialize(streamReader) as EffectsDefinition;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        _assetErrors.Add($"{package.packageName} - {e.Message}");
                        continue;
                    }

                    LoadEffects(package, effectsDef);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            EffectCollection.InitializeEffects(_effects.ToArray());
        }

        private void LoadEffects(Package package, EffectsDefinition effectsDef)
        {
            if (effectsDef?.Effects == null || effectsDef.Effects.Count == 0)
            {
                _assetErrors.Add($"{package.packageName} - effects list is null or empty.");
                return;
            }

            foreach (var effectDef in effectsDef.Effects)
            {
                if (effectDef?.Name == null)
                {
                    _assetErrors.Add($"{package.packageName} - Effect name missing.");
                    continue;
                }

                if (_effects.Any(e => e.name == effectDef.Name) || EffectCollection.FindEffect(effectDef.Name) != null)
                {
                    //_assetErrors.Add($"{package.packageName} - {effectDef.Name} - Duplicate effect name!");
                    continue;
                }

                if (effectDef is EffectsDefinition.LightEffect lightEffectDef)
                {
                    LoadLightEffect(package, lightEffectDef);
                }
                // TODO add support for other asset types!
            }
        }

        private void LoadLightEffect(Package package, EffectsDefinition.LightEffect effectDef)
        {
            if(effectDef.VariationColors == null || effectDef.VariationColors.Count == 0)
            {
                _assetErrors.Add($"{package.packageName} - {effectDef.Name} - no color variation defined");
                return;
            }

            LightType lightType;
            try
            {
                lightType = (LightType)Enum.Parse(typeof(LightType), effectDef.Type, true);
            } 
            catch
            {
                _assetErrors.Add($"{package.packageName} - {effectDef.Name} - unknown light type \"${effectDef.Type}\"");
                return;
            }

            LightEffect.BlinkType blinkType;
            try
            {
                blinkType = (LightEffect.BlinkType)Enum.Parse(typeof(LightEffect.BlinkType), effectDef.BlinkType ?? "None", true);
            }
            catch
            {
                _assetErrors.Add($"{package.packageName} - {effectDef.Name} - unknown blink type \"${effectDef.BlinkType}\"");
                return;
            }

            var effectGo = new GameObject(effectDef.Name);
            effectGo.transform.parent = _prefabCollection.transform;
            effectGo.SetActive(false);

            var light = effectGo.AddComponent<Light>();
            light.intensity = effectDef.Intensity;
            light.range = effectDef.Range;
            light.spotAngle = effectDef.SpotAngle;
            light.color = effectDef.VariationColors[0].ToUnityColor();
            light.type = lightType;

            LightEffect lightEffect = effectGo.gameObject.AddComponent<LightEffect>();

            lightEffect.m_batchedLight = effectDef.BatchedLight;
            if(effectDef.BatchedLight)
            {
                effectGo.layer = RenderManager.instance.lightSystem.m_lightLayer;
            }

            lightEffect.m_fadeStartDistance = effectDef.FadeStartDistance;
            lightEffect.m_fadeEndDistance = effectDef.FadeEndDistance;
            lightEffect.m_offRange = new Vector2(effectDef.OffMin, effectDef.OffMax);
            lightEffect.m_spotLeaking = effectDef.SpotLeaking;
            lightEffect.m_renderDuration = effectDef.RenderDuration;

            lightEffect.m_blinkType = blinkType;
            lightEffect.m_rotationSpeed = effectDef.RotationSpeed;
            lightEffect.m_rotationAxis = new Vector3(effectDef.RotationAxisX, effectDef.RotationAxisY, effectDef.RotationAxisZ);

            if (effectDef.VariationColors.Count > 0)
            {
                lightEffect.m_variationColors = effectDef.VariationColors.Select(c => c.ToUnityColor()).ToArray();
            }

            lightEffect.InitializeEffect();
                       
            _effects.Add(lightEffect);
        }

        public bool IsCustomEffect(EffectInfo effect)
        {
            return _effects.Contains(effect);
        }

        private void MaybeShowAssetErrorsModal()
        {
            if (_assetErrors.Count == 0) return;

            var errorMessage = "The effects of the following assets failed to load. "
                               + "Please report the error to the asset creator:\n\n"
                               + $"{string.Join("\n\n", _assetErrors.ToArray())}";

            UIView.library.ShowModal<ExceptionPanel>("ExceptionPanel")
                .SetMessage("Custom Effect Loader", errorMessage, false);
        }

        #if DEBUG
        private void DumpExampleDefFile()
        {
            var def = new EffectsDefinition
            {
                Effects = new List<EffectsDefinition.Effect>
                {
                    new EffectsDefinition.LightEffect
                    {
                        Name = "MY CUSTOM EFFECT",
                        Type = "Spot",
                        VariationColors = new List<EffectsDefinition.Color>
                        {
                            new EffectsDefinition.Color
                            {
                                R = 255,
                                G = 255,
                                B = 255,
                                A = 255
                            }
                        }
                    }
                }
            };
            try
            {
                var xmlSerializer = new XmlSerializer(typeof(EffectsDefinition));
                using (var streamWriter = new StreamWriter("EffectsDefinition.xml"))
                {
                    xmlSerializer.Serialize(streamWriter, def);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        #endif
    }
}
