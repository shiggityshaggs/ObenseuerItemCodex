using BepInEx;
using ItemCodex.Utility;
using System;

namespace ItemCodex;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static GUIComponent GUIComponent;

    private void Awake()
    {
        Console.WriteLine($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        GUIComponent = gameObject.AddComponent<GUIComponent>();
        GUIComponent.enabled = false;

        gameObject.AddComponent<InputHandler>();
    }
}
