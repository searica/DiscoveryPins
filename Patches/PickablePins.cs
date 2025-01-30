using System;
using System.Collections.Generic;
using DiscoveryPins.Pins;
using HarmonyLib;
using UnityEngine;
using DiscoveryPins.Extensions;


namespace DiscoveryPins.Patches
{
    [HarmonyPatch]
    internal static class PickablePins
    {
        private static readonly List<string> PickableNames = [
            "Raspberry",
            "Blueberry",
            "Cloudberry",
            "Carrot",
            "Onion",
            "Turnip",
            "Thistle",
            "Mushroom",
            "Barley",
            "Flax",
            "Dandelion",
            "DragonEgg",
            "Guck", // not pickable per se, but might be pin-worthy
            //"Flint", // pickable but probably annoying as pin
            //"Branch", // pickable but probably annoying as pin
            //"Stone", // pickable but probably annoying as pin
            //"Pickable" // Fallback probably not necessary
        ];

        private static readonly Dictionary<string, string> PickablesDict = new Dictionary<string, string>
        {
            { "BlueberryBush", "Blueberry" },
            { "CloudberryBush", "Cloudberry" },
            { "RaspberryBush", "Raspberry" },
            { "Pickable_SeedCarrot", "Carrot seeds" },
            { "Pickable_Carrot", "Carrot" },
            { "Pickable_SeedOnion", "Onion seeds" },
            { "Pickable_Onion", "Onion" },
            { "Pickable_SeedTurnip", "Turnip seeds" },
            { "Pickable_Turnip", "Turnip" },
            { "Pickable_Thistle", "Thistle" },
            { "Pickable_Mushroom_yellow", "Yellow mushroom" },
            { "Pickable_Mushroom_blue", "Blue mushroom" },
            { "Pickable_Mushroom", "Mushroom" },
            { "Pickable_Barley", "Barley" },
            { "Pickable_Barley_Wild", "Barley" },
            { "Pickable_Flax", "Flax" },
            { "Pickable_Flax_Wild", "Flax" },
            { "Pickable_Dandelion", "Dandelion" },
            { "Pickable_DragonEgg", "Dragon egg" },
            { "GuckSack_small", "Guck" },
            { "GuckSack", "Guck" },
            //{ "Pickable_Flint", "Flint" },
            //{ "Pickable_Branch", "Branch" },
            //{ "Pickable_Stone", "Stone" }
        };

        private static readonly HashSet<string> PickablePrefabNames = [
            ];

        /// <summary>
        ///     Adds AutoPinner to prefab if it is actually Pickable and not already modified.
        /// </summary>
        /// <param name="prefab"></param>
        internal static void TryAddAutoPinnerToPickable(GameObject prefab)
        {
            if (IsPickablePrefab(prefab, out string PickableName) && !PickablePrefabNames.Contains(prefab.name))
            {
                PickablePrefabNames.Add(prefab.name);
                AutoPinner.AddAutoPinner(prefab, PickableName, AutoPins.AutoPinCategory.Pickable);
            }
        }


        /// <summary>
        ///     Check if it is an Pickable prefab and get Pickable name
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="PickableName"></param>
        /// <returns></returns>
        private static bool IsPickablePrefab(GameObject gameObject, out string PickableName)
        {
            bool isPickablePrefab = false;
            PickableName = null;
            foreach (var name in PickablesDict.Keys)
            {
                if (gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    PickablesDict.TryGetValue(name, out PickableName);
                    isPickablePrefab = true;
                    break;
                }
            }

            return isPickablePrefab;
        }
    }
}
