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
        static readonly AccessTools.FieldRef<PMTabCargo, CargoAll> CargoAllRef =
            AccessTools.FieldRefAccess<PMTabCargo, CargoAll>("cargoAll");

        public static CargoAll GetCargoAll(PMTabCargo tab) => CargoAllRef(tab);

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

        public static string Summarize(CargoTemplate t)
        {
            var parts = new List<string>();
            foreach (var item in t.items)
                parts.Add($"{ResourceName(item.id)} {item.mass:0.##}t");
            return string.Join(" · ", parts);
        }

        /// <summary>Snapshot the resources (not fuel, not modules) currently in the cargo lists.</summary>
        public static CargoTemplate SaveCurrent(PMTabCargo tab, string name)
        {
            var cargoAll = GetCargoAll(tab);
            if (cargoAll == null) return null;

            // Merge duplicate resource rows into one template item.
            var byId = new Dictionary<string, double>();
            var order = new List<string>();
            void Collect(List<Cargo> list)
            {
                if (list == null) return;
                foreach (var cargo in list)
                {
                    if (cargo == null || cargo.IsCargoFuelSpecial) continue;
                    if (cargo.resourceTypeType != EResourceTypeType.resorces) continue; // resources only
                    if (cargo.resourceType == null || cargo.cargoMass <= 0) continue;
                    string id = cargo.resourceType.ID;
                    if (!byId.ContainsKey(id)) { byId[id] = 0; order.Add(id); }
                    byId[id] += cargo.cargoMass;
                }
            }
            Collect(cargoAll.listCargo);
            Collect(cargoAll.listCargoToOrbit);

            if (order.Count == 0) return null;

            var template = new CargoTemplate { name = name };
            foreach (string id in order)
                template.items.Add(new TemplateItem { id = id, mass = Math.Round(byId[id], 2) });
            return template;
        }

        /// <summary>
        /// Append the template's resources to the current cargo via the game's own AddCargo path
        /// (which clamps to origin availability and remaining capacity), then trim each added row
        /// down to the template amount.
        /// </summary>
        public static void Apply(PMTabCargo tab, CargoTemplate template)
        {
            var cargoAll = GetCargoAll(tab);
            if (cargoAll == null || template == null) return;

            var all = SerializedMonoBehaviourSingleton<AllScriptableObjectManager>.Instance;
            if (all == null) return;

            foreach (var item in template.items)
            {
                ResourceDefinition rd = all.AllResourceDefinitions.GetByID(item.id);
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
                            added.cargoMass = Math.Min(item.mass, added.cargoMass);
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
