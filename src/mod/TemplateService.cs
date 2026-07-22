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
                if (player != null && !player.IsUnlockFacility(f) && !ShowUnresearched) continue;
                if (f.Price == null || f.Price.ListResources == null || f.Price.ListResources.Count == 0) continue;
                result.Add(f);
            }
            return result;
        }

        /// <summary>An unlocked spacecraft or launch vehicle with its construction cost.</summary>
        public class CraftCost
        {
            public string id;
            public UnityEngine.Sprite sprite;
            public Game.UI.Windows.Elements.SpaceCraftConstructElements.ResourcePrice price;
            public bool locked; // not yet researched by the player
        }

        static Game.Company Player => MonoBehaviourSingleton<GameManager>.Instance != null
            ? MonoBehaviourSingleton<GameManager>.Instance.Player : null;

        static bool ShowUnresearched => Plugin.CfgShowUnresearched != null && Plugin.CfgShowUnresearched.Value;

        public static bool IsBuildingUnlocked(Data.ScriptableObject.FacilityBaseDescriptor f)
        {
            var player = Player;
            return player == null || player.IsUnlockFacility(f);
        }

        /// <summary>A space module type that can be part of a template.</summary>
        public class ModuleOption
        {
            public string id;
            public UnityEngine.Sprite sprite;
            public bool locked;
            public int crewCapacity; // > 0 for crew-transport modules
        }

        /// <summary>Module types for the editor picker (unlocked, or all with ShowUnresearched).</summary>
        public static List<ModuleOption> AvailableModuleTypes()
        {
            var result = new List<ModuleOption>();
            var all = SerializedMonoBehaviourSingleton<AllScriptableObjectManager>.Instance;
            if (all == null || all.AllFacility == null) return result;
            var player = Player;
            foreach (var f in all.AllFacility.ListNotEmpty)
            {
                if (!(f is SpaceModuleDescriptor smd)) continue;
                bool locked = player != null && !player.IsUnlockFacility(smd);
                if (locked && !ShowUnresearched) continue;
                int crew = smd.specialAbilityFacilityNew.HasFlag(ESpecialAbilityFacilityNew.CrewTransport)
                    ? (int)smd.specialAbilityParameter : 0;
                result.Add(new ModuleOption { id = smd.ID, sprite = smd.Sprite, locked = locked, crewCapacity = crew });
            }
            return result;
        }

        /// <summary>
        /// Live module instances of the given descriptor id at the mission origin that are not
        /// already assigned to the current cargo.
        /// </summary>
        public static List<SpaceModule> AvailableModuleInstances(PMTabCargo tab, string descriptorId)
        {
            var result = new List<SpaceModule>();
            try
            {
                var p = tab.PlanMissionWindow.PMMissionParameter;
                var player = Player;
                if (p.Start == null || player == null) return result;

                var used = new HashSet<SpaceModule>();
                var cargoAll = GetCargoAll(tab);
                if (cargoAll != null)
                {
                    void CollectUsed(List<Cargo> list)
                    {
                        if (list == null) return;
                        foreach (var c in list)
                            if (c != null && c.SourceModule != null) used.Add(c.SourceModule);
                    }
                    CollectUsed(cargoAll.listCargo);
                    CollectUsed(cargoAll.listCargoToOrbit);
                    CollectUsed(cargoAll.listCargoGravityAssists);
                }

                foreach (var sm in p.Start.GetAvailableModulesForCargo(player, null))
                {
                    if (sm == null || used.Contains(sm)) continue;
                    if (sm.facilityDescriptor != null && sm.facilityDescriptor.ID == descriptorId)
                        result.Add(sm);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"AvailableModuleInstances failed: {e}");
            }
            return result;
        }

        /// <summary>Unlocked spacecraft + launch vehicles that have a resource cost.</summary>
        public static List<CraftCost> AvailableCraft()
        {
            var result = new List<CraftCost>();
            var all = SerializedMonoBehaviourSingleton<AllScriptableObjectManager>.Instance;
            if (all == null) return result;
            var player = MonoBehaviourSingleton<GameManager>.Instance != null
                ? MonoBehaviourSingleton<GameManager>.Instance.Player : null;

            if (all.AllSpacecraftType != null)
            {
                foreach (var sc in all.AllSpacecraftType.ListNotEmpty)
                {
                    if (sc == null) continue;
                    bool locked = player != null && !player.IsUnlockSpacecraftType(sc);
                    if (locked && !ShowUnresearched) continue;
                    var price = sc.PriceBase;
                    if (price == null || price.ListResources == null || price.ListResources.Count == 0) continue;
                    result.Add(new CraftCost { id = sc.ID, sprite = sc.RocketBackGround, price = price, locked = locked });
                }
            }
            if (all.AllLaunchVehicleType != null)
            {
                foreach (var lv in all.AllLaunchVehicleType.ListNotEmpty)
                {
                    if (lv == null) continue;
                    bool locked = player != null && !player.IsUnlockRocketType(lv);
                    if (locked && !ShowUnresearched) continue;
                    var price = lv.PriceBase;
                    if (price == null || price.ListResources == null || price.ListResources.Count == 0) continue;
                    result.Add(new CraftCost { id = lv.ID, sprite = lv.rocketBackGround, price = price, locked = locked });
                }
            }
            return result;
        }

        /// <summary>"[icon] 120t · [icon] 30t" for a construction cost.</summary>
        public static string SummarizePrice(Game.UI.Windows.Elements.SpaceCraftConstructElements.ResourcePrice price)
        {
            var parts = new List<string>();
            foreach (var one in price.ListResources)
            {
                var rd = one.ResourceDefinition;
                if (rd == null) continue;
                string icon;
                try { icon = rd.IconString + " "; } catch { icon = ResourceName(rd.ID) + " "; }
                parts.Add($"{icon}{FormatMass(one.Price)}");
            }
            return string.Join(" · ", parts);
        }

        public static string SummarizePrice(Data.ScriptableObject.FacilityBaseDescriptor f)
            => SummarizePrice(f.Price);

        /// <summary>Merge a construction cost into a template (summing same-resource items).</summary>
        public static void AddCost(CargoTemplate t, Game.UI.Windows.Elements.SpaceCraftConstructElements.ResourcePrice price)
        {
            foreach (var one in price.ListResources)
            {
                var rd = one.ResourceDefinition;
                if (rd == null || one.Price <= 0) continue;
                string id = rd.ID;
                var existing = t.items.Find(x => x.id == id);
                if (existing != null) existing.mass = Math.Round(existing.mass + one.Price, 2);
                else t.items.Add(new TemplateItem { id = id, mass = Math.Round(one.Price, 2) });
            }
        }

        public static void AddBuildingCost(CargoTemplate t, Data.ScriptableObject.FacilityBaseDescriptor f)
            => AddCost(t, f.Price);

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
                if (item.module)
                {
                    int needed = (int)Math.Round(item.mass) * multiplier;
                    int avail = AvailableModuleInstances(tab, item.id).Count;
                    string text = $"{needed}× {ResourceName(item.id)}";
                    parts.Add(avail < needed ? $"<color={RedHex}>{text}</color>" : text);
                    continue;
                }
                double neededMass = item.mass * multiplier;
                ResourceDefinition rd = GetResource(item.id);
                bool short_ = rd == null || AvailableAtOrigin(tab, rd) + 1e-9 < neededMass;
                string icon;
                try { icon = rd != null ? rd.IconString + " " : ""; } catch { icon = ""; }
                string amount = FormatMass(neededMass);
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
                if (item.module)
                {
                    try { ApplyModuleItem(tab, cargoAll, item, multiplier); }
                    catch (Exception e) { Plugin.Log.LogError($"Failed to add module {item.id}: {e}"); }
                    continue;
                }
                ResourceDefinition rd = GetResource(item.id);
                if (rd == null)
                {
                    Plugin.Log.LogWarning($"Template resource not found, skipping: {item.id}");
                    continue;
                }
                try
                {
                    double want = item.mass * multiplier;

                    // Merge into an existing row of the same resource: a duplicate row would make
                    // the game's ChangeCargoToMaxOnPlanet re-clamp the per-planet total and zero
                    // out the earlier row.
                    Cargo existing = cargoAll.listCargo.Find(c =>
                        c != null && !c.IsCargoFuelSpecial &&
                        c.resourceTypeType == EResourceTypeType.resorces && c.resourceType == rd);
                    if (existing != null)
                    {
                        double othersSame = 0;
                        foreach (var c in cargoAll.listCargo)
                            if (c != null && c != existing && c.resourceType == rd) othersSame += c.cargoMass;
                        foreach (var c in cargoAll.listCargoToOrbit)
                            if (c != null && c.resourceType == rd) othersSame += c.cargoMass;

                        double target = existing.cargoMass + want;
                        double stock = AvailableAtOrigin(tab, rd);
                        target = Math.Min(target, Math.Max(stock - othersSame, existing.cargoMass));
                        target = Math.Min(target, existing.cargoMass + Math.Max(cargoAll.FreeSpace, 0));
                        existing.cargoMass = Math.Round(Math.Max(existing.cargoMass, target), 2);
                        continue;
                    }

                    int before = cargoAll.listCargo.Count;
                    tab.AddCargo(rd); // game clamps mass to availability + capacity
                    if (cargoAll.listCargo.Count > before)
                    {
                        Cargo added = cargoAll.listCargo[cargoAll.listCargo.Count - 1];
                        if (added.resourceType == rd)
                            added.cargoMass = Math.Min(want, added.cargoMass);
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Failed to add {item.id} from template: {e}");
                }
            }
            tab.SetDataResourcesList();
        }

        /// <summary>
        /// Add up to count×multiplier instances of a module type from the origin. Crew modules get
        /// full crew by default (the game's AddCargo does that); if the origin doesn't have enough
        /// people left, the crew value is trimmed down — partial crew — mirroring the game's own
        /// SliderCrewChange clamping.
        /// </summary>
        static void ApplyModuleItem(PMTabCargo tab, CargoAll cargoAll, TemplateItem item, int multiplier)
        {
            int wanted = (int)Math.Round(item.mass) * multiplier;
            if (wanted <= 0) return;
            var instances = AvailableModuleInstances(tab, item.id);
            if (instances.Count < wanted)
                Plugin.Log.LogInfo($"Template wants {wanted}x {item.id}, only {instances.Count} available at origin");

            var origin = tab.PlanMissionWindow.PMMissionParameter.Start;
            var player = Player;

            for (int i = 0; i < wanted && i < instances.Count; i++)
            {
                int before = cargoAll.listCargo.Count;
                tab.AddCargo(instances[i], setDataResourcesListInvoke: false);
                if (cargoAll.listCargo.Count <= before) continue;

                Cargo added = cargoAll.listCargo[cargoAll.listCargo.Count - 1];
                if (added.crewValue > 0 && origin != null && player != null)
                {
                    int availablePeople = origin.CurrentCrewAvailableToFly(player);
                    int totalCrew = cargoAll.HowMuchCrew();
                    if (totalCrew > availablePeople)
                        added.crewValue = Math.Max(added.crewValue - (totalCrew - availablePeople), 0);
                }
            }
        }
    }
}
