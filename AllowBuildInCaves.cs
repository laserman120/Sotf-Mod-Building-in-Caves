using Construction;
using Sons.Gui;
using SonsSdk;
using UnityEngine;
using SUI;
using RedLoader;
using Sons.Areas;
using HarmonyLib;
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
    public static bool ItemCollectUIFix { get; set; } = false;
    public static bool CaveTeleportFix { get; set; } = false;
    

    // Add methods to update the state when entering/exiting caves
    public static void EnterCave() => IsInCaves = true;
    public static void ExitCave() => IsInCaves = false;
}
public class AllowBuildInCaves : SonsMod
{
    private static SeasonsManager SeasonManager;
    public static ESonsScene SonsScene;
    public AllowBuildInCaves()
    {
        // Uncomment any of these if you need a method to run on a specific update loop.
        //OnUpdateCallback = MyUpdateMethod;
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
        RLog.Msg(Path.Combine(LoaderEnvironment.ModsDirectory, "pop.mp3"));
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

        //Find the construction manager in the scene.
        //This is the manager that handles all construction in the game.
        IsInCavesStateManager.GPSShouldLoseSignal = Config.GPSLoseSignal.Value;
        IsInCavesStateManager.ApplyBlueFix = Config.BlueFix.Value;
        if (IsInCavesStateManager.ApplyBlueFix && IsInCavesStateManager.IsInCaves) { BlueFix(); }
        IsInCavesStateManager.AllowItemsDuringAnimation = Config.KeepItemsInCutscene.Value;
        IsInCavesStateManager.ApplySnowFix = Config.SnowFix.Value;
        if (IsInCavesStateManager.ApplySnowFix && IsInCavesStateManager.IsInCaves) { SnowFix(false, false); }
        IsInCavesStateManager.EnableEasyBunkers = Config.EasyBunkers.Value;
        IsInCavesStateManager.ItemCollectUIFix = Config.ItemCollectUIFix.Value;

        SeasonManager = GameObject.Find("SeasonsManager").GetComponent<SeasonsManager>();

        DestroyEntrances();
        AdjustCellars();
        AddTriggerComponentToBunkers();
    }

    //Terrain Height Patch
    [HarmonyPatch(typeof(TerrainUtilities), "GetTerrainHeight")]
    private static class GetTerrainHeightPatch
    {
        private static bool Prefix(Vector3 transformPosition, ref float __result)
        {
            if (IsInCavesStateManager.CaveTeleportFix)
            {
                if (IsInCavesStateManager.IsInCaves == true)
                {
                    __result = transformPosition.y;
                    return false;
                }
            }
            return true;
        }
    }


    //Cave Teleport Fix
    [HarmonyPatch(typeof(GatherablePickup), "TryGather")]
    private static class TryGatheringPatch
    {
        private static void Prefix()
        {
            IsInCavesStateManager.CaveTeleportFix = true;
            if (IsInCavesStateManager.ChangeIsInCaves == null)
            {
                IsInCavesStateManager.ChangeIsInCaves = true;
            }
        }
    }

    [HarmonyPatch(typeof(GatherablePickup), "OnGatheringCompleteCallback")]
    private static class EndGatheringPatch
    {
        private static void Prefix()
        {
            IsInCavesStateManager.CaveTeleportFix = false;
            IsInCavesStateManager.ChangeIsInCaves = false;
        }
    }

    //Gps Tracker Fix
    [HarmonyPatch(typeof(GPSTrackerSystem), "LateUpdate")]
    private static class GPSTrackerCaveFix
    {
        private static void Postfix(GPSTrackerSystem __instance)
        {
            if (IsInCavesStateManager.GPSShouldLoseSignal == true)
            {
                __instance._trackerSignalLost = IsInCavesStateManager.IsInCaves;
                __instance._signalLost.SetActive(__instance._trackerSignalLost);
                __instance._screenStatic.SetActive(!__instance._trackerSignalLost);
                __instance._playerArrow.gameObject.SetActive(!__instance._trackerSignalLost);
            }
        }
    }

    //Sledding Teleport Fix
    [HarmonyPatch(typeof(PlayerAnimatorControl), "StartSledding")]
    private static class StartSleddingPatch
    {
        private static void Prefix()
        {
            IsInCavesStateManager.CaveTeleportFix = true;

            if (IsInCavesStateManager.ChangeIsInCaves == null)
            {
                IsInCavesStateManager.ChangeIsInCaves = true;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerAnimatorControl), "EndSledding")]
    private static class EndSleddingPatch
    {
        private static void Prefix()
        {
            IsInCavesStateManager.CaveTeleportFix = false;
            IsInCavesStateManager.ChangeIsInCaves = false;
        }
    }

