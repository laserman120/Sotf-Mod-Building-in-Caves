using RedLoader;
using RedLoader.Preferences;

namespace AllowBuildInCaves;

public static class Config
{
    public static ConfigCategory Category { get; private set; }
    public static ConfigEntry<bool> DontOpenCaves { get; private set; }
    //public static ConfigEntry<bool> SomeEntry { get; private set; }

    public static void Init()
    {
        Category = ConfigSystem.CreateFileCategory("AllowBuildInCaves", "AllowBuildInCaves", "AllowBuildInCaves.cfg");

        DontOpenCaves = Category.CreateEntry(
            "dont_enable_caves",
           false,
           "Dont open Caves",
           "Will not open up cave entrances, but still allows building. Applies after restart");
    }

    // Same as the callback in "CreateSettings". Called when the settings ui is closed.
    public static void OnSettingsUiClosed()
    {
    }
}