using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using StubosShowXP.Patches;

namespace StubosShowXP;
[BepInPlugin(LCMPluginInfo.PLUGIN_GUID, LCMPluginInfo.PLUGIN_NAME, LCMPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log = null!;
    private void Awake()
    {
        Log = Logger;
        Log.LogInfo($"Plugin {LCMPluginInfo.PLUGIN_NAME} version {LCMPluginInfo.PLUGIN_VERSION} is loaded!");

        var harmony = new Harmony(LCMPluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(ShowXPGain));
    }
}