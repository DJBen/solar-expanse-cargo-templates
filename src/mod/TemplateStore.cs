using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace SolarExpanseCargoTemplates
{
    [Serializable]
    public class TemplateItem
    {
        // ResourceDefinition.ID, e.g. "id_resource_metal" — the game's stable save identifier.
        public string id;
        public double mass;
    }

    [Serializable]
    public class CargoTemplate
    {
        public string name;
        public List<TemplateItem> items = new List<TemplateItem>();
    }

    [Serializable]
    public class TemplateFile
    {
        public List<CargoTemplate> templates = new List<CargoTemplate>();
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
                {
                    var file = JsonUtility.FromJson<TemplateFile>(File.ReadAllText(PathToFile));
                    if (file != null && file.templates != null) return file.templates;
                }
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
                var file = new TemplateFile { templates = templates };
                File.WriteAllText(PathToFile, JsonUtility.ToJson(file, prettyPrint: true));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Failed to save templates: {e}");
            }
        }
    }
}
