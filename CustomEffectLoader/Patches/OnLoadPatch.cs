using System;
using ColossalFramework.UI;
using HarmonyLib;
using UnityEngine;

namespace CustomEffectLoader.Patches
{
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
}
