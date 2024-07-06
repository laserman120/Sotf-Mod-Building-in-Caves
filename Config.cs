using RedLoader;
using RedLoader.Preferences;

namespace AllowBuildInCaves;

public static class Config
{
    public static ConfigCategory Category { get; private set; }
    public static ConfigEntry<bool> DontOpenCaves { get; private set; }
    public static ConfigEntry<bool> GPSLoseSignal { get; private set; }
    public static ConfigEntry<bool> BlueFix { get; private set; }
    public static ConfigEntry<bool> KeepItemsInCutscene { get; private set; }
    public static ConfigEntry<bool> SnowFix { get; private set; }

    //public static KeybindConfigEntry ToggleKey { get; private set; }

    public static void Init()
    {
        Category = ConfigSystem.CreateFileCategory("AllowBuildInCaves", "AllowBuildInCaves", "AllowBuildInCaves.cfg");

        DontOpenCaves = Category.CreateEntry(
            "dont_enable_caves",
           false,
           "Dont open Caves",
           "Will not open up cave entrances, but still allows building. Applies after restart");

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

        SnowFix = Category.CreateEntry(
           "snow_fix",
           true,
           "Disables snow when inside a cave",
           "Will disable the snow on builds when entering caves");
        
            //ToggleKey = Category.CreateKeybindEntry(
            //"toggle_key",
           //EInputKey.numpadMinus,
           // "Key to toggle the mod",
            //"Does it matter at this point ffs");
    }

    // Same as the callback in "CreateSettings". Called when the settings ui is closed.
    public static void OnSettingsUiClosed()
    {
        IsInCavesStateManager.GPSShouldLoseSignal = GPSLoseSignal.Value;
        IsInCavesStateManager.ApplyBlueFix = BlueFix.Value;
        IsInCavesStateManager.AllowItemsDuringAnimation = KeepItemsInCutscene.Value;
        IsInCavesStateManager.ApplySnowFix = SnowFix.Value;

        if (SnowFix.Value && IsInCavesStateManager.IsInCaves) { AllowBuildInCaves.SnowFix(false, true); }
        if (!SnowFix.Value && IsInCavesStateManager.IsInCaves) { AllowBuildInCaves.SnowFix(true, true); }
    }
}