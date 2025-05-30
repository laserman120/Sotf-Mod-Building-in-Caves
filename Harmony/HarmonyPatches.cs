using HarmonyLib;
using Sons.Animation.PlayerControl;
using Sons.Areas;
using Sons.Crafting;
using Sons.Cutscenes;
using Sons.Gameplay.GPS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllowBuildInCaves.Harmony
{
    internal class HarmonyPatches
    {
        //test add patch to cave enter animation
        [HarmonyPatch(typeof(CaveEntranceCutscene), "OnEnterCaveEntrance")]
        private static class EnterCutscenePatch
        {
            private static void Prefix()
            {
                AllowBuildInCaves.RemoveItemsOnEnter();
            }
        }

        [HarmonyPatch(typeof(CaveEntranceCutscene), "FinalizeSequence")]
        private static class FinishCutscenePatch
        {
            private static void Postfix()
            {
                AllowBuildInCaves.AddItemsOnExit();
            }
        }


        //Climb up hatch
        [HarmonyPatch(typeof(ClimbUpHatchTrigger), "ClimbInputReceived")]
        private static class EnterHatchUpCutscenePatch
        {
            private static void Prefix()
            {
                AllowBuildInCaves.RemoveItemsOnEnter();
                AllowBuildInCaves.SnowFix(true, false);
            }
        }

        [HarmonyPatch(typeof(Cutscene), "Cleanup")]
        private static class FinishHatchUpCutscenePatch
        {
            private static void Postfix()
            {
                AllowBuildInCaves.AddItemsOnExit();
            }
        }

        //Main IsInCaves Patch

        [HarmonyPatch(typeof(CaveEntranceManager), "OnCaveEnter")]
        private static class EnterPatch
        {
            private static void Postfix()
            {
                IsInCavesStateManager.EnterCave();
                AllowBuildInCaves.BlueFix();
                AllowBuildInCaves.SnowFix(false, false);
            }
        }

        [HarmonyPatch(typeof(CaveEntranceManager), "OnCaveExit")]
        private static class ExitPatch
        {
            //private static void Prefix()
            //{
            //    CaveEntranceManager._isInCaves = true;
            //}
            private static void Postfix()
            {
                IsInCavesStateManager.ExitCave();
                AllowBuildInCaves.UndoBlueFix();
                AllowBuildInCaves.SnowFix(true, false);
                //FixCollectUI();
            }
        }

        //Gps Tracker Fix
        [HarmonyPatch(typeof(GPSTrackerSystem), "LateUpdate")]
        private static class GPSTrackerCaveFix
        {
            private static void Postfix(GPSTrackerSystem __instance)
            {
                if (IsInCavesStateManager.GPSShouldLoseSignal == false)
                {
                    __instance._trackerSignalLost = false;
                    __instance._signalLost.SetActive(__instance._trackerSignalLost);
                    __instance._screenStatic.SetActive(!__instance._trackerSignalLost);
                    __instance._playerArrow.gameObject.SetActive(!__instance._trackerSignalLost);
                }
            }
        }
        public static void RefreshRequiredItemsUI()
        {
            AllowBuildInCaves.CraftingSystem.RefreshRequiredItemsUi();
        }

        public static void RefreshRequiredItemsUiInCave()
        {
            HudGui._instance.ClearAllRequiredCollectionCounts();
        }


        [HarmonyPatch(typeof(HudGui), "ClearAllRequiredCollectionCounts")]
        private static class HudGuiClearRequiredPatch
        {
            private static bool Prefix()
            {
                if (IsInCavesStateManager.ItemCollectUIFix) { return true; }
                if (IsInCavesStateManager.IsInCaves)
                {
                    AllowBuildInCaves.CraftingSystem.UpdateRequiredCountUIForAllItems();
                    return false;

                };
                return true;
            }
        }

        public static void UpdateRequiredCountUi()
        {
            if (IsInCavesStateManager.IsInCaves && !IsInCavesStateManager.ItemCollectUIFix)
            {
                AllowBuildInCaves.CraftingSystem.UpdateRequiredCountUIForAllItems();
            }
        }
    }
}