    //Butterfly Spawn Fix
    [HarmonyPatch(typeof(ButterflySpawner), "CanSpawn")]
    private static class ButterflySpawnPatch
    {
        static bool Prefix()
        {
            if (IsInCavesStateManager.IsInCaves == true)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    //Rebreather fix
    [HarmonyPatch(typeof(PlayerAnimatorControl), "UpdateInWaterControl")]
    private static class RebreatherFixPatch
    {
        private static void Postfix(PlayerAnimatorControl __instance)
        {
            if (__instance._divingStarted && IsInCavesStateManager.IsInCaves == true)
            {
                if (LocalPlayer.Inventory.Owns(444))
                {
                    LocalPlayer.Inventory.TryEquip(444, false, true);
                }
            }
        }
    }

    //Inventory LED strip fix
    [HarmonyPatch(typeof(Sons.Inventory.InventoryLedStripManager), "Update")]
    private static class LedStripPatch
    {
        private static void Postfix(Sons.Inventory.InventoryLedStripManager __instance)
        {
            if (IsInCavesStateManager.IsInCaves == true && !__instance._manuallyTriggeredPowerState && !__instance._isPowerOn)
            {
                __instance.PowerOn(true);
            }
        }
    }

    //Proximity trigger Fix
    [HarmonyPatch(typeof(TheForest.SerializableTaskSystem.ProximityCondition), "IsWithinRangeOfTarget")]
    private static class IsWithinRangeOfTargetSerializableFix
    {
        private static bool Prefix(TheForest.SerializableTaskSystem.ProximityCondition __instance, ref bool __result)
        {
            if (__instance._inCaveOnly && !IsInCavesStateManager.IsInCaves)
            {
                __result = false;
                return false;
            }
            if (__instance._use2dDistance)
            {
                __result = Vector2.Distance(new Vector2(__instance._targetObject.position.x, __instance._targetObject.position.z), new Vector2(LocalPlayer.Transform.position.x, LocalPlayer.Transform.position.z)) < __instance._distance;
                return false;
            }
            __result = Vector3.Distance(__instance._targetObject.position, LocalPlayer.Transform.position) < __instance._distance;
            return false;
        }
    }

    [HarmonyPatch(typeof(TheForest.TaskSystem.ProximityCondition), "IsWithinRangeOfTarget")]
    private static class IsWithinRangeOfTargetFix
    {
        private static bool Prefix(TheForest.TaskSystem.ProximityCondition __instance, ref bool __result)
        {
            if (!LocalPlayer.Transform)
            {
                __result = false;
                return false;
            }
            if (__instance._inCaveOnly && !IsInCavesStateManager.IsInCaves)
            {
                __result = false;
                RLog.Msg("returned false");
                return false;
            }
            if (__instance._use2dDistance)
            {
                __result = Vector2.Distance(new Vector2(__instance._targetObject.position.x, __instance._targetObject.position.z), new Vector2(LocalPlayer.Transform.position.x, LocalPlayer.Transform.position.z)) < __instance._distance;
                return false;
            }
            __result = Vector3.Distance(__instance._targetObject.position, LocalPlayer.Transform.position) < __instance._distance;
            return false;
        }
    }

    //Gather Items UI Fix
    //Very aggressive and not very efficient, but it works
    [HarmonyPatch(typeof(Sons.Crafting.Structures.StructureCraftingSystem), "Update")] 
    private static class StructureCraftingSystemFix
    {
        private static void Prefix(Sons.Crafting.Structures.StructureCraftingSystem __instance)
        {
            if (IsInCavesStateManager.IsInCaves && IsInCavesStateManager.ItemCollectUIFix)
            {
                if (IsInCavesStateManager.ChangeIsInCaves == null)
                {
                    IsInCavesStateManager.ChangeIsInCaves = true;
                }
            }
        }

        private static void Postfix(Sons.Crafting.Structures.StructureCraftingSystem __instance)
        {
            if (IsInCavesStateManager.IsInCaves && IsInCavesStateManager.ItemCollectUIFix)
            {
                IsInCavesStateManager.ChangeIsInCaves = false;
                HudGui.Instance.ClearAllRequiredCollectionCounts();
            }
        }
    }

    [HarmonyPatch(typeof(Sons.Crafting.Structures.StructureCraftingSystem), "UpdateRequiredCountUIForAllItems")]
    private static class StructureCraftingSystemFix2
    {
        private static void Prefix(Sons.Crafting.Structures.StructureCraftingSystem __instance)
        {
            if (IsInCavesStateManager.IsInCaves && IsInCavesStateManager.ItemCollectUIFix)
            {
                if (IsInCavesStateManager.ChangeIsInCaves == null)
                {
                    IsInCavesStateManager.ChangeIsInCaves = true;
                }
            }
        }

        private static void Postfix(Sons.Crafting.Structures.StructureCraftingSystem __instance)
        {
            if (IsInCavesStateManager.IsInCaves && IsInCavesStateManager.ItemCollectUIFix)
            {
                IsInCavesStateManager.ChangeIsInCaves = false;
                HudGui.Instance.ClearAllRequiredCollectionCounts();
            }
        }
    }

    [HarmonyPatch(typeof(Sons.Crafting.Structures.StructureCraftingSystem), "RefreshRequiredItemsUi")]
    private static class StructureCraftingSystemFix3
    {
        private static void Prefix(Sons.Crafting.Structures.StructureCraftingSystem __instance)
        {
            if (IsInCavesStateManager.IsInCaves && IsInCavesStateManager.ItemCollectUIFix)
            {
                if (IsInCavesStateManager.ChangeIsInCaves == null)
                {
                    IsInCavesStateManager.ChangeIsInCaves = true;
                }
            }
        }

        private static void Postfix(Sons.Crafting.Structures.StructureCraftingSystem __instance)
        {
            if (IsInCavesStateManager.IsInCaves && IsInCavesStateManager.ItemCollectUIFix)
            {
                IsInCavesStateManager.ChangeIsInCaves = false;
                HudGui.Instance.ClearAllRequiredCollectionCounts();
            }
        }
    }

    //Wetness fix
    [HarmonyPatch(typeof(BloodAndColdScreenOverlay), "UpdateWetnessAndRain")]
    private static class WetnessPatch
    {
        static bool Prefix(BloodAndColdScreenOverlay __instance)
        {
            if (IsInCavesStateManager.IsInCaves == true)
            {
                __instance._bloodColdController.rainAmount.value = 0f;
                return false;
            }
            return true;
        }
    }

    //Weather FIx
    [HarmonyPatch(typeof(WeatherSystem), "CheckInCave")]
    private static class WeatherSystemPatch
    {
        private static bool Prefix()
        {
            RainTypes rainTypes = WeatherSystem.GetRainTypes();
            if (rainTypes == null)
            {
                return false;
            }
            GameObject caveFilter = rainTypes.CaveFilter;
            if (caveFilter == null)
            {
                return false;
            }
            if (IsInCavesStateManager.IsInCaves || (LocalPlayer.Inventory && LocalPlayer.Inventory.CurrentView == PlayerInventory.PlayerViews.PlaneCrash))
            {
                if (rainTypes.CaveFilter.activeSelf)
                {
                    rainTypes.CaveFilter.SetActive(false);
                    return false;
                }
            }
            else if (!caveFilter.activeSelf)
            {
                caveFilter.SetActive(true);
            }
            return false;
        }
    }

    //IsInSnow fix (Honestly dont know what this does)
    [HarmonyPatch(typeof(PlayerStats), "IsInSnow")]
    private static class IsInSnowPatch
    {
        static bool Prefix(ref bool __result)
        {
            if (IsInCavesStateManager.IsInCaves == true)
            {
                __result = false;
                return false;
            }
            return true;
        }
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
            CaveEntranceManager._isInCaves = false;
            IsInCavesStateManager.EnterCave();
            BlueFix();
            SnowFix(false, false);
        }
    }

