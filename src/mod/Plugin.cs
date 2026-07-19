using BepInEx;
using HarmonyLib;

namespace SolarExpanseCargoTemplates
{
    [BepInPlugin("com.djben.solar-expanse-cargo-templates", "Solar Expanse Cargo Templates", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static BepInEx.Logging.ManualLogSource Log { get; private set; }

        void Awake()
        {
            Log = base.Logger;
            Log.LogInfo("Solar Expanse Cargo Templates loaded");
            new Harmony("com.djben.solar-expanse-cargo-templates").PatchAll();
        }
    }
}
