using System;
using System.Collections.Generic;
using Data.ScriptableObject;
using Game.ObjectInfoDataScripts;
using Game.UI.Windows.Elements.PlanMissionElements;
using HarmonyLib;
using Language;
using Manager;
using ScriptableObjectScripts;

namespace SolarExpanseCargoTemplates
{
    /// <summary>Reads/applies templates against the live PMTabCargo. Resources only — no modules.</summary>
    public static class TemplateService
    {
        public const string RedHex = "#FF6B6B";

        static readonly AccessTools.FieldRef<PMTabCargo, CargoAll> CargoAllRef =
            AccessTools.FieldRefAccess<PMTabCargo, CargoAll>("cargoAll");

        public static CargoAll GetCargoAll(PMTabCargo tab) => CargoAllRef(tab);

        public static ResourceDefinition GetResource(string id)
        {
            var all = SerializedMonoBehaviourSingleton<AllScriptableObjectManager>.Instance;
            return all != null ? all.AllResourceDefinitions.GetByID(id) : null;
        }

        /// <summary>All resources the game allows as cargo — the manager window's picker list.</summary>
        public static List<ResourceDefinition> AllCargoResources()
        {
            var all = SerializedMonoBehaviourSingleton<AllScriptableObjectManager>.Instance;
            if (all == null) return new List<ResourceDefinition>();
            return all.AllResourceDefinitions.ListResourceDefinitionTakeAsCargo ?? new List<ResourceDefinition>();
        }

        /// <summary>Resource name for UI labels, falling back to the raw id.</summary>
        public static string ResourceName(string id)
        {
            try
            {
                string text = LEManager.Get(id);
                if (!string.IsNullOrEmpty(text)) return text;
            }
            catch { /* localization not ready — fall through */ }
            return id;
        }

        /// <summary>How much of a resource the mission's origin currently has, or 0.</summary>
        public static double AvailableAtOrigin(PMTabCargo tab, ResourceDefinition rd)
        {
            try
            {
                var p = tab.PlanMissionWindow.PMMissionParameter;
                var data = p.Start != null ? p.Start.GetObjectInfoData(p.FlyCompany) : null;
                return data != null ? data.CheckResources(rd) : 0.0;
            }
            catch { return 0.0; }
        }

        /// <summary>
        /// Rich-text summary of a template at a given multiplier. Items whose required amount
        /// exceeds the origin's stock (or whose resource is unknown) are rendered red.
        /// </summary>
        public static string SummarizeForOrigin(PMTabCargo tab, CargoTemplate t, int multiplier)
        {
            var parts = new List<string>();
            foreach (var item in t.items)
            {
                double needed = item.mass * multiplier;
                ResourceDefinition rd = GetResource(item.id);
                bool short_ = rd == null || AvailableAtOrigin(tab, rd) + 1e-9 < needed;
                string text = $"{ResourceName(item.id)} {needed:0.##}t";
                parts.Add(short_ ? $"<color={RedHex}>{text}</color>" : text);
            }
            return string.Join(" · ", parts);
        }

        public static string Summarize(CargoTemplate t)
        {
            var parts = new List<string>();
            foreach (var item in t.items)
                parts.Add($"{ResourceName(item.id)} {item.mass:0.##}t");
            return string.Join(" · ", parts);
        }

        /// <summary>
        /// Append the template's resources (× multiplier) to the current cargo via the game's own
        /// AddCargo path (which clamps to origin availability and remaining capacity), then trim
        /// each added row down to the requested amount.
        /// </summary>
        public static void Apply(PMTabCargo tab, CargoTemplate template, int multiplier)
        {
            var cargoAll = GetCargoAll(tab);
            if (cargoAll == null || template == null) return;
            if (multiplier < 1) multiplier = 1;

            foreach (var item in template.items)
            {
                ResourceDefinition rd = GetResource(item.id);
                if (rd == null)
                {
                    Plugin.Log.LogWarning($"Template resource not found, skipping: {item.id}");
                    continue;
                }
                try
                {
                    int before = cargoAll.listCargo.Count;
                    tab.AddCargo(rd); // game clamps mass to availability + capacity
                    if (cargoAll.listCargo.Count > before)
                    {
                        Cargo added = cargoAll.listCargo[cargoAll.listCargo.Count - 1];
                        if (added.resourceType == rd)
                            added.cargoMass = Math.Min(item.mass * multiplier, added.cargoMass);
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Failed to add {item.id} from template: {e}");
                }
            }
            tab.SetDataResourcesList();
        }
    }
}
