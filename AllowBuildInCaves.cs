using Construction;
using Sons.Gui;
using SonsSdk;
using UnityEngine;
using SUI;
using RedLoader;
using Sons.Areas;
using Endnight.Utilities;
using TheForest.Utils;
using System.Reflection.Emit;
using System.Reflection;
using TheForest.Player.Actions;
using Endnight.Environment;
using System.Runtime.InteropServices;
using Sons.Gameplay;
using Sons.Gameplay.GPS;
using Sons.Ai;
using Sons.Animation.PlayerControl;
using Sons.Cutscenes;
using UnityEngine.Playables;
using static RedLoader.RLog;
using TheForest.Items.Inventory;
using TheForest.Items.Special;
using Sons.Settings;
using Sons.Atmosphere;
using Endnight.Extensions;
using Sons.Player;
using TheForest.World;
using System.Collections;
using JetAnnotations;
using RedLoader.Utils;
using Endnight.Types;
using Sons.Crafting.Structures;
using AllowBuildInCaves.Triggers;
using Sons.Animation;
using Harmony;
using Sons.Ai.Vail;
using Sons.Extensions;
using UnityEngine.UI;
using HarmonyLib;


namespace AllowBuildInCaves;

public static class IsInCavesStateManager
{
    public static bool IsInCaves { get; private set; } // Store the real state
    public static bool? ChangeIsInCaves { get; set; } = null; // Store the state that will be used in the game
    public static bool GPSShouldLoseSignal { get; set; } = false; // Store the state that will be used in the game
    public static bool ApplyBlueFix { get; set; } = false; // Store the state that will be used in the game
    public static bool AllowItemsDuringAnimation { get; set; } = false;
    public static int ItemId { get; set; } = 0;
    public static int ItemAmount { get; set; } = 0;
    public static bool TryAddItems { get; set; } = false;
    public static bool ApplySnowFix { get; set; } = false;
    public static bool EnableEasyBunkers { get; set; } = false;
    public static bool RockRemoverCaveBRunning { get; set; } = false;
    public static bool RockRemoverBunkerFoodRunning { get; set; } = false;
    public static bool ItemCollectUIFix { get; set; } = false;
    //public static bool CaveTeleportFix { get; set; } = false;
    

    // Add methods to update the state when entering/exiting caves
    public static void EnterCave() => IsInCaves = true;
    public static void ExitCave() => IsInCaves = false;
}
public class AllowBuildInCaves : SonsMod
{
    private static SeasonsManager SeasonManager;
    private static StructureCraftingSystem CraftingSystem;
    public static ESonsScene SonsScene;
    public AllowBuildInCaves()
    {
        // Uncomment any of these if you need a method to run on a specific update loop.
        OnUpdateCallback = MyUpdateMethod;
        //OnLateUpdateCallback = MyLateUpdateMethod;
        //OnFixedUpdateCallback = MyFixedUpdateMethod;
        //OnGUICallback = MyGUIMethod;

        // Uncomment this to automatically apply harmony patches in your assembly.
        HarmonyPatchAll = true;
        
    }

    protected override void OnSonsSceneInitialized(ESonsScene sonsScene)
    {
        SonsScene = sonsScene;
    }

    protected override void OnInitializeMod()
    {
        // Do your early mod initialization which doesn't involve game or sdk references here
        Config.Init();
    }
    protected override void OnSdkInitialized()
    {
        // Do your mod initialization which involves game or sdk references here
        // This is for stuff like UI creation, event registration etc.
        AllowBuildInCavesUi.Create();

        // Add in-game settings ui for your mod.
        SettingsRegistry.CreateSettings(this, null, typeof(Config));

        // Add a keybind to toggle the mod
        SoundTools.RegisterSound("TeleportPickupPop", Path.Combine(LoaderEnvironment.ModsDirectory, "AllowBuildInCaves/pop.mp3"), true);

        //Config.ToggleKey.Notify(MainToggle);

    }

    public static void PlaySound(Vector3 pos)
    {
        float soundVolume = Sons.Settings.GameSettingsManager.GetSetting("Audio.MasterVolume", 1f);
        SoundTools.PlaySound("TeleportPickupPop", pos, 35, soundVolume);
    }

