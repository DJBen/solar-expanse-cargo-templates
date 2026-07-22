using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SolarExpanseCargoTemplates
{
    /// <summary>
    /// Minimal JSON reader/writer for the template file. JsonUtility silently produced "{}"
    /// for these types in-game, so serialization is done by hand — the schema is tiny.
    /// </summary>
    internal static class MiniJson
    {
        // ── Write ───────────────────────────────────────────────────────────────────────────

        public static string Write(List<CargoTemplate> templates)
        {
            var sb = new StringBuilder();
            sb.Append("{\n  \"templates\": [");
            for (int i = 0; i < templates.Count; i++)
            {
                var t = templates[i];
                if (i > 0) sb.Append(',');
                sb.Append("\n    { \"name\": ").Append(Quote(t.name))
                  .Append(", \"collapsed\": ").Append(t.collapsed ? "true" : "false")
                  .Append(", \"items\": [");
                for (int j = 0; j < t.items.Count; j++)
                {
                    var item = t.items[j];
                    if (j > 0) sb.Append(',');
                    sb.Append("\n      { \"id\": ").Append(Quote(item.id))
                      .Append(", \"mass\": ").Append(item.mass.ToString("0.##", CultureInfo.InvariantCulture));
                    if (item.module) sb.Append(", \"module\": true");
                    sb.Append(" }");
                }
                if (t.items.Count > 0) sb.Append("\n    ");
                sb.Append("] }");
            }
            if (templates.Count > 0) sb.Append("\n  ");
            sb.Append("]\n}\n");
            return sb.ToString();
        }

        static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\t') sb.Append("\\t");
                else if (c == '\r') sb.Append("\\r");
                else sb.Append(c);
            }
            return sb.Append('"').ToString();
        }

        // ── Read ────────────────────────────────────────────────────────────────────────────

        public static List<CargoTemplate> Parse(string json)
        {
            var result = new List<CargoTemplate>();
            int i = 0;
            object root = ParseValue(json, ref i);
            if (!(root is Dictionary<string, object> rootObj)) return result;
            if (!rootObj.TryGetValue("templates", out object templatesVal) ||
                !(templatesVal is List<object> templatesList)) return result;

            foreach (object tVal in templatesList)
            {
                if (!(tVal is Dictionary<string, object> tObj)) continue;
                var template = new CargoTemplate
                {
                    name = tObj.TryGetValue("name", out object n) ? n as string ?? "Template" : "Template",
                    collapsed = tObj.TryGetValue("collapsed", out object c) && c is bool cb && cb
                };
                if (tObj.TryGetValue("items", out object itemsVal) && itemsVal is List<object> itemsList)
                {
                    foreach (object iVal in itemsList)
                    {
                        if (!(iVal is Dictionary<string, object> iObj)) continue;
                        string id = iObj.TryGetValue("id", out object idVal) ? idVal as string : null;
                        double mass = iObj.TryGetValue("mass", out object mVal) && mVal is double d ? d : 0;
                        bool module = iObj.TryGetValue("module", out object modVal) && modVal is bool mb && mb;
                        if (!string.IsNullOrEmpty(id))
                            template.items.Add(new TemplateItem { id = id, mass = mass, module = module });
                    }
                }
                result.Add(template);
            }
            return result;
        }

        static object ParseValue(string s, ref int i)
        {
            SkipWs(s, ref i);
            if (i >= s.Length) throw new FormatException("unexpected end");
            char c = s[i];
            if (c == '{') return ParseObject(s, ref i);
            if (c == '[') return ParseArray(s, ref i);
            if (c == '"') return ParseString(s, ref i);
            if (c == 't') { Expect(s, ref i, "true"); return true; }
            if (c == 'f') { Expect(s, ref i, "false"); return false; }
            if (c == 'n') { Expect(s, ref i, "null"); return null; }
            return ParseNumber(s, ref i);
        }

        static Dictionary<string, object> ParseObject(string s, ref int i)
        {
            var obj = new Dictionary<string, object>();
            i++; // '{'
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return obj; }
            while (true)
            {
                SkipWs(s, ref i);
                string key = ParseString(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException("expected :");
                i++;
                obj[key] = ParseValue(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length) throw new FormatException("unterminated object");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return obj; }
                throw new FormatException("expected , or }");
            }
        }

        static List<object> ParseArray(string s, ref int i)
        {
            var arr = new List<object>();
            i++; // '['
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return arr; }
            while (true)
            {
                arr.Add(ParseValue(s, ref i));
                SkipWs(s, ref i);
                if (i >= s.Length) throw new FormatException("unterminated array");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return arr; }
                throw new FormatException("expected , or ]");
            }
        }

        static string ParseString(string s, ref int i)
        {
            if (s[i] != '"') throw new FormatException("expected string");
            i++;
            var sb = new StringBuilder();
            while (i < s.Length && s[i] != '"')
            {
                char c = s[i++];
                if (c == '\\' && i < s.Length)
                {
                    char e = s[i++];
                    if (e == 'n') sb.Append('\n');
                    else if (e == 't') sb.Append('\t');
                    else if (e == 'r') sb.Append('\r');
                    else if (e == 'u' && i + 4 <= s.Length)
                    { sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; }
                    else sb.Append(e); // \" \\ \/ and anything else literal
                }
                else sb.Append(c);
            }
            if (i >= s.Length) throw new FormatException("unterminated string");
            i++; // closing quote
            return sb.ToString();
        }

        static double ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && ("+-0123456789.eE".IndexOf(s[i]) >= 0)) i++;
            return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
        }

        static void Expect(string s, ref int i, string word)
        {
            if (i + word.Length > s.Length || s.Substring(i, word.Length) != word)
                throw new FormatException("expected " + word);
            i += word.Length;
        }

        static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++;
        }
    }
}
