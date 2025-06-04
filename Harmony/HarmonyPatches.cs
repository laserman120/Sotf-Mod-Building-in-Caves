using Endnight.Utilities;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Pathfinding;
using RedLoader;
using Sons.Ai;
using Sons.Ai.Vail;
using Sons.Animation.PlayerControl;
using Sons.Areas;
using Sons.Crafting;
using Sons.Cutscenes;
using Sons.Gameplay.GPS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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


        //SCUFFED TESTING
        
        [HarmonyPatch(typeof(NavMeshCustomMeshAdd), nameof(NavMeshCustomMeshAdd.TryAddNavLinkToTerrain))]
        private static class NavMeshCustomMeshAddPatch
        {
            private static bool Prefix(NavMeshCustomMeshAdd __instance, ref bool __result, Vector3 linkPoint, Vector3 checkPoint)
            {
                bool flag;
                Vector3 closestNavMeshPoint = AiUtilities.GetClosestNavMeshPoint(checkPoint, 1, out flag);
                Vector3 testLink = new Vector3();
                bool isLinked = __instance.TestLinkToNavGraph(checkPoint, out testLink, false);
                if (testLink == new Vector3())
                {
                    RLog.Msg("Failed to find valid testLink for NavMeshCustomMeshAddPatch");
                    __result = false;
                    return false;
                }
                if (!isLinked)
                {
                    RLog.Msg("Failed to link NavMeshCustomMeshAddPatch");
                    __result = false;
                    return false;
                }
                bool traceSucceeded = false;
                bool isLockedDoor = false;
                if (isLinked)
                {
                    traceSucceeded = __instance.CheckPhysicsTrace(linkPoint, closestNavMeshPoint, false, out isLockedDoor);
                }

                __instance._navLinkTests.Add(new NavMeshCustomMeshAdd.NavLinkLocations(linkPoint, checkPoint, testLink, isLinked, traceSucceeded, isLockedDoor, false));
                if (!flag || Vector3ExtensionMethods.DistanceWithYMargin(checkPoint, closestNavMeshPoint, 0.25f) > __instance._navLinkMaxDistance)
                {
                    __result = false;
                    return false;
                }
                GameObject gameObject = new GameObject("start");
                NavAddLink navAddLink = gameObject.AddComponent<NavAddLink>();
                gameObject.transform.parent = __instance.transform;
                gameObject.transform.position = linkPoint;
                Transform transform = new GameObject("target").transform;
                transform.parent = __instance.transform;
                transform.position = closestNavMeshPoint;
                navAddLink.end = transform;
                __instance._navAddLinks.Add(navAddLink);
                __result = true;
                return false;
            }
        }

        
        //NavMeshCutSetup.TryCreateFromWorldPoints
        [HarmonyPatch(typeof(NavMeshCustomMeshAdd), nameof(NavMeshCustomMeshAdd.TestLinkToNavGraph))]
        private static class TestLinkToNavGraphPatch
        {
            private static void Prefix(Vector3 checkPoint, ref Vector3 closestNavPoint, ref bool testGroundHeight)
            {
                RLog.Msg("Ran Patch for TestLinkToNavGraphPatch");
                if (testGroundHeight)
                {
                    testGroundHeight = false; // Disable ground height testing
                    RLog.Msg("testGroundHeight set to false in TestLinkToNavGraphPatch");
                }
            }
        }

        
        [HarmonyPatch(typeof(NavMeshCutSetup), nameof(NavMeshCutSetup.TryCreateFromWorldPoints))]
        private static class TryCreateFromWorldPointsPatch
        {
            private static void Prefix(Transform cutTr,ref Il2CppStructArray<Vector3> points, NavmeshCut navCut, float extraHeight, float margin,ref bool checkTerrainDist,ref bool checkTerrainDistMinHeight,bool cutAddedGeo)
            {
                RLog.Msg("Ran Patch for TryCreateFromWorldPoints");
                if (checkTerrainDist)
                {
                    checkTerrainDist = false; // Disable ground height testing
                    RLog.Msg("checkTerrainDist set to false in TryCreateFromWorldPoints");
                }

                if (checkTerrainDistMinHeight)
                {
                    checkTerrainDistMinHeight = false;
                    RLog.Msg("checkTerrainDistMinHeight set to false in TryCreateFromWorldPoints");
                }
            }
        }
    }
}
