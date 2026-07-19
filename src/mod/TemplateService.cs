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

        /// <summary>
        /// Buildings the player can currently construct (unlocked, not locked/obsolete) that have
        /// a resource cost — the "add from building cost" picker list.
        /// </summary>
        public static List<Data.ScriptableObject.FacilityBaseDescriptor> AvailableBuildings()
        {
            var result = new List<Data.ScriptableObject.FacilityBaseDescriptor>();
            var all = SerializedMonoBehaviourSingleton<AllScriptableObjectManager>.Instance;
            if (all == null || all.AllFacility == null) return result;
            var player = MonoBehaviourSingleton<GameManager>.Instance != null
                ? MonoBehaviourSingleton<GameManager>.Instance.Player : null;
            foreach (var f in all.AllFacility.ListNotEmpty)
            {
                if (f == null) continue;
                // IsLocked only means "research-gated" — IsUnlockFacility already returns true
                // for both never-locked facilities and ones the player has since unlocked.
                if (player != null && !player.IsUnlockFacility(f)) continue;
                if (f.Price == null || f.Price.ListResources == null || f.Price.ListResources.Count == 0) continue;
                result.Add(f);
            }
            return result;
        }

        /// <summary>"[icon] 120t · [icon] 30t" for a building's construction cost.</summary>
        public static string SummarizePrice(Data.ScriptableObject.FacilityBaseDescriptor f)
        {
            var parts = new List<string>();
            foreach (var one in f.Price.ListResources)
            {
                var rd = one.ResourceDefinition;
                if (rd == null) continue;
                string icon;
                try { icon = rd.IconString + " "; } catch { icon = ResourceName(rd.ID) + " "; }
                parts.Add($"{icon}{FormatMass(one.Price)}");
            }
            return string.Join(" · ", parts);
        }

        /// <summary>Merge a building's resource cost into a template (summing same-resource items).</summary>
        public static void AddBuildingCost(CargoTemplate t, Data.ScriptableObject.FacilityBaseDescriptor f)
        {
            foreach (var one in f.Price.ListResources)
            {
                var rd = one.ResourceDefinition;
                if (rd == null || one.Price <= 0) continue;
                string id = rd.ID;
                var existing = t.items.Find(x => x.id == id);
                if (existing != null) existing.mass = Math.Round(existing.mass + one.Price, 2);
                else t.items.Add(new TemplateItem { id = id, mass = Math.Round(one.Price, 2) });
            }
        }

        /// <summary>All resources the game allows as cargo — the manager window's picker list.</summary>
        public static List<ResourceDefinition> AllCargoResources()
        {
            var all = SerializedMonoBehaviourSingleton<AllScriptableObjectManager>.Instance;
            if (all == null) return new List<ResourceDefinition>();
            return all.AllResourceDefinitions.ListResourceDefinitionTakeAsCargo ?? new List<ResourceDefinition>();
        }

        /// <summary>
        /// "&lt;sprite …&gt; Name" — icon + name via the game's own TMP sprite markup
        /// (ResourceDefinition.IconString), falling back to plain name if the resource is unknown.
        /// </summary>
        public static string ResourceLabel(string id)
        {
            ResourceDefinition rd = GetResource(id);
            if (rd == null) return ResourceName(id);
            try { return rd.IconString + " " + ResourceName(id); }
            catch { return ResourceName(id); }
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
        /// Rich-text "[icon] 100t · [icon] 10t" summary of a template at a given multiplier.
        /// Items whose required amount exceeds the origin's stock (or whose resource is unknown)
        /// have their amount rendered red. Meant for a wrapping multi-line label.
        /// </summary>
        /// <summary>"850t", "15kt", "1.5Mt" — compact tonnage for summaries.</summary>
        public static string FormatMass(double tons)
        {
            if (tons >= 1e6) return $"{tons / 1e6:0.##}Mt";
            if (tons >= 1e3) return $"{tons / 1e3:0.##}kt";
            return $"{tons:0.##}t";
        }

        public static string SummarizeForOrigin(PMTabCargo tab, CargoTemplate t, int multiplier)
        {
            var parts = new List<string>();
            foreach (var item in t.items)
            {
                double needed = item.mass * multiplier;
                ResourceDefinition rd = GetResource(item.id);
                bool short_ = rd == null || AvailableAtOrigin(tab, rd) + 1e-9 < needed;
                string icon;
                try { icon = rd != null ? rd.IconString + " " : ""; } catch { icon = ""; }
                string amount = FormatMass(needed);
                // The sprite ignores the color tag; the amount text carries the red signal.
                parts.Add(icon + (short_ ? $"<color={RedHex}>{amount}</color>" : amount));
            }
            return string.Join("  ·  ", parts);
        }

        public static string Summarize(CargoTemplate t)
        {
            var parts = new List<string>();
            foreach (var item in t.items)
                parts.Add($"{ResourceName(item.id)} {FormatMass(item.mass)}");
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
