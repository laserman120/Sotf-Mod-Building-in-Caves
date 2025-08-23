using AllowBuildInCaves.Harmony;
using RedLoader;
using RedLoader.Preferences;

namespace AllowBuildInCaves;

public static class Config
{
    public static ConfigCategory Category { get; private set; }
    public static ConfigEntry<bool> DontOpenCaves { get; private set; }
    public static ConfigEntry<bool> AllowActorsInCaves { get; private set; }
    public static ConfigEntry<bool> GPSLoseSignal { get; private set; }
    public static ConfigEntry<bool> BlueFix { get; private set; }
    public static ConfigEntry<bool> KeepItemsInCutscene { get; private set; }
    public static ConfigEntry<bool> SnowFix { get; private set; }
    public static ConfigEntry<bool> EasyBunkers { get; private set; }
    public static ConfigEntry<bool> ItemCollectUIFix { get; private set; }
    public static ConfigEntry<bool> DebugMode { get; private set; }
    public static KeybindConfigEntry PosKey { get; private set; }

    public static void Init()
    {
        Category = ConfigSystem.CreateFileCategory("AllowBuildInCaves", "AllowBuildInCaves", "AllowBuildInCaves.cfg");

        DontOpenCaves = Category.CreateEntry(
            "dont_enable_caves",
           false,
           "Dont open Caves",
           "Will not open up cave entrances, but still allows building. Applies after restart");

        AllowActorsInCaves = Category.CreateEntry(
            "allow_actors_in_caves",
           false,
           "Allow actors to traverse caves",
           "Allows all actors (including Kelvin) to walk in and out of caves. Requires opening of cave entrances. Applies after restart");

        GPSLoseSignal = Category.CreateEntry(
           "gps_lose_signal",
           true,
           "Allow GPS to lose signal",
           "When enabled the GPS will work as usual instead of always showing the map");

        BlueFix = Category.CreateEntry(
           "blue_fix",
           false,
           "Switch to City filter when entering Cave (Blue Fix)",
           "City filter removes the Blue hue inside caves. BlueFix made by TerroDucky.");

        KeepItemsInCutscene = Category.CreateEntry(
           "keep_items_patch",
           true,
           "Keep Logs or Stones when entering Caves or Bunkers",
           "This will give you the Logs or Stones back when entering a Cave or Bunker through the cutscene");

        EasyBunkers = Category.CreateEntry(
           "easy_bunkers",
           false,
           "Easy transportation of logs or stones into bunkers",
           "Logs or Stones thrown onto a bunker entrance will be teleported inside");

        ItemCollectUIFix = Category.CreateEntry(
           "item_collect_ui_fix",
           true,
           "Disable collect Items UI inside Caves",
           "Will remove the items to collect ui when entering a cave");

        Category = ConfigSystem.CreateFileCategory("DebugOptions", "DebugOptions", "AllowBuildInCaves.cfg");

        DebugMode = Category.CreateEntry(
           "enable_debug_mode",
           false,
           "Enable Debug Mode",
           "Purely for debugging and development purposes, this option can be ignored");

        PosKey = Category.CreateKeybindEntry(
               "menu_key", // Set identifier
               EInputKey.numpad5, // Set default input key
               "[DEBUG OPTION] Create internal path point", // //Set name displayed in mod menu settings
               "Purely for debugging and development purposes, this option can be ignored"); //Set description shown on hovering mouse over displayed name
    }

    // Same as the callback in "CreateSettings". Called when the settings ui is closed.
    public static void OnSettingsUiClosed()
    {
        IsInCavesStateManager.GPSShouldLoseSignal = GPSLoseSignal.Value;
        IsInCavesStateManager.ApplyBlueFix = BlueFix.Value;
        IsInCavesStateManager.AllowItemsDuringAnimation = KeepItemsInCutscene.Value;
        //IsInCavesStateManager.ApplySnowFix = SnowFix.Value;
        IsInCavesStateManager.EnableEasyBunkers = EasyBunkers.Value;


        IsInCavesStateManager.ItemCollectUIFix = ItemCollectUIFix.Value;

        //if (SnowFix.Value && IsInCavesStateManager.IsInCaves) { AllowBuildInCaves.SnowFix(false, true); }
        //if (!SnowFix.Value && IsInCavesStateManager.IsInCaves) { AllowBuildInCaves.SnowFix(true, true); }
        if(ItemCollectUIFix.Value && IsInCavesStateManager.IsInCaves) { HarmonyPatches.RefreshRequiredItemsUiInCave(); }
        if (!ItemCollectUIFix.Value && IsInCavesStateManager.IsInCaves) { HarmonyPatches.RefreshRequiredItemsUI(); }

    }
}