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
using AllowBuildInCaves.Debug;

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
    public static int graphMask = -1;
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

        string AstarVersion = ReplaceExistingMeshes.FetchAstarVersion();
        RLog.Msg($"Astar Version: {AstarVersion}");

        ReplaceExistingMeshes.EnableCuttingOnCaveMeshes();
    }

    protected override void OnGameStart()
    {
        

        //Permanently Enable Cave Collision
        ForceActivateCaveCollision.ForceActivateCaveCollisions();


        //register debug commands
        GameCommands.RegisterFromType(typeof(CustomPathGeneration));
        GameCommands.RegisterFromType(typeof(HoleGenerationHelper));


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

        bool EnableDebugDrawing = false;

        CustomPathGeneration.ProcessAllCustomPaths(EnableDebugDrawing);

        List<CustomHoleGeneration.TerrainHole> terrainHoles = new List<CustomHoleGeneration.TerrainHole>
        {
            new CustomHoleGeneration.TerrainHole(4, 5, new Vector3(-423.24f, 0f, 1510.705f), 0), //Cave A
            new CustomHoleGeneration.TerrainHole(1, 1, new Vector3(-478.056f, 0f, 710.574f), 0), //Bunker A
        };

        CustomHoleGeneration.CreateTerrainHole(terrainHoles);
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
        //CaveAEntranceTriggerCreation();
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

