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
using UnityEngine.AI;
using Ai.AiUtilities;
using Pathfinding;
using UnityEngine.SocialPlatforms;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.HID;
using Il2CppInterop.Runtime;
using static Endnight.Utilities.PlayerLocation;
using AllowBuildInCaves.NavMeshEditing;
using AllowBuildInCaves.Harmony;
using AllowBuildInCaves.CaveCustomPathStorage;
using SonsSdk.Attributes;
using UnityEngine.Rendering;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

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
public class AllowBuildInCaves : SonsMod, IOnGameActivatedReceiver
{
    private static SeasonsManager SeasonManager;
    public static StructureCraftingSystem CraftingSystem;
    public static ESonsScene SonsScene;
    public AllowBuildInCaves()
    {
        // Uncomment any of these if you need a method to run on a specific update loop.
        OnUpdateCallback = HarmonyPatches.UpdateRequiredCountUi;
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
        SoundTools.RegisterSound("TeleportPickupPop", System.IO.Path.Combine(LoaderEnvironment.ModsDirectory, "AllowBuildInCaves/pop.mp3"), true);

        //Config.ToggleKey.Notify(MainToggle);
        Config.PosKey.Notify(CustomPathGeneration.LogCurrentPositionData);
    }

    public static void PlaySound(Vector3 pos)
    {
        float soundVolume = Sons.Settings.GameSettingsManager.GetSetting("Audio.MasterVolume", 1f);
        SoundTools.PlaySound("TeleportPickupPop", pos, 35, soundVolume);
    }

    public void OnGameActivated()
    {
        //Add component to enforce nav masks
        List<VailActorTypeId> CannibalList = GetActorTypesOfClass(Sons.Ai.Vail.VailActorClassId.Cannibal);
        List<VailActorTypeId> CreepyList = GetActorTypesOfClass(Sons.Ai.Vail.VailActorClassId.Creepy);
        List<VailActorTypeId> AnimalList = GetActorTypesOfClass(Sons.Ai.Vail.VailActorClassId.Animal);

        List<VailActorTypeId> AllList = new List<VailActorTypeId>();
        AllList.AddRange(CannibalList);
        AllList.AddRange(CreepyList);
        AllList.AddRange(AnimalList);

        foreach (var actorType in AllList)
        {
            VailActor actor = ActorTools.GetPrefab(actorType);

            if (actor == null) { continue; }

            GameObject actorObject = actor.gameObject;

            if (actorObject == null)
            {
                continue;
            }

            actorObject.GetOrAddComponent<BuildInCavesActorMaskChanger>();
        }

        ActorTools.GetPrefab(VailActorTypeId.Robby)?.gameObject.GetOrAddComponent<SpecialActorCaveFixes>();
        ActorTools.GetPrefab(VailActorTypeId.Virginia)?.gameObject.GetOrAddComponent<SpecialActorCaveFixes>();
        //Add component to enforce nav masks END

        //Prepare astar for cave meshes
        ReplaceExistingMeshes.AdjustRecastGraphSettings();
        //Create new cave meshes
        ReplaceExistingMeshes.GenerateCaveMeshes();
    }

    protected override void OnGameStart()
    {
        //register debug commands for path creation
        GameCommands.RegisterFromType(typeof(CustomPathGeneration));


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


        //Permanently Enable Cave Collision
        ForceActivateCaveCollision.ForceActivateCaveCollisions();

        bool EnableDebugDrawing = false;

        CustomPathGeneration.ProcessAllCustomPaths(EnableDebugDrawing);

        //CreateTerrainHole(100, 150, new Vector3(1000f, 0f, 1000f), 0);
        CreateTerrainHole(4, 5, new Vector3(-423.24f, 0f, 1510.705f), 0);

    }

