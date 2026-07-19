using Game.UI.Windows.Elements.PlanMissionElements;
using Game.UI.Windows.Windows;
using HarmonyLib;
using SolarExpanseCargoTemplates.UI;

namespace SolarExpanseCargoTemplates.Patches
{
    /// <summary>Inject the templates button every time the Cargo step becomes active.</summary>
    [HarmonyPatch(typeof(PMTabCargo), "ActiveTabPart1")]
    static class PMTabCargoActivatePatch
    {
        static void Postfix(PMTabCargo __instance)
        {
            // Regular Plan Mission window only — never cyclical missions, never the CargoB stage.
            if (__instance.PlanMissionWindow is PlanCyclicalMissionWindow) return;
            if (__instance.Stage != PlanMissionWindow.EStageWindow.Cargo) return;
            TemplatesUI.EnsureInjected(__instance);
        }
    }

    /// <summary>Close the dropdown when the Cargo step deactivates (tab change / window close).</summary>
    [HarmonyPatch(typeof(PMTabCargo), "DeActivateTab")]
    static class PMTabCargoDeactivatePatch
    {
        static void Postfix() => TemplatesUI.HidePanel();
    }

    /// <summary>Also close it when the window resets its state.</summary>
    [HarmonyPatch(typeof(PMTabCargo), nameof(PMTabCargo.ClearVariable))]
    static class PMTabCargoClearPatch
    {
        static void Postfix() => TemplatesUI.HidePanel();
    }
}
