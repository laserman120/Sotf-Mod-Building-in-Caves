using RedLoader;
using RedLoader.Preferences;

namespace AllowBuildInCaves;

public static class Config
{
    public static ConfigCategory Category { get; private set; }
    public static ConfigEntry<bool> DontOpenCaves { get; private set; }
    public static ConfigEntry<bool> GPSLoseSignal { get; private set; }

    //public static KeybindConfigEntry ToggleKey { get; private set; }

    //public static KeybindConfigEntry ToggleKey2 { get; private set; }
    //public static ConfigEntry<bool> SomeEntry { get; private set; }

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
    /*
        ToggleKey = Category.CreateKeybindEntry(
        "toggle_key",
        EInputKey.numpadMinus,
        "Key to toggle the mod",
        "Does it matter at this point ffs");

        ToggleKey2 = Category.CreateKeybindEntry(
        "toggle_key2",
        EInputKey.numpadMinus,
        "Key to toggle the mod",
        "Does it matter at this point ffs");*/
    }

    // Same as the callback in "CreateSettings". Called when the settings ui is closed.
    public static void OnSettingsUiClosed()
    {
        IsInCavesStateManager.GPSShouldLoseSignal = GPSLoseSignal.Value;
    }
}