    private void CreateTerrainHole(int rectHeight, int rectWidth, Vector3 position, int rotation)
    {
        Il2CppReferenceArray<Terrain> activeTerrains = TerrainUtilities.ActiveTerrains();
        for (int i = 0; i < activeTerrains.Length; i++)
        {
            Terrain terrain = activeTerrains[i];
            if (terrain == null)
            {
                RLog.Error("Terrain is null, skipping terrain processing.");
                continue;
            }

            if (terrain.name.StartsWith("Site02"))
            {
                RLog.Msg($"Processing terrain: {terrain.name}");
                TerrainData td = terrain.terrainData;

                int holeMapResolution = td.holesResolution;
                RLog.Msg($"Hole map resolution for {terrain.name}: {holeMapResolution}x{holeMapResolution}");

                RenderTexture holeProcessingRT = RenderTexture.GetTemporary(holeMapResolution, holeMapResolution, 0, RenderTextureFormat.R8);

                if (holeProcessingRT == null) 
                {
                    RLog.Error("Failed to get temporary RenderTexture.");
                    continue;
                }

                RenderTexture oldActive = RenderTexture.active; 
                RenderTexture.active = holeProcessingRT;

                Texture existingHolesTexture = td.holesTexture;
                if (existingHolesTexture != null)
                {
                    RLog.Msg("Existing holesTexture found. Blitting to processing RT.");
                    Graphics.Blit(existingHolesTexture, holeProcessingRT);
                }
                else
                {
                    RLog.Msg("No existing holesTexture found or it's null. Clearing processing RT to 'all solid'.");
                    GL.Clear(true, true, Color.white);
                }

                Material blackMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                blackMaterial.SetColor("_Color", Color.black);

                DrawTerrainHole(rectHeight, rectWidth, position, rotation, holeMapResolution, blackMaterial);

                RectInt rectToCopyFromRT = new RectInt(0, 0, holeMapResolution, holeMapResolution);
                int destinationXOnHoleMap = 0;
                int destinationYOnHoleMap = 0;
                bool allowDelayedCPUSync = true;

                RLog.Msg($"Calling Internal_CopyActiveRenderTextureToHoles for {terrain.name}");
                RLog.Msg($"  Source Rect: x={rectToCopyFromRT.x}, y={rectToCopyFromRT.y}, w={rectToCopyFromRT.width}, h={rectToCopyFromRT.height}");
                RLog.Msg($"  Dest Coords: x={destinationXOnHoleMap}, y={destinationYOnHoleMap}");
                RLog.Msg($"  Delayed Sync: {allowDelayedCPUSync}");

                try
                {
                    td.Internal_CopyActiveRenderTextureToHoles(rectToCopyFromRT, destinationXOnHoleMap, destinationYOnHoleMap, allowDelayedCPUSync);
                    RLog.Msg("Successfully called Internal_CopyActiveRenderTextureToHoles.");
                }
                catch (System.Exception e)
                {
                    RLog.Error($"Exception calling Internal_CopyActiveRenderTextureToHoles: {e.Message}\n{e.StackTrace}");
                }
        
                //finally sync collider with holes
                td.Internal_SyncHoles();


                // Restore previously active RenderTexture and release the temporary one
                RenderTexture.active = oldActive;
                RenderTexture.ReleaseTemporary(holeProcessingRT);

                if (blackMaterial != null) UnityEngine.Object.Destroy(blackMaterial);

                break;
            }
        }
    }

