using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using Jotunn.Managers;
using DiscoveryPins.Pins;
using Configs;
using Logging;

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
        public const string PluginVersion = "0.2.4";

        internal static DiscoveryPins Instance;
        internal static ConfigFile ConfigFile;

        // Global settings
        internal const string GlobalSection = "Global";
        internal ConfigEntry<int> PinSpacing;


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

            PinSpacing = Config.BindConfigInOrder(
                GlobalSection,
                "Pin Spacing",
                5,
                "The minimum allowable distance between auto-pins."
                +" If a new auto-pin would be closer to an existing pin"
                +" than the value of Pin Spacing, then no pin will be plaecd.",
                new AcceptableValueRange<int>(0, 25),
                synced: false
            );

            // Auto Pin shortcut configs
            AutoPinShortcutConfigs = new AutoPinShortcutConfig()
            {
                Enabled = Config.BindConfigInOrder(
                    AutoPinShortcutSection,
                    "Enabled",
                    true,
                    "Whether to allow using the auto pin shortcut.",
                    synced: true
                ),
                Range = Config.BindConfigInOrder(
                    AutoPinShortcutSection,
                    "Range",
                    20f,
                    "Maximum distance that auto pins can be generated.",
                    new AcceptableValueRange<float>(AutoPinner.CloseEnoughXZ, 50f),
                    synced: true
                ),
                Shortcut = Config.BindConfigInOrder(
                    AutoPinShortcutSection,
                    "Shortcut",
                    new KeyboardShortcut(KeyCode.Mouse1, KeyCode.LeftShift),
                    "Shortcut to trigger auto pin.",
                    synced: false
                )
            };


            // Tombstone configs
            DeathPinConfigs = new DeathPinConfig()
            {
                PinWhenInvIsEmpty = Config.BindConfigInOrder(
                    DeathPinSection,
                    "Generate with empty inventory", 
                    true, 
                    "Death pin will/won't be generated if your inventory was empty.",
                    synced: false
                ),
                AutoRemoveEnabled = Config.BindConfigInOrder(
                    DeathPinSection,
                    "Remove on retrieval",
                    true,
                    "Death pin be removed automatically when tombstone is retrieved.",
                    synced: false
                )
            };

            // Auto pin configs
            
            foreach (KeyValuePair<AutoPins.AutoPinCategory, Minimap.PinType> pair in AutoPins.DefaultPinTypes)
            {
                var sectionName = $"Auto Pin: {pair.Key}";
                AutoPinConfigs.Add(
                    pair.Key,
                    new AutoPinConfig()
                    {
                        Enabled = Config.BindConfigInOrder(
                            sectionName,
                            "Enabled",
                            true,
                            "Whether auto pinning is enabled.",
                            synced: false
                        ),
                        Icon = Config.BindConfigInOrder(
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
            EnableColors = Config.BindConfigInOrder(
                ColorSection, 
                "Enabled",
                true, 
                "Whether to enable custom pin colors.",
                synced: false
            );
            foreach (KeyValuePair<Minimap.PinType, string> pair in PinColors.DefaultPinColors) 
            {   
                PinColorConfigs.Add(
                    pair.Key,
                    Config.BindConfigInOrder(
                        ColorSection, 
                        PinNames.PinTypeToName(pair.Key), 
                        pair.Value, 
                        "Color to use for pins of this type.",
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
}
