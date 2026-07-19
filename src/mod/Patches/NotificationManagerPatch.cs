using HarmonyLib;
using Manager;
using SolarExpanseCargoTemplates.UI;

namespace SolarExpanseCargoTemplates.Patches
{
    /// <summary>Inject the standalone CARGO TEMPLATES top-bar button once the top bar exists.</summary>
    [HarmonyPatch(typeof(NotificationManager), "Awake")]
    internal static class NotificationManagerPatch
    {
        [HarmonyPostfix]
        static void Postfix(NotificationManager __instance)
        {
            Plugin.Log.LogInfo("[CT] NotificationManager.Awake postfix — injecting");
            ManagerWindow.Inject(__instance);
        }
    }
}
