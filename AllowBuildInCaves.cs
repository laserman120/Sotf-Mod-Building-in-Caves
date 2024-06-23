using Construction;
using Sons.Gui;
using SonsSdk;
using UnityEngine;
using SUI;
using RedLoader;
using Sons.Areas;
using HarmonyLib;

namespace AllowBuildInCaves;

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
    }


    protected override void OnGameStart()
    {
        // This is called once the player spawns in the world and gains control.

        //Find the construction manager in the scene.
        //This is the manager that handles all construction in the game.
        DestroyEntrances();
    }

    [HarmonyPatch(typeof(CaveEntranceManager), "OnCaveEnter")]
    private static class EnterPatch
    {
        private static void Postfix()
        {
            CaveEntranceManager._isInCaves = false;
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
                    if (t2.name == "Renderable"){ t2.gameObject.SetActive(false); }
                    if (t2.name == "EntranceTrigger" || t2.name == "ExitTrigger" || t2.name == "QuadBlockerEnterCave" || t2.name == "QuadBlockerExitCave") t2.gameObject.SetActive(false);
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

    private void DestroyEntrances()
    {
        //Adjustments to allow building in caves/cellars
        AdjustHouseCave("CaveG_External");
        AdjustHouseCave("CellarA");
        AdjustCellarCave("CellarN");
        AdjustCellarCave("CellarF");
        AdjustCellarCave("CellarO");
        AdjustCellarCave("CellarE");
        AdjustCellarCave("CellarB");
        AdjustCellarCave("CellarD");
        AdjustCellarCave("CellarK");
        AdjustCellarCave("CellarP");
        AdjustCellarCave("CellarC");
        AdjustCellarCave("CellarL");
        AdjustCellarCave("CellarQ");
        AdjustCellarCave("CellarM");
        AdjustCellarCave("CellarH");
        if (Config.DontOpenCaves.Value) { return; }
        //opening cave entrances
        DestroyCaveEntrance("CaveBExternal");
        DestroyCaveEntrance("CaveCExternal");
        DestroyCaveEntrance("CaveDExternal");
        DestroyCaveEntrance("CaveF_External");
        DestroyCaveEntrance("BE_External");
        DestroyCaveEntrance("BF_External");
        DestroyCaveEntrance("BunkerFExternal");
    }
}