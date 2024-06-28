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

namespace AllowBuildInCaves;

public static class IsInCavesStateManager
{
    public static bool IsInCaves { get; private set; } // Store the real state
    public static bool? ChangeIsInCaves { get; set; } = null; // Store the state that will be used in the game
    public static bool GPSShouldLoseSignal { get; set; } = false; // Store the state that will be used in the game
    public static bool ApplyBlueFix { get; set; } = false; // Store the state that will be used in the game

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
        if(IsInCavesStateManager.ApplyBlueFix && IsInCavesStateManager.IsInCaves) { BlueFix(); }
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
            } else
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
            if(__instance._divingStarted && IsInCavesStateManager.IsInCaves == true)
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
            if(IsInCavesStateManager.IsInCaves == true && !__instance._manuallyTriggeredPowerState && !__instance._isPowerOn)
            {
                __instance.PowerOn(true);
            }
        }
    }

    //IsInSnow fix (Honestly dont know what this does)
    [HarmonyPatch(typeof(PlayerStats), "IsInSnow")]
    private static class IsInSnowPatch
    {
        static bool Prefix(ref bool __result)
        {
            if(IsInCavesStateManager.IsInCaves == true)
            {
                __result = false;
                return false;
            }
            return true;
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
                if(t.name.EndsWith("exitShimmy")) { continue; }
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
            if (!t.name.StartsWith("Lighting")) { continue;}    
            List<Transform> CaveEntranceTransforms = t.gameObject.GetChildren();
            foreach (Transform t2 in CaveEntranceTransforms)
            {
                if (t2.name != "InternalLightingGrp") { continue;}
                    
                List<Transform> CaveEntranceTransforms2 = t2.gameObject.GetChildren();
                foreach (Transform t3 in CaveEntranceTransforms2)
                {
                    if (t3.name != "InternalLighting") { continue;}

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
            if(t.name.EndsWith("CaveEntrance") || t.name.EndsWith("Cavexit"))
            {
                //Fetch the component Endnight.Utilities.DirectionalTrigger
                DirectionalTrigger dt = t.GetComponent<DirectionalTrigger>();
                if(dt != null) { dt._useInnerBoundary = true; }
                if (t.name.EndsWith("Cavexit"))
                {
                    t.localScale = new Vector3(0.1f, 0.7f, 0.5f);
                } else
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
                if(dt != null) { dt._useInnerBoundary = true; }
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

        var caveEntranceNames = new List<string> {"CaveCExternal", "CaveDExternal", "CaveF_External", "BE_External", "BF_External", "BunkerFExternal" };
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
        if(!IsInCavesStateManager.ApplyBlueFix) {
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
}