    protected override void OnGameStart()
    {
        // This is called once the player spawns in the world and gains control.
        var inWorldModules = ConstructionSystem._instance._inWorldModules;
        var inCaveModules = ConstructionSystem._instance._inCavesModules;

        foreach (var module in inWorldModules._allHighPriorityOnStructureElementDynamicModules)
            inCaveModules._allHighPriorityOnStructureElementDynamicModules.AddIfUnique(module);
        foreach (var module in inWorldModules._allOnStructureElementDynamicModules)
            inCaveModules._allOnStructureElementDynamicModules.AddIfUnique(module);
        foreach (var module in inWorldModules._allOnStructureElementPredictingModules)
            inCaveModules._allOnStructureElementPredictingModules.AddIfUnique(module);
        foreach (var module in inWorldModules._allOnOtherTargetModules)
        {
            if (module != null) inCaveModules._allOnOtherTargetModules.AddIfUnique(module);
        }


        CraftingSystem = StructureCraftingSystem._instance;

        //Find the construction manager in the scene.
        //This is the manager that handles all construction in the game.
        IsInCavesStateManager.GPSShouldLoseSignal = Config.GPSLoseSignal.Value;
        IsInCavesStateManager.ApplyBlueFix = Config.BlueFix.Value;
        if (IsInCavesStateManager.ApplyBlueFix && IsInCavesStateManager.IsInCaves) { BlueFix(); }
        IsInCavesStateManager.AllowItemsDuringAnimation = Config.KeepItemsInCutscene.Value;
        IsInCavesStateManager.ApplySnowFix = true;
        if (IsInCavesStateManager.ApplySnowFix && IsInCavesStateManager.IsInCaves) { SnowFix(false, false); }
        //IsInCavesStateManager.EnableEasyBunkers = Config.EasyBunkers.Value;
        IsInCavesStateManager.ItemCollectUIFix = Config.ItemCollectUIFix.Value;

        SeasonManager = GameObject.Find("SeasonsManager").GetComponent<SeasonsManager>();

        DestroyEntrances();
        AdjustCellars();
        AddTriggerComponentToBunkers();






        Vector3[] points1 = new Vector3[]
        {
        new Vector3(1823f, 19.5f, 539f),
        new Vector3(1824f, 19.5f, 540f),
        new Vector3(1825f, 19.5f, 540f),
        new Vector3(1827f, 19.5f, 542f),
        new Vector3(1824f, 19.5f, 549f),
        };

        Vector3[] points2 = new Vector3[]
{
        new Vector3(1830f, 19.5f, 536f),
        new Vector3(1835f, 19.5f, 535f),
        new Vector3(1831f, 19.5f, 533f),
        new Vector3(1836f, 19.5f, 536f),
        new Vector3(1840f, 19.5f, 533f),
};

        GenerateMeshData(points1, 0.2f, 0.2f);
        GenerateMeshData(points2, 0.2f, 0.2f);

        // Example usage with 4 points
        Vector3[] points = new Vector3[]
        {
            new Vector3(1756.013f, 39.8327f, 552.9528f), // Connects to the main navmesh
            new Vector3(1753.378f, 40.346f, 552.8621f),
            new Vector3(1751.516f, 39.51f, 552.8435f),
            new Vector3(1740.132f, 39.52f, 553.4894f),
            new Vector3(1739.909f, 39.52f, 549.1848f),
            new Vector3(1739.792f, 35.82f, 539.6614f),
            new Vector3(1736.882f, 35.82f, 539.5541f),
            new Vector3(1737.004f, 32.12f, 552.0319f),
            new Vector3(1739.782f, 32.12f, 552.2333f),
            new Vector3(1740.061f, 28.42f, 538.2747f),
            new Vector3(1737.299f, 28.42f, 538.0711f),
            new Vector3(1737.066f, 28.42f, 541.1553f),
            new Vector3(1737.077f, 24.72f, 553.1839f),
            new Vector3(1666.564f, 24.718f, 553.1696f),
            new Vector3(1666.616f, 22.952f, 544.5799f),
            new Vector3(1664.501f, 22.952f, 544.9677f),
            new Vector3(1664.543f, 21.507f, 552.0028f),
            new Vector3(1643.415f, 21.43f, 554.8794f),
        };

        GenerateMeshData(points, 0.2f, 0.2f);
    }   

    public static void RefreshRequiredItemsUI()
    {
        CraftingSystem.RefreshRequiredItemsUi();
    }

