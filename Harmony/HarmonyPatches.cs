using AllowBuildInCaves.NavMeshEditing;
using Endnight.Environment;
using Endnight.Utilities;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Pathfinding;
using RedLoader;
using Sons.Ai;
using Sons.Ai.Vail;
using Sons.Animation.Mae;
using Sons.Animation.PlayerControl;
using Sons.Areas;
using Sons.Crafting;
using Sons.Cutscenes;
using Sons.Gameplay;
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
                if (testGroundHeight)
                {
                    testGroundHeight = false; // Disable ground height testing
                }
            }
        }


        [HarmonyPatch(typeof(NavMeshCutSetup), nameof(NavMeshCutSetup.TryCreateFromWorldPoints))]
        private static class TryCreateFromWorldPointsPatch
        {
            private static void Prefix(Transform cutTr, ref Il2CppStructArray<Vector3> points, NavmeshCut navCut, float extraHeight, float margin, ref bool checkTerrainDist, ref bool checkTerrainDistMinHeight, bool cutAddedGeo)
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

                if (checkTerrainDist)
                {
                    checkTerrainDist = false; // Disable ground height testing
                }

                if (checkTerrainDistMinHeight)
                {
                    checkTerrainDistMinHeight = false;
                }
            }
        }



        // Robby fixes lol

        [HarmonyPatch(typeof(VailActor), nameof(VailActor.SetupMoveToTarget))]
        private static class SetupMoveToTargetPatch
        {
            private static void Prefix(VailActor __instance, ref MoveToParams moveToParams, Transform targetTransform, bool targetIsStimuli)
            {
                if(__instance.TypeId == VailActorTypeId.Robby)
                {
                    Vector3 actorPos = __instance.transform.position;

                    if (actorPos.y < TerrainUtilities.GetTerrainHeight(actorPos) - 5f && moveToParams.StopIgnoreY == true) 
                    { 
                        moveToParams.StopIgnoreY = false; // Disable Y-axis ignoring for Robby
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Robby), nameof(Robby.FindChopTreeStimuli))]
        private static class RobbyFindChopTreeStimuli
        {
            private static bool Prefix(Robby __instance, ref Vector3 checkPos)
            {
                checkPos = TryFindBetterSearchPos(__instance, checkPos);

                return true; // Let the original method run.
            }
        }

        [HarmonyPatch(typeof(Robby), nameof(Robby.FindClearStumpStimuli))]
        private static class RobbyFindClearStumpStimuli
        {
            private static bool Prefix(Robby __instance, ref Vector3 checkPos)
            {
                checkPos = TryFindBetterSearchPos(__instance, checkPos);

                return true; // Let the original method run.
            }
        }

        [HarmonyPatch(typeof(Robby), nameof(Robby.FindBushClearStimuli))]
        private static class RobbyFindBushClearStimuli
        {
            private static bool Prefix(Robby __instance, ref Vector3 checkPos)
            {
                checkPos = TryFindBetterSearchPos(__instance, checkPos);

                return true; // Let the original method run.
            }
        }

        private static Vector3 TryFindBetterSearchPos(Robby __instance, Vector3 checkPos)
        {
            try
            {
                Vector3 actorPos = __instance._actor.Position();

                // First, get the actor's current node and graph index.
                // setup a constraint to only find navmeshgraphs, not recast graphs.
                NNConstraint nNConstraint = NNConstraint.None;
                //set the graph mask to ALL except the recast graph (graph 0)

                nNConstraint.graphMask = ~(1 << 0);
                nNConstraint.walkable = true; // Ensure we only consider walkable nodes.

                NNInfo nodeInfo = AstarPath.active.GetNearest(actorPos, nNConstraint);

                if (actorPos.y < TerrainUtilities.GetTerrainHeight(actorPos) - 5f)
                {
                    // Check if the node exists and is NOT on the surface graph (graph 0).
                    if (nodeInfo.node != null && nodeInfo.node.GraphIndex != 0)
                    {
                        int currentGraphIndex = (int)nodeInfo.node.GraphIndex;

                        // Use our new function to find the closest pre-defined exit.
                        if (TryFindBestCaveExit(actorPos, currentGraphIndex, out Vector3 bestExitPoint))
                        {
                            // Success! Reroute the search to the best exit.
                            checkPos = bestExitPoint;
                        }
                        else
                        {
                            RLog.Warning($"No mapped exits found or no valid path for cave with graph index {currentGraphIndex}.");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                RLog.Error($"RobbyCaveExitWaypointPatch failed: {e.Message}");
            }

            return checkPos;
        }


        private static readonly Dictionary<int, List<Vector3>> _caveExitPoints = new Dictionary<int, List<Vector3>>
        {
        // Graph 2 // Cave B
        { 2, new List<Vector3> 
            {
                new Vector3(-1118f, 131f, -161f),
                new Vector3(-1252f, 148f, -307f)
            }  
        },
        
        // Graph 3 // Cave C
        { 3, new List<Vector3> 
            { 
                new Vector3(-1118f, 131f, -161f), 
                new Vector3(-1252f, 148f, -307f) 
            } 
        },
        
        //  Graph 4 // Cave D
        { 4, new List<Vector3> { } },

    };

        public static bool TryFindBestCaveExit(Vector3 startPos, int graphIndex, out Vector3 bestExit)
        {
            bestExit = Vector3.zero;

            // 1. Check if we have defined any exits for this cave's graph index.
            if (!_caveExitPoints.TryGetValue(graphIndex, out List<Vector3> potentialExits))
            {
                // We haven't mapped this cave, so we can't find an exit.
                return false;
            }

            float shortestDistance = float.MaxValue;
            bool foundExit = false;

            foreach (Vector3 exitPoint in potentialExits)
            {
                // Calculate the simple, straight-line distance.
                float distance = Vector3.Distance(startPos, exitPoint);

                // If it's the shortest so far, save it.
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    bestExit = exitPoint;
                    foundExit = true;
                }
            }

            return foundExit;
        }
    }
}
