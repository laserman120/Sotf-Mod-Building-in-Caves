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
    

    // Add methods to update the state when entering/exiting caves
    public static void EnterCave() => IsInCaves = true;
    public static void ExitCave() => IsInCaves = false;
}
public class AllowBuildInCaves : SonsMod
{
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

        //Config.ToggleKey.Notify(MainToggle);
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
        if (IsInCavesStateManager.ApplySnowFix && IsInCavesStateManager.IsInCaves) { SnowFix(false); }
        DestroyEntrances();
    }

    //Cave Teleport Fix
    [HarmonyPatch(typeof(GatherablePickup), "TryGather")]
    private static class TryGatheringPatch
    {
        private static void Prefix()
        {
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
            SnowFix(true);
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
            SnowFix(false);
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
            SnowFix(true);
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

    private void DestroyCaveEntranceCaveBException(string CaveName)
    {
        GameObject CaveExternal = GameObject.Find(CaveName);
        List<Transform> CaveExternalTransforms = CaveExternal.GetChildren();
        foreach (Transform t in CaveExternalTransforms)
        {
            if (t.name.StartsWith("CaveEntrance") || t.name.StartsWith("CaveEntranceShimmy") || t.name.StartsWith("CaveExitShimmy"))
            {
                if (t.name.EndsWith("exitShimmy")) { continue; }
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

        if (Config.DontOpenCaves.Value) { return; }

        var caveEntranceNames = new List<string> { "CaveCExternal", "CaveDExternal", "CaveF_External", "BE_External", "BF_External", "BunkerFExternal" };
        //opening cave entrances
        foreach (var caveEntranceName in caveEntranceNames)
        {
            DestroyCaveEntrance(caveEntranceName);
        }
        DestroyCaveEntranceCaveBException("CaveBExternal");

        EntranceManagerGroupFix("CaveC_EntranceManagerGroup");
        EntranceManagerGroupFix("CaveBExternal");
        EntranceManagerGroupFix("CaveF_External");
        EntranceManagerGroupFix("CaveEExternal");

        DestroyLuxuryEntrance("CaveEExternal");
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
        if (!IsInCavesStateManager.AllowItemsDuringAnimation) { return; }
        PlayerInventory Inventory = LocalPlayer.Inventory;
        int itemId = Inventory._HeldOnlyItemController_k__BackingField.HeldItem?._itemID ?? 0;
        bool hasLogs = false;
        bool hasStone = false;
        if (itemId == 78) { hasLogs = true; }
        if (itemId == 640) { hasStone = true; }
        if (hasLogs || hasStone)
        {
            IsInCavesStateManager.ItemId = itemId;
            IsInCavesStateManager.ItemAmount = Inventory.AmountOf(itemId, false, false);
            for (int i = 0; i < IsInCavesStateManager.ItemAmount; i++)
            {
                Inventory.HeldOnlyItemController.PutDown(false, false, false, null, 0, 0);
            }
        }
    }

    public static void AddItemsOnExit()
    {
        IsInCavesStateManager.TryAddItems = true;
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
                yield break;
            }

            yield return new WaitForSeconds(0.2f); // Wait for 0.2 seconds before retrying
        }
    }

    public static void SnowFix(bool EnableSnow)
    {
        if (!IsInCavesStateManager.ApplySnowFix) { return; }
        SeasonsManager SeasonManager = GameObject.Find("SeasonsManager").GetComponent<SeasonsManager>();
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

