using System;
using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Utils;
using Jotunn.Managers;
using DiscoveryPins.Helpers;
using System.Collections.Generic;
using DiscoveryPins.Pins;
using DiscoveryPins.Configs;

namespace DiscoveryPins
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
    [NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
    [SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
    internal sealed class DiscoveryPins : BaseUnityPlugin
    {
        public const string PluginName = "DiscoveryPins";
        internal const string Author = "Searica";
        public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
        public const string PluginVersion = "0.2.2";

        internal static DiscoveryPins Instance;
        internal static ConfigFile ConfigFile;

        // Auto Pin hot key set up
        internal const string AutoPinShortcutSection = "Auto Pin Shortcut";
        internal class AutoPinShortcutConfig
        {
            internal ConfigEntry<bool> Enabled;
            internal ConfigEntry<float> Range;
            internal ConfigEntry<KeyboardShortcut> Shortcut;
        }
        internal AutoPinShortcutConfig AutoPinShortcutConfigs;

        // Death pin configs
        internal const string DeathPinSection = "Auto Pin: Tombstone";
        internal class DeathPinConfig
        {
            internal ConfigEntry<bool> PinWhenInvIsEmpty;
            internal ConfigEntry<bool> AutoRemoveEnabled;
        }
        internal DeathPinConfig DeathPinConfigs;


        // AutoPin Configs
        internal class AutoPinConfig
        {
            internal ConfigEntry<bool> Enabled;
            internal ConfigEntry<string> Icon;
        }
        internal readonly Dictionary<AutoPins.AutoPinCategory, AutoPinConfig> AutoPinConfigs = new();


        // Pin Color Configs
        internal const string ColorSection = "Pin Colors";
        internal ConfigEntry<bool> EnableColors;
        internal readonly Dictionary<Minimap.PinType, ConfigEntry<string>> PinColorConfigs = new();
        internal static bool ColorConfigsChanged = false;

        /// <summary>
        ///     Event hook to set whether a config entry
        ///     for a piece setting has been changed.
        /// </summary>
        internal static void OnColorConfigChanged(object obj, EventArgs args)
        {
            if (!ColorConfigsChanged) ColorConfigsChanged = true;
        }

        public void Awake()
        {
            Instance = this;
            ConfigFile = Config;
            Log.Init(Logger);

            Config.Init(PluginGUID, false);
            SetUpConfigEntries();
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
            Game.isModded = true;

            UpdatePlugin(saveConfig: true, initialUpdate: true);

            // Re-initialization after reloading config and don't save since file was just reloaded
            Config.SetupWatcher();
            ConfigFileManager.OnConfigFileReloaded += () =>
            {
                UpdatePlugin(saveConfig: false);
            };

            // Re-initialize after changing config data in-game and trigger a save to disk.
            SynchronizationManager.OnConfigurationWindowClosed += () =>
            {
                UpdatePlugin(saveConfig: true);
            };

            // Re-initialize after getting updated config data and trigger a save to disk.
            SynchronizationManager.OnConfigurationSynchronized += (obj, attr) =>
            {
                UpdatePlugin(saveConfig: true);
            };
        }

        internal void SetUpConfigEntries()
        {
            // Auto Pin shortcut configs
            int sectionCounter = 1;
            AutoPinShortcutConfigs = new AutoPinShortcutConfig()
            {
                Enabled = Config.BindConfig(
                    AutoPinShortcutSection,
                    "Enabled",
                    true,
                    "Whether to allow using the auto pin shortcut.",
                    sectionOrder: sectionCounter,
                    synced: true
                ),
                Range = Config.BindConfig(
                    AutoPinShortcutSection,
                    "Range",
                    20f,
                    "Maximum distance that auto pins can be generated.",
                    new AcceptableValueRange<float>(AutoPinner.CloseEnoughXZ, 50f),
                    sectionOrder: sectionCounter,
                    synced: true
                ),
                Shortcut = Config.BindConfig(
                    AutoPinShortcutSection,
                    "Shortcut",
                    new KeyboardShortcut(KeyCode.Mouse1, KeyCode.LeftShift),
                    "Shortcut to trigger auto pin.",
                    sectionOrder: sectionCounter,
                    synced: false
                )
            };


            // Tombstone configs
            sectionCounter++;
            DeathPinConfigs = new DeathPinConfig()
            {
                PinWhenInvIsEmpty = Config.BindConfig(
                    DeathPinSection,
                    "Generate with empty inventory", 
                    true, 
                    "Death pin will/won't be generated if your inventory was empty.",
                    sectionOrder: sectionCounter,
                    synced: false
                ),
                AutoRemoveEnabled = Config.BindConfig(
                    DeathPinSection,
                    "Remove on retrieval",
                    true,
                    "Death pin be removed automatically when tombstone is retrieved.",
                    sectionOrder: sectionCounter,
                    synced: false
                )
            };

            // Auto pin configs
            
            foreach (KeyValuePair<AutoPins.AutoPinCategory, Minimap.PinType> pair in AutoPins.DefaultPinTypes)
            {
                sectionCounter++;
                var sectionName = $"{sectionCounter} - Auto Pin: {pair.Key.ToString()}";
     
                AutoPinConfigs.Add(
                    pair.Key,
                    new AutoPinConfig()
                    {
                        Enabled = Config.BindConfig(
                            sectionName,
                            "Enabled",
                            true,
                            "Whether auto pinning is enabled.",
                            synced: false
                        ),
                        Icon = Config.BindConfig(
                            sectionName,
                            "Icon",
                            PinNames.PinTypeToName(pair.Value),
                            "Which icon to create the pin with.",
                            PlaceablePins.AllowedPlaceablePinNames,
                            synced: false
                        )
                    }
                );
            }

            // Color configs
            sectionCounter++;
            EnableColors = Config.BindConfig(
                ColorSection, 
                "Enabled",
                true, 
                "Whether to enable custom pin colors.",
                order: 10,
                sectionOrder: sectionCounter,
                synced: false
            );
            foreach (KeyValuePair<Minimap.PinType, string> pair in PinColors.DefaultPinColors) 
            {   
                PinColorConfigs.Add(
                    pair.Key,
                    Config.BindConfig(
                        ColorSection, 
                        PinNames.PinTypeToName(pair.Key), 
                        pair.Value, 
                        "Color to use for pins of this type.",
                        sectionOrder: sectionCounter,
                        synced: false
                    )
                );
                PinColorConfigs[pair.Key].SettingChanged += OnColorConfigChanged;
            }
           
        }

        private void UpdatePlugin(bool saveConfig = true, bool initialUpdate = false)
        {
            if (ColorConfigsChanged || initialUpdate)
            {
                PinColors.UpdatePinColorMap();
                ColorConfigsChanged = false;
            }
            
            if (saveConfig)
            {
                Config.Save();
            }
        }

        public void OnDestroy()
        {
            Config.Save();
        }

    }

    /// <summary>
    ///     Log level to control output to BepInEx log
    /// </summary>
    internal enum LogLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
    }

    /// <summary>
    ///     Helper class for properly logging from static contexts.
    /// </summary>
    internal static class Log
    {
        internal static ConfigEntry<LogLevel> Verbosity { get; set; }
        internal static LogLevel VerbosityLevel => Verbosity.Value;
        internal static bool IsVerbosityLow => Verbosity.Value >= LogLevel.Low;
        internal static bool IsVerbosityMedium => Verbosity.Value >= LogLevel.Medium;
        internal static bool IsVerbosityHigh => Verbosity.Value >= LogLevel.High;

        private static ManualLogSource logSource;

        internal static void Init(ManualLogSource logSource)
        {
            Log.logSource = logSource;
        }

        internal static void LogDebug(object data) => logSource.LogDebug(data);

        internal static void LogError(object data) => logSource.LogError(data);

        internal static void LogFatal(object data) => logSource.LogFatal(data);

        internal static void LogMessage(object data) => logSource.LogMessage(data);

        internal static void LogWarning(object data) => logSource.LogWarning(data);

        internal static void LogInfo(object data, LogLevel level = LogLevel.Low)
        {
            if (Verbosity is null || VerbosityLevel >= level)
            {
                logSource.LogInfo(data);
            }
        }

        internal static void LogGameObject(GameObject prefab, bool includeChildren = false)
        {
            LogInfo("***** " + prefab.name + " *****");
            foreach (Component compo in prefab.GetComponents<Component>())
            {
                LogComponent(compo);
            }

            if (!includeChildren) { return; }

            LogInfo("***** " + prefab.name + " (children) *****");
            foreach (Transform child in prefab.transform)
            {
                LogInfo($" - {child.gameObject.name}");
                foreach (Component compo in child.gameObject.GetComponents<Component>())
                {
                    LogComponent(compo);
                }
            }
        }

        internal static void LogComponent(Component compo)
        {
            LogInfo($"--- {compo.GetType().Name}: {compo.name} ---");

            PropertyInfo[] properties = compo.GetType().GetProperties(ReflectionUtils.AllBindings);
            foreach (var property in properties)
            {
                LogInfo($" - {property.Name} = {property.GetValue(compo)}");
            }

            FieldInfo[] fields = compo.GetType().GetFields(ReflectionUtils.AllBindings);
            foreach (var field in fields)
            {
                LogInfo($" - {field.Name} = {field.GetValue(compo)}");
            }
        }
    }
}
