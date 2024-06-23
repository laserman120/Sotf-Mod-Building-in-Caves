using RedLoader;
using RedLoader.Preferences;

namespace AllowBuildInCaves;

public static class Config
{
    public static ConfigCategory Category { get; private set; }
    public static KeybindConfigEntry ToggleKey { get; private set; }
    //public static ConfigEntry<bool> SomeEntry { get; private set; }

    public static void Init()
    {
        Category = ConfigSystem.CreateFileCategory("AllowBuildInCaves", "AllowBuildInCaves", "AllowBuildInCaves.cfg");

        ToggleKey = Category.CreateKeybindEntry(
               "toggle_key", // Set identifier
               EInputKey.numpadPlus, // Set default input key
               "Quickly toggles the mod", // //Set name displayed in mod menu settings
               "Quickly toggles the mod on or off"); //Set description shown on hovering mouse over displayed name
    }

    // Same as the callback in "CreateSettings". Called when the settings ui is closed.
    public static void OnSettingsUiClosed()
    {
    }
}