    public static void RefreshRequiredItemsUiInCave()
    {
        HudGui._instance.ClearAllRequiredCollectionCounts();
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
    [HarmonyPatch(typeof(HudGui), "ClearAllRequiredCollectionCounts")]
    private static class HudGuiClearRequiredPatch
    {
        private static bool Prefix()
        {
            if (IsInCavesStateManager.ItemCollectUIFix) { return true; }
            if (IsInCavesStateManager.IsInCaves)
            {
                CraftingSystem.UpdateRequiredCountUIForAllItems();
                return false;
                
            };
            return true;
        }
    }

    private static void MyUpdateMethod()
    {
        if(IsInCavesStateManager.IsInCaves && !IsInCavesStateManager.ItemCollectUIFix)
        {
            CraftingSystem.UpdateRequiredCountUIForAllItems();
        }
    }

    public bool GenerateMeshData(Vector3[] IPoints, float forwardDistance, float backwardDistance)
    {
        if (IPoints.Length < 2)
        {
            RLog.Error("At least 2 points are required to create a navmesh line.");
            return false;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Il2CppSystem.Collections.Generic.List<UnityEngine.Vector3> allVertices = new Il2CppSystem.Collections.Generic.List<UnityEngine.Vector3>();
        Il2CppSystem.Collections.Generic.List<int> allIndices = new Il2CppSystem.Collections.Generic.List<int>();
        Vector3 firstPoint = IPoints[0];

        // Handle the first IPoint
        Vector3 firstDirection = (IPoints[1] - IPoints[0]).normalized;
        Vector3 firstPerpendicular = Vector3.Cross(firstDirection, Vector3.up).normalized;
        if (firstPerpendicular == Vector3.zero)
        {
            firstPerpendicular = Vector3.Cross(firstDirection, Vector3.forward).normalized;
        }
        vertices.Add(IPoints[0] - firstPerpendicular * forwardDistance); // Point 1
        vertices.Add(IPoints[0] + firstPerpendicular * forwardDistance); // Point 2

        // Vertices Calculation
        for (int i = 1; i < IPoints.Length - 1; i++)
        {
            Vector3 p1 = new Vector3(IPoints[i - 1].x, IPoints[i].y, IPoints[i - 1].z);
            Vector3 p2 = IPoints[i];
            Vector3 p3 = new Vector3(IPoints[i + 1].x, IPoints[i].y, IPoints[i + 1].z);

            // Calculate the normal of the plane
            Vector3 normal = Vector3.Cross(p1 - p2, p3 - p2).normalized;

            // Calculate the angle between IPoint 1 and 3
            float angle = Vector3.Angle(p1 - p2, p3 - p2);
            Vector3 bisector = ((p1 - p2).normalized + (p3 - p2).normalized).normalized;

            // Project the bisector onto the plane
            Vector3 projectedBisector = bisector - Vector3.Dot(bisector, normal) * normal;

            // Place a point along the projected bisector (Point 3, 6, 9, etc.)
            vertices.Add(p2 + projectedBisector.normalized * forwardDistance);

            // Draw imaginary line from IPoint3 to IPoint2 and place a point (Point 4, 7, 10, etc.)
            vertices.Add(p2 + (p2 - p3).normalized * backwardDistance);

            // Draw imaginary line from IPoint1 to IPoint2 and place a point (Point 5, 8, etc.)
            vertices.Add(p2 + (p2 - p1).normalized * backwardDistance);
        }

        // Handle the last IPoint
        Vector3 lastDirection = (IPoints[IPoints.Length - 2] - IPoints[IPoints.Length - 1]).normalized;
        Vector3 lastPerpendicular = Vector3.Cross(lastDirection, Vector3.up).normalized;
        if (lastPerpendicular == Vector3.zero)
        {
            lastPerpendicular = Vector3.Cross(lastDirection, Vector3.forward).normalized;
        }
        vertices.Add(IPoints[IPoints.Length - 1] + lastPerpendicular * forwardDistance); // Point 13
        vertices.Add(IPoints[IPoints.Length - 1] - lastPerpendicular * forwardDistance); // Point 12

        //Triangle calculation
        for (int i = 1; i < IPoints.Length - 1; i++)
        {
            RLog.Msg("Calculating for IPoint " +  i);
            if (i == 1) // Special handling for the second IPoint (since we start at i = 1)
            {
                triangles.Add(0);     // 0, 1, 2 
                triangles.Add(2);
                triangles.Add(1);

                triangles.Add(1);     // 2, 3, 1 
                triangles.Add(2);
                triangles.Add(3);
            }
            else
            {
                int baseIndex = (i - 1) * 3; // 3 vertices per IPoint
                RLog.Msg("Running loop for:");
                RLog.Msg("BaseIndex: " + baseIndex);
                RLog.Msg("For Points: " + (baseIndex - 1 ) + " " + (baseIndex + 2) + " " + (baseIndex + 1) + " " + (baseIndex + 3));
                RLog.Msg("Point " + (baseIndex - 1) + " : " + vertices[baseIndex - 1]);
                RLog.Msg("Point " + (baseIndex + 2) + " : " + vertices[baseIndex + 2]);
                RLog.Msg("Point " + (baseIndex + 1) + " : " + vertices[baseIndex + 1]);
                RLog.Msg("Point " + (baseIndex + 3) + " : " + vertices[baseIndex + 3]);
                RLog.Msg("Intersection: " + DoLinesIntersect(vertices[baseIndex - 1], vertices[baseIndex + 2], vertices[baseIndex + 1], vertices[baseIndex + 3]));
                if (!DoLinesIntersect(vertices[baseIndex - 1], vertices[baseIndex + 2], vertices[baseIndex + 1], vertices[baseIndex + 3]))
                {
                    //If they do NOT intersect
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 3);

                    triangles.Add(baseIndex - 1);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 1);
                    

                } else
                {
                    //If they DO intersect
                    triangles.Add(baseIndex - 1);
                    triangles.Add(baseIndex + 3);
                    triangles.Add(baseIndex + 2);

                    triangles.Add(baseIndex - 1);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 1);
                }



                //WINDING ORDER COUNTER CLOCKWISE!
                // Form triangles 
                triangles.Add(baseIndex - 1);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex);
            }


        }

        // Form triangles for the last segment (adjust indices)
        int lastBaseIndex = (IPoints.Length - 2) * 3;
        if (!DoLinesIntersect(vertices[lastBaseIndex - 1], vertices[lastBaseIndex + 2], vertices[lastBaseIndex + 1], vertices[lastBaseIndex + 3]))
        {
            //If they do NOT intersect
            triangles.Add(lastBaseIndex + 1);
            triangles.Add(lastBaseIndex + 2);
            triangles.Add(lastBaseIndex + 3);

            triangles.Add(lastBaseIndex - 1);
            triangles.Add(lastBaseIndex + 2);
            triangles.Add(lastBaseIndex + 1);


        }
        else
        {
            //If they DO intersect
            triangles.Add(lastBaseIndex - 1);
            triangles.Add(lastBaseIndex + 3);
            triangles.Add(lastBaseIndex + 2);

            triangles.Add(lastBaseIndex - 1);
            triangles.Add(lastBaseIndex + 2);
            triangles.Add(lastBaseIndex + 1);
        }

        triangles.Add(lastBaseIndex - 1);
        triangles.Add(lastBaseIndex + 1);
        triangles.Add(lastBaseIndex);


        // Add the vertices and indices to the combined lists
        foreach (var vertex in vertices)
        {
            allVertices.Add(vertex);
        }
        for (int j = 0; j < triangles.Count; j++)
        {
            allIndices.Add(triangles[j]); // No need for vertexOffset here
        }

        // --- Create GameObject and component ---

        GameObject navMeshObject = new GameObject("_CustomNavMeshLine");
        NavMeshCustomMeshAdd navMeshAdder = navMeshObject.AddComponent<NavMeshCustomMeshAdd>();

        // --- Apply the combined navmesh ---

        navMeshAdder.ApplyNavMeshAdd(allVertices, allIndices);

        // --- Nav link creation (only for the first point) ---

        bool isConnected = false;
        Vector3 connectionPoint = Vector3.zero;

        // Multiple connection attempts with raycasting from the FIRST point
        for (float offset = 0.05f; offset <= 0.3f; offset += 0.05f)
        {
            connectionPoint = firstPoint + Vector3.down * offset;

            if (navMeshAdder.TryAddNavLinkToTerrain(firstPoint, connectionPoint))
            {
                isConnected = true;
                break;
            }
        }

        if (isConnected)
        {
            RLog.Msg($"Created successful connection at {firstPoint} with link point {connectionPoint}");
            RLog.Msg("All Vertices");
            for (int j = 0; j < allVertices.Count; j++)
            {
                RLog.Msg(allVertices[j]);
            }
            RLog.Msg("All Indices");
            for(int j = 0; j < allIndices.Count; j++){
                RLog.Msg(allIndices[j]);
            }
            return true;
        }
        else
        {
            RLog.Error("Failed to connect to terrain navmesh.");
            // ... (consider disabling the navmesh or other actions) ...
            RLog.Msg("Failed to create nav mesh link");
            return false;
        }
    }

    bool DoLinesIntersect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        // Ensure all points are on the same plane (using p1.y as the height)
        p1.y = p2.y = p3.y = p4.y;

        // Calculate the direction vectors of the lines (Corrected)
        Vector3 d1 = (p2 - p1);
        Vector3 d2 = (p4 - p3);

        // Calculate the denominator for the parametric equations
        float denominator = d1.x * d2.z - d1.z * d2.x;

        // If the denominator is zero, the lines are parallel
        if (Mathf.Abs(denominator) < 1e-6f)
        {
            return false;
        }

        // Calculate the parameters for the intersection point
        float t1 = ((p1.z - p3.z) * d2.x - (p1.x - p3.x) * d2.z) / denominator;
        float t2 = ((p1.z - p3.z) * d1.x - (p1.x - p3.x) * d1.z) / denominator;

        // Check if the intersection point lies within both line segments
        return (t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1);
    }


    //test add patch to cave enter animation
    [HarmonyPatch(typeof(CaveEntranceCutscene), "OnEnterCaveEntrance")]
    private static class EnterCutscenePatch
    {
        private static void Prefix()
        {
            RemoveItemsOnEnter();
        }
    }

    [HarmonyPatch(typeof(CaveEntranceCutscene), "FinalizeSequence")]
    private static class FinishCutscenePatch
    {
        private static void Postfix()
        {
            AddItemsOnExit();
        }
    }

    
    //Climb up hatch
    [HarmonyPatch(typeof(ClimbUpHatchTrigger), "ClimbInputReceived")]
    private static class EnterHatchUpCutscenePatch
    {
        private static void Prefix()
        {
            RemoveItemsOnEnter();
            SnowFix(true, false);
        }
    }

    [HarmonyPatch(typeof(Cutscene), "Cleanup")]
    private static class FinishHatchUpCutscenePatch
    {
        private static void Postfix()
        {
            AddItemsOnExit();
        }
    }

    //Main IsInCaves Patch

    [HarmonyPatch(typeof(CaveEntranceManager), "OnCaveEnter")]
    private static class EnterPatch
    {
        private static void Postfix()
        {
            IsInCavesStateManager.EnterCave();
            BlueFix();
            SnowFix(false, false);
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
            UndoBlueFix();
            SnowFix(true, false);
            //FixCollectUI();
        }
    }

    //General Routines
    private void DestroyCaveEntrance(string CaveName)
    {
        GameObject CaveExternal = GameObject.Find(CaveName);
        List<Transform> CaveExternalTransforms = CaveExternal.GetChildren();
        foreach (Transform t in CaveExternalTransforms)
        {
            if (t.name.StartsWith("CaveEntrance") || t.name.StartsWith("CaveEntranceShimmy") || t.name.StartsWith("CaveExitShimmy"))
            {
                List<Transform> CaveEntranceTransforms = t.gameObject.GetChildren();
                foreach (Transform t2 in CaveEntranceTransforms)
                {
                    if (t2.name == "EntranceTrigger" || t2.name == "ExitTrigger" || t2.name == "QuadBlockerEnterCave" || t2.name == "QuadBlockerExitCave" || t2.name == "Renderable") t2.gameObject.SetActive(false);
                }
            }
        }
    }

    private void AdjustHouseCave(string CaveName)
    {
        GameObject CaveExternal = GameObject.Find(CaveName);
        List<Transform> CaveExternalTransforms = CaveExternal.GetChildren();
        foreach (Transform t in CaveExternalTransforms)
        {
            if (t.name.StartsWith("EntranceLighting"))
            {
                List<Transform> CaveEntranceTransforms = t.gameObject.GetChildren();
                foreach (Transform t2 in CaveEntranceTransforms)
                {
                    if (t2.name == "AmbientGroup")
                    {
                        List<Transform> CaveEntranceTransforms2 = t2.gameObject.GetChildren();
                        foreach (Transform t3 in CaveEntranceTransforms2)
                        {
                            if (t3.name == "AmbientOverride") { GameObject.Destroy(t3.gameObject); }
                        }
                    }
                }
            }
        }
    }

    private void AdjustCellarCave(string CaveName)
    {
        GameObject CaveExternal = GameObject.Find(CaveName);
        List<Transform> CaveExternalTransforms = CaveExternal.GetChildren();
        foreach (Transform t in CaveExternalTransforms)
        {
            if (!t.name.StartsWith("Lighting")) { continue; }
            List<Transform> CaveEntranceTransforms = t.gameObject.GetChildren();
            foreach (Transform t2 in CaveEntranceTransforms)
            {
                if (t2.name != "InternalLightingGrp") { continue; }

                List<Transform> CaveEntranceTransforms2 = t2.gameObject.GetChildren();
                foreach (Transform t3 in CaveEntranceTransforms2)
                {
                    if (t3.name != "InternalLighting") { continue; }

                    List<Transform> CaveEntranceTransforms3 = t3.gameObject.GetChildren();
                    foreach (Transform t4 in CaveEntranceTransforms3)
                    {
                        if (t4.name == "AmbientOverride") { GameObject.Destroy(t4.gameObject); }
                    }
                }
            }

        }
    }

    private void AdjustIceCave(string CaveName)
    {
        GameObject CaveExternal = GameObject.Find(CaveName);
        List<Transform> CaveExternalTransforms = CaveExternal.GetChildren();
        foreach (Transform t in CaveExternalTransforms)
        {
            if (t.name == "AmbientOverride") { GameObject.Destroy(t.gameObject); }
        }
    }

    private void DestroyLuxuryEntrance(string CaveName)
    {
        GameObject CaveExternal = GameObject.Find(CaveName);
        List<Transform> CaveExternalTransforms = CaveExternal.GetChildren();
        foreach (Transform t in CaveExternalTransforms)
        {
            if (t.name != "LAYOUT") { continue; }
            List<Transform> CaveEntranceTransforms = t.gameObject.GetChildren();
            foreach (Transform t2 in CaveEntranceTransforms)
            {
                if (t2.name.StartsWith("BodyShelving") || t2.name.StartsWith("CaveEntrance"))
                {
                    List<Transform> CaveEntranceTransforms2 = t2.gameObject.GetChildren();
                    foreach (Transform t3 in CaveEntranceTransforms2)
                    {
                        if (t3.name == "WorkerDudePoser" || t3.name == "Renderable") { GameObject.Destroy(t2.gameObject); }
                    }
                }
            }
        }
    }

    private void EntranceManagerGroupFix(string EntranceManagerGroupName)
    {
        GameObject EntranceManagerGroup = GameObject.Find(EntranceManagerGroupName);
        List<Transform> EntranceManagerGroupTransforms = EntranceManagerGroup.GetChildren();
        foreach (Transform t in EntranceManagerGroupTransforms)
        {
            //Cave C Fix
            if (t.name.EndsWith("CaveEntrance") || t.name.EndsWith("Cavexit"))
            {
                //Fetch the component Endnight.Utilities.DirectionalTrigger
                DirectionalTrigger dt = t.GetComponent<DirectionalTrigger>();
                if (dt != null) { dt._useInnerBoundary = true; }
                if (t.name.EndsWith("Cavexit"))
                {
                    t.localScale = new Vector3(0.1f, 0.7f, 0.5f);
                }
                else
                {
                    t.localScale = new Vector3(0.06f, 0.3f, 0.3f);
                }
            }

            //Cave A Fix
            if (t.name.EndsWith("CaveEntranceA"))
            {
                //Fetch the component Endnight.Utilities.DirectionalTrigger
                DirectionalTrigger dt = t.GetComponent<DirectionalTrigger>();
                if (dt != null) { dt._useInnerBoundary = true; }
            }

            //Cave B Fix
            if (t.name.EndsWith("entranceSwitcher") || t.name.EndsWith("exitSwitcher"))
            {
                //Fetch the component Endnight.Utilities.DirectionalTrigger
                DirectionalTrigger dt = t.GetComponent<DirectionalTrigger>();
                if (dt != null) { dt._useInnerBoundary = true; }
                t.localScale = new Vector3(0.1f, 0.7f, 0.5f);
            }

            //Cave F Fix && Luxury Bunker Fix
            if (t.name == "CaveEntrance")
            {
                //Fetch the component Endnight.Utilities.DirectionalTrigger
                DirectionalTrigger dt = t.GetComponent<DirectionalTrigger>();
                if (dt != null) { dt._useInnerBoundary = true; }
            }

        }
    }
    private void DestroyEntrances()
    {
        if (Config.DontOpenCaves.Value) { return; }

        var caveEntranceNames = new List<string> { "CaveAExternal", "CaveBExternal", "CaveCExternal", "CaveDExternal", "CaveF_External", "BE_External", "BF_External", "BunkerFExternal" };
        //opening cave entrances
        foreach (var caveEntranceName in caveEntranceNames)
        {
            DestroyCaveEntrance(caveEntranceName);
        }
        //DestroyCaveEntranceCaveBException("CaveBExternal");

        EntranceManagerGroupFix("CaveA_EntranceManagerGroup");
        EntranceManagerGroupFix("CaveC_EntranceManagerGroup");
        EntranceManagerGroupFix("CaveBExternal");
        EntranceManagerGroupFix("CaveF_External");
        EntranceManagerGroupFix("CaveEExternal");

        DestroyLuxuryEntrance("CaveEExternal");

        CaveBExitTriggerCreation();
        CaveAEntranceTriggerCreation();
        BunkerFoodExitTriggerCreation();
    }

    private void AdjustCellars()
    {
        //Adjustments to allow building in caves/cellars
        var houseCaveNames = new List<string> { "CaveG_External", "CellarA" };
        var cellarNames = new List<string> { "CellarN", "CellarF", "CellarO", "CellarE", "CellarB", "CellarD", "CellarK", "CellarP", "CellarC", "CellarL", "CellarQ", "CellarM", "CellarH" };
        var íceCaveNames = new List<string> { "IceCaveAInventoryAmbientOverride", "IceCaveCInventoryAmbientOverride" };

        foreach (var houseCaveName in houseCaveNames)
        {
            AdjustHouseCave(houseCaveName);
        }
        foreach (var cellarName in cellarNames)
        {
            AdjustCellarCave(cellarName);
        }
        foreach (var iceCaveName in íceCaveNames)
        {
            AdjustIceCave(iceCaveName);
        }
    }

    private void AddTriggerComponentToBunkers()
    {
        var bunkerNames = new List<String> { "BunkerAExternal", "BunkerBExternal", "BunkerCExternal" };
        foreach (var bunkerName in bunkerNames)
        {
            GameObject bunker = GameObject.Find(bunkerName);
            List<Transform> CaveExternalTransforms = bunker.GetChildren();
            foreach (Transform t in CaveExternalTransforms)
            {
                if (t.name.StartsWith("ExtentsTrigger")) 
                {

                    if(t.gameObject == null) return;

                    t.gameObject.AddComponent<PlayerDetectionTrigger>();

                    GameObject triggerObject = new GameObject("ProximityTrigger");
                    triggerObject.transform.position = t.position;
                    triggerObject.transform.rotation = t.rotation;
                    triggerObject.transform.parent = bunker.transform;
                    int targetLayer = LayerMask.NameToLayer("Prop");
                    triggerObject.layer = targetLayer;

                    triggerObject.AddComponent<BunkerTeleportTrigger>();

                }              
            }       
        }
    }

    private void CaveBExitTriggerCreation()
    {
        Vector3 triggerPosition = new Vector3(-1230.998f, 142.2489f, -300.5959f);
        GameObject triggerObject = new GameObject("CaveBExitFixTrigger");

        triggerObject.transform.position = triggerPosition;
        triggerObject.AddComponent<CaveBExitTrigger>();

        GameObject rockRemoverTrigger = new GameObject("RockRemoverTrigger");
        int targetLayer = LayerMask.NameToLayer("BasicCollider");
        rockRemoverTrigger.layer = targetLayer;
        rockRemoverTrigger.transform.position = triggerPosition;
        rockRemoverTrigger.AddComponent<RockRemoverTriggerCaveB>();

        GameObject rockRemoverTriggerDetector = new GameObject("RockRemoverTriggerDetector");
        rockRemoverTriggerDetector.transform.position = triggerPosition;
        rockRemoverTriggerDetector.AddComponent<RockRemoverPlayerDetectionTrigger>();
    }

    private void CaveAEntranceTriggerCreation()
    {
        Vector3 triggerPosition = new Vector3(-423.0533f, 14.3267f, 1511.071f);
        GameObject triggerObject = new GameObject("CaveAEntranceTrigger");

        triggerObject.transform.position = triggerPosition;
        triggerObject.AddComponent<CaveAEntranceFixTrigger>();
    }

    private void BunkerFoodExitTriggerCreation()
    {
        GameObject triggerObject = new GameObject("BunkerFoodExitTrigger");
        triggerObject.transform.position = new Vector3(-674.382f, 51.68f, 1148.683f);
        triggerObject.AddComponent<BunkerFoodExitFixTrigger>();


        GameObject rockRemoverTrigger = new GameObject("RockRemoverTriggerFoodBunkerExit");
        int targetLayer = LayerMask.NameToLayer("BasicCollider");
        rockRemoverTrigger.layer = targetLayer;
        rockRemoverTrigger.transform.position = new Vector3(-674.582f, 51.78f, 1148.683f);
        rockRemoverTrigger.AddComponent<RockRemoverTriggerBunkerFoodExit>();


        GameObject rockRemoverTriggerDetector = new GameObject("RockRemoverTriggerFoodBunkerExitDetector");
        rockRemoverTriggerDetector.transform.position = new Vector3(-674.582f, 51.78f, 1148.683f);
        rockRemoverTriggerDetector.AddComponent<RockRemoverTriggerBunkerFoodExitPlayerDetectionTrigger>();
    }

    public static void BlueFix()
    {
        if (!IsInCavesStateManager.ApplyBlueFix)
        {
            UndoBlueFix();
            return;
        }

        Sons.PostProcessing.PostProcessingManager.DeactivateColorGrade();
        Sons.PostProcessing.PostProcessingManager.ActivateColorGrade("City");
    }

    public static void UndoBlueFix()
    {
        Sons.PostProcessing.PostProcessingManager.DeactivateColorGrade();
        var colorGrade = GameSettingsManager.GetSetting("Graphics.ColorGrade", "Default");
        Sons.PostProcessing.PostProcessingManager.ActivateColorGrade(colorGrade.Replace(" ", ""));
    }

    public static void RemoveItemsOnEnter()
    {
        IsInCavesStateManager.TryAddItems = true;
        if (!IsInCavesStateManager.AllowItemsDuringAnimation) { return; }
        
        PlayerInventory Inventory = LocalPlayer.Inventory;
        int itemId = Inventory._HeldOnlyItemController_k__BackingField.HeldItem?._itemID ?? 0;
        bool hasLogs = false;
        bool hasStone = false;
        bool hasInfiniteHack = Inventory._HeldOnlyItemController_k__BackingField.InfiniteHack;
        if (itemId == 78) { hasLogs = true; }
        if (itemId == 640) { hasStone = true; }

        if(hasInfiniteHack)
        {
            Inventory._HeldOnlyItemController_k__BackingField.InfiniteHack = false;
        }

        if (hasLogs || hasStone)
        {
            IsInCavesStateManager.ItemId = itemId;
            IsInCavesStateManager.ItemAmount = Inventory.AmountOf(itemId, false, false);
            for (int i = 0; i < IsInCavesStateManager.ItemAmount; i++)
            {
                Inventory.HeldOnlyItemController.PutDown(false, false, false, null, 0, 0);
            }
        }

        if(hasInfiniteHack)
        {
            Inventory._HeldOnlyItemController_k__BackingField.InfiniteHack = true;
        }
    }

    public static void AddItemsOnExit()
    {
        if (!IsInCavesStateManager.AllowItemsDuringAnimation) { IsInCavesStateManager.TryAddItems = false; return; }
        AddItemRoutine().RunCoro();
    }

    private static IEnumerator AddItemRoutine()
    {
        while (IsInCavesStateManager.TryAddItems) // Keep trying until successful or stopped manually
        {
            if (!IsInCavesStateManager.AllowItemsDuringAnimation)
            {
                IsInCavesStateManager.TryAddItems = false;
                yield break;
            };
            if (IsInCavesStateManager.ItemId == 0)
            {
                IsInCavesStateManager.TryAddItems = false;
                yield break;
            };

            PlayerInventory inventory = LocalPlayer.Inventory;
            inventory.StashRightHandItem(false, false, true);

            for (int i = inventory.AmountOf(IsInCavesStateManager.ItemId, false, false); i < IsInCavesStateManager.ItemAmount; i++)
            {
                inventory.AddItem(IsInCavesStateManager.ItemId, 1, false, false, null);
            }

            if (IsInCavesStateManager.ItemAmount == inventory.AmountOf(IsInCavesStateManager.ItemId, false, false))
            {
                IsInCavesStateManager.TryAddItems = false; // Stop the coroutine when successful
                IsInCavesStateManager.ItemId = 0;
                IsInCavesStateManager.ItemAmount = 0;
                yield break;
            }

            yield return new WaitForSeconds(0.2f); // Wait for 0.2 seconds before retrying
        }
    }

    public static void SnowFix(bool EnableSnow, bool force)
    {
        if (!force && !IsInCavesStateManager.ApplySnowFix) { return; }
        if(SeasonManager == null) { return; }
        if (SeasonManager._activeSeason == SeasonsManager.Season.Winter)
        {
            if (EnableSnow)
            {
                Shader.SetGlobalFloat("_Sons_SnowAmount", 1);
            }
            else
            {
                Shader.SetGlobalFloat("_Sons_SnowAmount", 0);
            }
        }
    }
}

