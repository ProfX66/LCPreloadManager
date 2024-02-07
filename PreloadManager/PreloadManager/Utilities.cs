using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using Newtonsoft.Json;

namespace BepInEx.PreloadManager
{
    public static class Utilities
    {
        /// <summary>
        /// Internal BepInEx paths
        /// </summary>
        public static Dictionary<string, string> InternalVars = new Dictionary<string, string>
        {
            { "%ConfigPath%", Paths.ConfigPath },
            { "%PluginPath%", Paths.PluginPath }
        };

        /// <summary>
        /// Convert a delimited flat list to a string list
        /// </summary>
        /// <param name="list"></param>
        /// <param name="delimiter"></param>
        /// <returns>List<string></returns>
        public static List<string> ConvertFlatToList(string list, char delimiter = ';')
        {
            if (string.IsNullOrEmpty(list)) return null;
            list = Regex.Replace(list, @"\""", "");
            return list.Split(delimiter).ToList();
        }

        /// <summary>
        /// Resolves the passed list to a final list of just plugin names
        /// </summary>
        /// <param name="list"></param>
        /// <returns>List<string></returns>
        public static List<string> ResolvePluginList(List<string> list)
        {
            if (list == null || list.Count < 1) return null;

            List<string> retList = new List<string>();
            list.ForEach(item =>
            {
                if (Regex.IsMatch(item, @"\.json", RegexOptions.IgnoreCase))
                {
                    string exPath = ExpandVariables(item, InternalVars);
                    if (File.Exists(exPath))
                    {
                        Patcher.Logger.LogMessage($"Found json to load: {exPath}");
                        string fileContent = File.ReadAllText(exPath);
                        List<string> deserializedList = JsonConvert.DeserializeObject<List<string>>(fileContent);
                        retList.AddRange(deserializedList);
                    }
                    else
                    {
                        Patcher.Logger.LogWarning($"Unable to find [ {exPath} ] to load plugin names - Skipping...");
                    }
                }
                else { retList.Add(item); }
            });

            return retList;
        }

        /// <summary>
        /// Gets BepInPlugin information from the passed assembly if it has it
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static List<PluginData> GetPluginInfo(Assembly assembly)
        {
            List<PluginData> retList = null;
            List<Type> bepPlugin = assembly.GetTypes().Where(t => t.GetCustomAttribute<BepInPlugin>() != null).ToList();
            bepPlugin.ForEach(type =>
            {
                BepInPlugin bp = type.GetCustomAttribute<BepInPlugin>();
                PluginData pd = new PluginData
                {
                    name = bp.Name,
                    version = bp.Version,
                    type = type,
                    methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Where(m => m.Name == "Awake").ToList()
                };

                if (retList == null) { retList = new List<PluginData>(); }
                retList.Add(pd);
            });
            return retList;
        }

        /// <summary>
        /// Expands environmental and passed variables in the string
        /// </summary>
        /// <param name="Item"></param>
        /// <param name="Substitutions"></param>
        /// <returns>Expanded String</returns>
        public static string ExpandVariables(string Item, Dictionary<string, string> Substitutions = null)
        {
            string stReturn = Environment.ExpandEnvironmentVariables(Item);
            if (Substitutions != null)
            {
                foreach (KeyValuePair<string, string> item in Substitutions)
                {
                    stReturn = stReturn.Replace(item.Key, item.Value);
                }
            }

            return stReturn;
        }

        /// <summary>
        /// Finds files matching the search pattern list in the passed path
        /// </summary>
        /// <param name="searchPatterns"></param>
        /// <param name="path"></param>
        /// <param name="excludePattern"></param>
        /// <returns>IEnumerable<FileInfo></returns>
        public static IEnumerable<FileInfo> FindFilesParallel(List<string> searchPatterns, string path, string excludePattern = null)
        {
            return searchPatterns.AsParallel().SelectMany(searchPattern => GetManyFileInfo(path, searchPattern, excludePattern));
        }

        /// <summary>
        /// Finds many files recursively
        /// </summary>
        /// <param name="root"></param>
        /// <param name="searchPattern"></param>
        /// <param name="excludePattern"></param>
        /// <returns>IEnumerable<FileInfo></returns>
        public static IEnumerable<FileInfo> GetManyFileInfo(string root, string searchPattern, string excludePattern = null)
        {
            Stack<string> pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count != 0)
            {
                var path = pending.Pop();
                string[] next = null;
                try { next = Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories); }
                catch { }

                if (next != null && next.Length != 0)
                    foreach (var file in next) yield return new FileInfo(file);

                try
                {
                    if (!string.IsNullOrEmpty(excludePattern)) { next = Directory.GetDirectories(path); }
                    else { next = Directory.GetDirectories(path).Where(d => !Regex.IsMatch(d, excludePattern)).ToArray(); }
                    foreach (var subdir in next) pending.Push(subdir);
                }
                catch { }
            }
        }

        /// <summary>
        /// Converts the timespan to a friendly string value
        /// </summary>
        /// <param name="InputObject"></param>
        /// <param name="FormatPattern"></param>
        /// <returns>String</returns>
        public static string ToFriendlyTime(TimeSpan? InputObject, string FormatPattern = "{0:0.00}")
        {
            try
            {
                if (InputObject == null) { return null; }
                if (InputObject.Value.TotalSeconds < 60) { return string.Format(string.Concat(FormatPattern, " Second(s)"), InputObject.Value.TotalSeconds); }
                if (InputObject.Value.TotalMinutes < 60) { return string.Format(string.Concat(FormatPattern, " Minute(s)"), InputObject.Value.TotalMinutes); }
                if (InputObject.Value.TotalHours < 24) { return string.Format(string.Concat(FormatPattern, " Hour(s)"), InputObject.Value.TotalHours); }
                else { return string.Format(string.Concat(FormatPattern, " Day(s)"), InputObject.Value.TotalDays); }
            }
            catch { return null; }
        }
    }
}