    private void DrawTerrainHole(float rectHeight, float rectWidth, Vector3 worldCenterPosition, float rotationDegrees, int holeMapResolution, Material drawMaterial) // holeMapResolution is still passed for GL.LoadPixelMatrix and clarity
    {
        RLog.Msg($"DrawRectangularHoleOnActiveRT called with: H={rectHeight}, W={rectWidth}, Pos={worldCenterPosition}, Rot={rotationDegrees}, Res={holeMapResolution}");

        if (drawMaterial == null)
        {
            RLog.Error("Draw material is null in DrawRectangularHoleOnActiveRT.");
            return;
        }
        drawMaterial.SetPass(0); // Ensure material is set to draw the desired "hole" color

        GL.PushMatrix();
        // Setup GL to draw in pixel coordinates on the active RenderTexture.
        // (0,0) is top-left, (holeMapResolution, holeMapResolution) is bottom-right for drawing.
        GL.LoadPixelMatrix(0, holeMapResolution, holeMapResolution, 0);

        // --- Define Terrain World Boundaries for mapping to its hole texture ---
        const float terrainMinX = -2000f;
        const float terrainMinZ = -2000f; // The "bottom" or "south" Z extent of the terrain
        const float terrainTotalWidth = 4000f;  // (+2000 - (-2000))
        const float terrainTotalHeight = 4000f; // (+2000 - (-2000))

        // --- Calculate Rotated Rectangle Corners in World Space ---
        Vector2 center = new Vector2(worldCenterPosition.x, worldCenterPosition.z);
        float halfW = rectWidth / 2.0f;
        float halfH = rectHeight / 2.0f;

        float angleRad = rotationDegrees * Mathf.Deg2Rad;
        float cosTheta = Mathf.Cos(angleRad);
        float sinTheta = Mathf.Sin(angleRad);

        Vector2[] localCorners = new Vector2[] {
        new Vector2(-halfW,  halfH), // Local Top-Left
        new Vector2( halfW,  halfH), // Local Top-Right
        new Vector2( halfW, -halfH), // Local Bottom-Right
        new Vector2(-halfW, -halfH)  // Local Bottom-Left
    };

        Vector3[] pixelVertices = new Vector3[4]; // Will store Z as 0 for GL.Vertex3
        RLog.Msg("Calculating Pixel Vertices (Mapping to Terrain [-2000,+2000] -> Hole Texture [0,holeMapRes]):");

        for (int i = 0; i < 4; i++)
        {
            float localX = localCorners[i].x;
            float localY = localCorners[i].y;

            float worldXOffset = localX * cosTheta - localY * sinTheta;
            float worldZOffset = localX * sinTheta + localY * cosTheta;

            float currentWorldX = center.x + worldXOffset;
            float currentWorldZ = center.y + worldZOffset;

            // --- Convert World Corner to This Terrain's Hole Map Pixel Coordinates ---
            float normalizedX_onTerrain = (currentWorldX - terrainMinX) / terrainTotalWidth;
            float normalizedZ_onTerrain_fromMin = (currentWorldZ - terrainMinZ) / terrainTotalHeight;

            float pixelU_float = normalizedX_onTerrain * holeMapResolution;
            // V=0 corresponds to Max Z of terrain (+2000), where normalizedZ_onTerrain_fromMin = 1.0
            float pixelV_float = (1.0f - normalizedZ_onTerrain_fromMin) * holeMapResolution;

            // Clamp to ensure coordinates are within the texture dimensions (0 to holeMapResolution-1)
            // Subtract a small epsilon before floor/round for the max value to avoid going over due to float precision.
            float clampedU = Mathf.Clamp(pixelU_float, 0f, holeMapResolution - 0.001f);
            float clampedV = Mathf.Clamp(pixelV_float, 0f, holeMapResolution - 0.001f);

            pixelVertices[i] = new Vector3(Mathf.Floor(clampedU), Mathf.Floor(clampedV), 0f);

            RLog.Msg($"  Corner {i}: World({currentWorldX:F2},{currentWorldZ:F2}) => NormOnTerrain({normalizedX_onTerrain:F4},{normalizedZ_onTerrain_fromMin:F4}) => PixelFloat({pixelU_float:F2},{pixelV_float:F2}) => PixelInt({pixelVertices[i].x},{pixelVertices[i].y})");
        }

        RLog.Msg($"Drawing Quad at Pixels: V0({pixelVertices[0].x},{pixelVertices[0].y}), V1({pixelVertices[1].x},{pixelVertices[1].y}), V2({pixelVertices[2].x},{pixelVertices[2].y}), V3({pixelVertices[3].x},{pixelVertices[3].y})");

        GL.Begin(GL.QUADS);
        GL.Vertex(pixelVertices[0]); // Corresponds to local Top-Left
        GL.Vertex(pixelVertices[1]); // Corresponds to local Top-Right
        GL.Vertex(pixelVertices[2]); // Corresponds to local Bottom-Right
        GL.Vertex(pixelVertices[3]); // Corresponds to local Bottom-Left
        GL.End();

        GL.PopMatrix();
    }

    //helper to fetch all actors
    public static List<VailActorTypeId> GetActorTypesOfClass(VailActorClassId classId)
    {
        var result = new List<VailActorTypeId>();

        Array actorTypeIds = Enum.GetValues(typeof(VailActorTypeId));

        foreach (Sons.Ai.Vail.VailActorTypeId actorId in actorTypeIds)
        {
            if (VailTypes.GetActorClass(actorId) == classId)
            {
                result.Add(actorId);
            }
        }

        return result;
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