    [HarmonyPatch(typeof(CaveEntranceManager), "OnCaveExit")]
    private static class ExitPatch
    {
        private static void Prefix()
        {
            CaveEntranceManager._isInCaves = true;
        }
        private static void Postfix()
        {
            CaveEntranceManager._isInCaves = false;
            IsInCavesStateManager.ExitCave();
            UndoBlueFix();
            SnowFix(true, false);
            FixCollectUI();
        }
    }

    [HarmonyPatch(typeof(CaveEntranceManager), "OnUpdateMask")]
    private static class PerpetualPatch
    {
        private static void Prefix()
        {
            CaveEntranceManager._isInCaves = false;
        }
    }

    //Allow changing the IsInCaves state in the CaveEntranceManager

    [HarmonyPatch(typeof(CaveEntranceManager), "UpdateAllPlayerAreaMask")]
    private static class PlayerMaskHijakPatch
    {
        private static void Prefix()
        {
            if(IsInCavesStateManager.ChangeIsInCaves != null)
            {
                if (IsInCavesStateManager.ChangeIsInCaves == true)
                {
                    if (CaveEntranceManager.IsInCaves != IsInCavesStateManager.IsInCaves)
                    {
                        CaveEntranceManager._isInCaves = IsInCavesStateManager.IsInCaves;
                    }
                }
                else if (IsInCavesStateManager.ChangeIsInCaves == false)
                {
                    CaveEntranceManager._isInCaves = false;
                    IsInCavesStateManager.ChangeIsInCaves = null;
                }
                //Do not execute the rest of the code
                return;
            }

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
        rockRemoverTrigger.AddComponent<RockRemoverTrigger>();
        rockRemoverTrigger.AddComponent<RockRemoverPlayerDetectionTrigger>();
    }

    private void CaveAEntranceTriggerCreation()
    {
         
        Vector3 triggerPosition = new Vector3(-423.0533f, 14.3267f, 1511.071f);
        GameObject triggerObject = new GameObject("CaveAEntranceTrigger");

        triggerObject.transform.position = triggerPosition;
        triggerObject.AddComponent<CaveAEntranceFixTrigger>();
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

    public static void FixCollectUI()
    {
        if (IsInCavesStateManager.ItemCollectUIFix)
        {
            StructureCraftingSystem structureCraftingSystem = StructureCraftingSystem._instance;
            structureCraftingSystem.RefreshRequiredItemsUi();
        }
    }
}

