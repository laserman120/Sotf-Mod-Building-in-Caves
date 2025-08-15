using AllowBuildInCaves.NavMeshEditing;
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
using UnityEngine.UIElements;

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
                //Check if we should force replace the method or not:

                NNInfo nodeInfo = AstarPath.active.GetNearest(checkPoint);

                if (nodeInfo == null)
                {
                    return true;
                }

                if (nodeInfo.node == null)
                {
                    return true;
                }

                if (nodeInfo.node.GraphIndex == 0)
                {
                    //is the main recast graph, return out and let the original method run
                    return true;
                }

                bool flag;
                Vector3 closestNavMeshPoint = AiUtilities.GetClosestNavMeshPoint(checkPoint, AllowBuildInCaves.graphMask, out flag);
                Vector3 testLink = new Vector3();
                NNInfo nearestNode = Sons.Ai.AiUtilities.GetNearestNode(checkPoint, AllowBuildInCaves.graphMask, false);

                bool hasNoNode = nearestNode.node == null;

                if (hasNoNode)
                {
                    testLink = checkPoint;
                }
                else
                {
                    testLink = nearestNode.position;
                }

                if (testLink == new Vector3())
                {
                    //Failed to find a valid node, return out
                    __result = false;
                    return false;
                }

                bool traceSucceeded = false;
                bool isLockedDoor = false;

                traceSucceeded = __instance.CheckPhysicsTrace(linkPoint, closestNavMeshPoint, false, out isLockedDoor);


                __instance._navLinkTests.Add(new NavMeshCustomMeshAdd.NavLinkLocations(linkPoint, checkPoint, testLink, true, traceSucceeded, isLockedDoor, false));
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
                NNInfo nodeInfo = AstarPath.active.GetNearest(cutTr.position);

                if (nodeInfo == null)
                {
                    return;
                }

                if (nodeInfo.node == null)
                {
                    return;
                }

                if (nodeInfo.node.GraphIndex == 0)
                {
                    return;
                }

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

            /*private static void Postfix(Transform cutTr, ref Il2CppStructArray<Vector3> points, NavmeshCut navCut, float extraHeight, float margin, ref bool checkTerrainDist, ref bool checkTerrainDistMinHeight, bool cutAddedGeo)
            {
                if(navCut != null)
                {
                    float x = navCut.contourTransformationMatrix.m03;
                    float y = navCut.contourTransformationMatrix.m13;
                    float z = navCut.contourTransformationMatrix.m23;

                    Vector3 position = new Vector3(x, y, z);



                    NNInfo nodeInfo = AstarPath.active.GetNearest(position);

                    if(nodeInfo == null)
                    {
                        RLog.Msg("Failed to fetch nodeInfo");
                        return;
                    }

                    if(nodeInfo.node == null)
                    {
                        RLog.Msg("Failed to fetch nodeInfo.node");
                        return;
                    }

                    if(nodeInfo.node.GraphIndex == 0)
                    {
                        RLog.Msg("Node Graph Index is 0, this is not a valid NavMesh node");
                        return;
                    }

                    uint foundGraphIndex = nodeInfo.node.GraphIndex;

                    RLog.Msg($"Found Graph Index: {foundGraphIndex}");
                    if (foundGraphIndex > 0)
                    {
                        int finalGraphIndex = (int)(foundGraphIndex - 1);
                        RLog.Msg($"Final Graph Index: {finalGraphIndex}");

                        float usedGraphScale = ReplaceExistingMeshes.allNavMeshGraphs[finalGraphIndex].scale;

                        Vector2 currentSize = navCut.rectangleSize;
                        RLog.Msg("Found currentSize: " + currentSize.ToString());

                        Vector2 newSize = new Vector2(currentSize.x / usedGraphScale, currentSize.y / usedGraphScale);

                        RLog.Msg("Calculated newSize: " + newSize.ToString());

                        navCut.rectangleSize = newSize;
                    }

                    



                }
            }*/
        }
    }
}
