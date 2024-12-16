using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using static Minimap;
using DiscoveryPins.Extensions;

namespace DiscoveryPins.Pins
{
    /// <summary>
    ///     Handle pin colors
    /// </summary>
    [HarmonyPatch]
    internal static class PinColors
    {

        /// <summary>
        ///     Update pin colors whenever updating pins in map
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePins))]
        private static void MinimapUpdatePins_PostFix()
        {
            UpdatePinsColor();
        }

        internal static Dictionary<PinType, Color> PinColorMap = new();

        private const string Red = "#d43d3d";
        private const string Cyan = "#35b5cc";
        private const string Orange = "#d6b340";
        private const string LightDusk = "#737373";
        private const string Brown = "#a86840";
        private const string Purple = "#9c39ed";
        private const string White = "#ffffff";

        internal static readonly Dictionary<PinType, string> DefaultPinColors = new()
        {
            { PinType.Death, Red},
            { PinType.Bed,  Cyan },
            { PinType.Icon0, Orange }, // Fireplace 
            { PinType.Icon1,  Cyan },  // House 
            { PinType.Icon4,  Brown },  // Cave
            { PinType.Icon2, LightDusk },  // Hammer
            { PinType.Icon3, Red },  // Ball
            { PinType.Shout,  White },
            { PinType.Player,  Red },
            { PinType.Boss, Purple },
            { PinType.RandomEvent, White },
            { PinType.Ping, White },
            { PinType.EventArea, White },

        };

        /// <summary>
        ///     Update lib map of PinType to color
        /// </summary>
        public static void UpdatePinColorMap()
        {
            if (!DiscoveryPins.Instance.EnableColors.Value)
            {
                return;
            }
       
            PinColorMap.Clear();
            foreach (KeyValuePair<PinType, ConfigEntry<string>> pair in DiscoveryPins.Instance.PinColorConfigs){
                PinColorMap[pair.Key] = pair.Value.Value.ToColor();
            }
        }

        /// <summary>
        ///     Update pin colors on the minimap.
        /// </summary>
        public static void UpdatePinsColor()
        {
            if (!DiscoveryPins.Instance.EnableColors.Value)
            {
                return;
            }

            foreach (var pin in Minimap.instance.m_pins)
            {
                if (pin.m_iconElement == null)
                {
                    continue;
                }
                if (PinColorMap.TryGetValue(pin.m_type, out var color))
                {
                    pin.m_iconElement.color *= color;
                }
            }
        }

    }
}
