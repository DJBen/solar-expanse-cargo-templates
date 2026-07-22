using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace SolarExpanseCargoTemplates
{
    [BepInPlugin("com.djben.solar-expanse-cargo-templates", "Solar Expanse Cargo Templates", "1.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static BepInEx.Logging.ManualLogSource Log { get; private set; }

        internal static ConfigEntry<bool> CfgShowUnresearched;

        void Awake()
        {
            Log = base.Logger;
            CfgShowUnresearched = Config.Bind("UI", "ShowUnresearched", false,
                "Include unresearched buildings, spacecraft and launch vehicles in the cost pickers.");
            Log.LogInfo("Solar Expanse Cargo Templates loaded");
            new Harmony("com.djben.solar-expanse-cargo-templates").PatchAll();
        }
    }
}
