using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BepInEx.PreloadManager
{
    public static class Patcher
    {
        public static string Prefix => "PXC";
        public static string Id => "PreloadManager";
        public static string JsonPattern => $"{Id}.json";
        public static List<string> PluginToNeuter { get; set; }
        public static IEnumerable<string> TargetDLLs { get; } = new string[] { };

        public static Logging.ManualLogSource Logger = Logging.Logger.CreateLogSource(Id);
        private static readonly ConfigFile Config = new ConfigFile(Path.Combine(Paths.ConfigPath, $"{Prefix}.{Id}.cfg"), true);
        public static ConfigEntry<bool> DisablePreloader;
        public static ConfigEntry<bool> DisableJsonSearch;
        public static ConfigEntry<string> ModList;

        /// <summary>
        /// Initialize preloader
        /// </summary>
        public static void Initialize()
        {
            DateTime start = DateTime.Now;
            string category = "Preloader";
            DisablePreloader = Config.Bind<bool>(category, "Disable Preloader", false, "This will disable this preloader entirely");
            DisableJsonSearch = Config.Bind<bool>(category, "Disable Json Search", false, $"Disable searching for any '{JsonPattern}' files in the config and plugin directories");
            ModList = Config.Bind<string>(category, "Mod List", "", "This is the list of mods to patch separated by semi-colons (;) and/or a path to a json file\n\nInternal path variables:\n%ConfigPath% = Config directory\n%PluginPath% = Plugin directory");

            if (DisablePreloader.Value)
            {
                Logger.LogMessage("Preloader was disabled in the config - Skipping initialization!");
                return;
            }

            List<string> RawItemList = new List<string>();
            RawItemList = Utilities.ConvertFlatToList(ModList.Value);

            if (!DisableJsonSearch.Value)
            {
                List<string> searchPattern = new List<string>{ $"*{JsonPattern}*" };
                Utilities.InternalVars.Values.ToList().ForEach(path =>
                {
                    List<FileInfo> files = Utilities.FindFilesParallel(searchPattern, path).ToList();
                    if (files != null || files.Count >= 1)
                    {
                        files.ForEach(file =>
                        {
                            if (RawItemList == null) RawItemList = new List<string>();
                            RawItemList.Add(file.FullName);
                        });
                    }
                });
            }

            List<string> PrePluginToNeuter = Utilities.ResolvePluginList(RawItemList);
            if (PrePluginToNeuter != null) PluginToNeuter = PrePluginToNeuter.Distinct().ToList();

            if (PluginToNeuter == null || PluginToNeuter.Count < 1)
            {
                Logger.LogMessage("Preloader has zero mods configured to modify - Skipping initialization!");
                return;
            }

            Logger.LogMessage($"Initializing preloader event hooks to modify {PluginToNeuter.Count} plugins...");
            PluginToNeuter.ForEach(plugin => Logger.LogMessage($"Plugin to neuter: {plugin}"));
            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
            TimeSpan ts = DateTime.Now - start;
            Logger.LogMessage($"Finished initialization in: {Utilities.ToFriendlyTime(ts)}");
        }

        /// <summary>
        /// Hooked event when an assembly is being loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs e)
        {
            Harmony harmony = new Harmony(Id);
            Assembly loadedAssembly = e.LoadedAssembly;

            List<PluginData> bplugin = Utilities.GetPluginInfo(loadedAssembly);
            if (bplugin == null || bplugin.Count < 1) return;

            List<PluginData> actualPlugin = bplugin.Where(b => PluginToNeuter.Any(p => Regex.IsMatch(p, $"^{Regex.Escape(b.name.Trim())}$", RegexOptions.IgnoreCase))).ToList();
            if (actualPlugin == null || actualPlugin.Count < 1) return;

            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(Patcher).GetMethod("Neuter", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy));
            actualPlugin.ForEach(plugin =>
            {
                plugin.methods.ForEach(method =>
                {
                    string phrase = $"{plugin.name} v{plugin.version}";
                    Logger.LogMessage($"Attempting to remove '{method.Name}' from: {phrase}");
                    try
                    {
                        harmony.Patch((MethodBase)method, harmonyMethod, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
                        Logger.LogMessage($"Successfully neutered: {phrase}");
                    }
                    catch (Exception Ex)
                    {
                        Logger.LogError($"Failed to neuter: {phrase} - PLEASE REPORT THIS ON GITHUB!");
                        Logger.LogError($"Exception: {Ex.Message}");
                    }
                });
            });
        }

        /// <summary>
        /// Method to replace awake with to stop a plugin from loading
        /// </summary>
        /// <returns></returns>
        private static bool Neuter()
        {
            return false;
        }

        /// <summary>
        /// Entry point for preload patching
        /// </summary>
        /// <param name="_"></param>
        public static void Patch(AssemblyDefinition _)
        {
        }
    }
}
