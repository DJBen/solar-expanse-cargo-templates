using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace SolarExpanseCargoTemplates
{
    public class TemplateItem
    {
        // ResourceDefinition.ID (e.g. "id_resource_metal") or, for modules, the
        // SpaceModuleDescriptor's ID — the game's stable save identifiers.
        public string id;
        // Tons for resources; module COUNT for module items.
        public double mass;
        public bool module;
    }

    public class CargoTemplate
    {
        public string name;
        public bool collapsed; // editor-window fold state; persisted so it survives reopening
        public List<TemplateItem> items = new List<TemplateItem>();
    }

    /// <summary>Global (cross-save) template persistence in BepInEx/config.</summary>
    public static class TemplateStore
    {
        static string PathToFile => Path.Combine(Paths.ConfigPath, "SolarExpanseCargoTemplates.json");

        public static List<CargoTemplate> Load()
        {
            try
            {
                if (File.Exists(PathToFile))
                    return MiniJson.Parse(File.ReadAllText(PathToFile));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to load templates: {e}");
            }
            return new List<CargoTemplate>();
        }

        public static void Save(List<CargoTemplate> templates)
        {
            try
            {
                File.WriteAllText(PathToFile, MiniJson.Write(templates));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to save templates: {e}");
            }
        }
    }
}
