using AllowBuildInCaves.Debug;
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
using SonsSdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TheForest;
using TheForest.Utils;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
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
                if (!Config.DontOpenCaves.Value && Config.AllowActorsInCaves.Value)
                {
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
                } else
                {
                    return true;
                }
            }
        }


        //NavMeshCutSetup.TryCreateFromWorldPoints
        [HarmonyPatch(typeof(NavMeshCustomMeshAdd), nameof(NavMeshCustomMeshAdd.TestLinkToNavGraph))]
        private static class TestLinkToNavGraphPatch
        {
            private static void Prefix(Vector3 checkPoint, ref Vector3 closestNavPoint, ref bool testGroundHeight)
            {
                if (!Config.DontOpenCaves.Value && Config.AllowActorsInCaves.Value)
                {
                    if (testGroundHeight)
                    {
                        testGroundHeight = false; // Disable ground height testing
                    }
                }
            }
        }


        [HarmonyPatch(typeof(NavMeshCutSetup), nameof(NavMeshCutSetup.TryCreateFromWorldPoints))]
        private static class TryCreateFromWorldPointsPatch
        {
            private static void Prefix(Transform cutTr, ref Il2CppStructArray<Vector3> points, NavmeshCut navCut, float extraHeight, float margin, ref bool checkTerrainDist, ref bool checkTerrainDistMinHeight, bool cutAddedGeo)
            {
                if (!Config.DontOpenCaves.Value && Config.AllowActorsInCaves.Value)
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
        }


        [HarmonyPatch(typeof(DebugConsole), nameof(DebugConsole.CreateCaveLight))]
        private static class CaveLightPatch
        {
            private static bool Prefix(DebugConsole __instance, ref GameObject __result)
            {
                GameObject gameObject = new GameObject("CaveLight");
                gameObject.transform.parent = LocalPlayer.Transform;
                gameObject.transform.position = LocalPlayer.Transform.position + Vector3.up * 2f + Vector3.forward * 0.2f;
                gameObject.AddComponent<Light>().type = LightType.Point;
                HDAdditionalLightData hdadditionalLightData = gameObject.AddComponent<HDAdditionalLightData>();
                hdadditionalLightData.SetIntensity(10000f, LightUnit.Lux);
                hdadditionalLightData.luxAtDistance = 10f;
                hdadditionalLightData.SetRange(100f);
                hdadditionalLightData.affectsVolumetric = false;
                __result = gameObject;
                return false;
            }
        }


        // Robby fixes and improvements

        [HarmonyPatch(typeof(VailActor), nameof(VailActor.SetupMoveToTarget))]
        private static class SetupMoveToTargetPatch
        {
            private static void Prefix(VailActor __instance, ref MoveToParams moveToParams, Transform targetTransform, bool targetIsStimuli)
            {
                if(!Config.DontOpenCaves.Value && Config.AllowActorsInCaves.Value)
                {
                    if (__instance.TypeId == VailActorTypeId.Robby)
                    {
                        Vector3 actorPos = __instance.transform.position;

                        if (actorPos.y < TerrainUtilities.GetTerrainHeight(actorPos) - 5f && moveToParams.StopIgnoreY == true)
                        {
                            moveToParams.StopIgnoreY = false; // Disable Y-axis ignoring for Robby
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Robby), nameof(Robby.FindChopTreeStimuli))]
        private static class RobbyFindChopTreeStimuli
        {
            private static bool Prefix(Robby __instance, ref Vector3 checkPos)
            {
                if(!Config.DontOpenCaves.Value && Config.AllowActorsInCaves.Value)
                {
                    checkPos = TryFindBetterSearchPos(__instance, checkPos);
                }

                return true; // Let the original method run.
            }
        }

        [HarmonyPatch(typeof(Robby), nameof(Robby.FindClearStumpStimuli))]
        private static class RobbyFindClearStumpStimuli
        {
            private static bool Prefix(Robby __instance, ref Vector3 checkPos)
            {
                if (!Config.DontOpenCaves.Value && Config.AllowActorsInCaves.Value)
                {
                    checkPos = TryFindBetterSearchPos(__instance, checkPos);
                }

                return true; // Let the original method run.
            }
        }

        [HarmonyPatch(typeof(Robby), nameof(Robby.FindBushClearStimuli))]
        private static class RobbyFindBushClearStimuli
        {
            private static bool Prefix(Robby __instance, ref Vector3 checkPos)
            {
                if (!Config.DontOpenCaves.Value && Config.AllowActorsInCaves.Value)
                {
                    checkPos = TryFindBetterSearchPos(__instance, checkPos);
                }

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
                        if (GetBestCaveExitWithFallback(actorPos, currentGraphIndex, out Vector3 bestExitPoint))
                        {
                            // Success! Reroute the search to the best exit.
                            checkPos = bestExitPoint;
                        }
                        else
                        {
                            DebugManager.DebugLogError($"No mapped exits found or no valid path for cave with graph index {currentGraphIndex}.");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                DebugManager.DebugLogError($"RobbyCaveExitWaypointPatch failed: {e.Message}");
            }

            return checkPos;
        }


        private static readonly Dictionary<int, List<Vector3>> _caveExitPoints = new Dictionary<int, List<Vector3>>
        {
            // Graphs:
            // 0 Main Surface Recast Graph
            // 1 Cave A
            // 2 Cave B
            // 3 Cave C
            // 4 Bunker Entertainment
            // 5 Bunker Food
            // 6 Bunker A
            // 7 Bunker B
            // 8 Bunker C
            // 9 Cave E
            // 10 Bunker Residential
            // 11 Cave D
            // 12 Cave F
            // 13 Cave E Timmy Only

            // Graph 1 // CaveA
            { 1, new List<Vector3> 
                {
                    new Vector3(-422.122f, 14.6819f, 1528.715f)
                } 
            },
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
                    new Vector3(-525.8137f, 197.5222f, 117.9281f), 
                    new Vector3(-605.4895f, 178.9792f, 103.7082f) 
                } 
            },
        
            // Graph 4 // Bunker Entertainment
            { 4, new List<Vector3>
                {
                    new Vector3(-1188.931f, 65.6018f, 133.9528f),
                    new Vector3(-984.5704f, 92.232f, 119.5672f)
                }
            },

            // Graph 5 // Bunker Food
            { 5, new List<Vector3>
                {
                    new Vector3(-1014.895f, 97.5355f, 1026.573f),
                    new Vector3(-678.6793f, 49.984f, 1155.883f)
                }
            },

            //  Graph 9 // Cave E
            { 9, new List<Vector3>
                {
                    new Vector3(1758.4f, 39.1792f, 552.9557f)
                }
            },

            // Graph 10 // Bunker Residential
            { 10, new List<Vector3>
                {
                    new Vector3(1242.888f, 237.4724f, -659.7164f),
                    new Vector3(1155.044f, 244.6649f, -519.1276f)
                }
            },

            //  Graph 11 // Cave D
            { 11, new List<Vector3>
                {
                    new Vector3(-566.1116f, 290.5502f, -622.6024f),
                    new Vector3(-662.8178f, 197.2815f, -1057.859f)
                }
            },

        };

        private static string WarningMessage = "Warning! Kelvin was unable to find a path outside, your buildings might be blocking the exit";
        private static float WarningCooldown = 120f;
        private static float LastWarningTime = 0f;

        public static bool GetBestCaveExitWithFallback(Vector3 startPos, int graphIndex, out Vector3 bestExit)
        {
            // 1. Try the "perfect" method first, which uses true path distance.
            if (TryFindBestCaveExitByPath(startPos, graphIndex, out bestExit))
            {
                // Success! We found a reachable exit.
                return true;
            }

            // 2. If it failed, it means all paths are blocked. Log a warning.
            DebugManager.DebugLogWarning($"Path to all predefined exits is blocked. Reverting to straight-line distance fallback.");
            float distanceToPlayer = Vector3.Distance(startPos, LocalPlayer.Transform.position);
            //Check if we should warn the player about this.
            if (distanceToPlayer < 60f && Time.time - LastWarningTime > WarningCooldown)
            {
                SonsTools.ShowMessage(WarningMessage, 10f);
                LastWarningTime = Time.time;
            }


            // 3. Execute the fallback method to find the geometrically closest exit.
            if (TryFindBestCaveExitByDistance(startPos, graphIndex, out bestExit))
            {
                // We found the closest exit, even if it's currently unreachable.
                return true;
            }

            return false;
        }




        private static bool TryFindBestCaveExitByPath(Vector3 startPos, int graphIndex, out Vector3 bestExit)
        {
            bestExit = Vector3.zero;
            if (!_caveExitPoints.TryGetValue(graphIndex, out List<Vector3> potentialExits)) { return false; }

            float shortestPathLength = float.MaxValue;
            bool foundValidPath = false;

            foreach (Vector3 exitPoint in potentialExits)
            {
                var path = ABPath.Construct(startPos, exitPoint, null);
                AstarPath.StartPath(path);
                path.BlockUntilCalculated();

                if (!path.error && path.GetTotalLength() < shortestPathLength)
                {
                    shortestPathLength = path.GetTotalLength();
                    bestExit = exitPoint;
                    foundValidPath = true;
                }
            }
            return foundValidPath;
        }

        public static bool TryFindBestCaveExitByDistance(Vector3 startPos, int graphIndex, out Vector3 bestExit)
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
