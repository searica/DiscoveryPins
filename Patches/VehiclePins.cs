using System;
using System.Collections.Generic;
using DiscoveryPins.Pins;
using HarmonyLib;
using UnityEngine;
using DiscoveryPins.Extensions;
using System.Linq;


namespace DiscoveryPins.Patches
{
    [HarmonyPatch]
    internal static class VehiclePins
    {
        private static readonly List<string> VehicleNames = [
            "Cart",
            "Raft",
            "Karve",
            "VikingShip_Ashlands", // Drakkar
            "VikingShip", //Longship
            "Trailership", // Trailer Ship
        ];

        private static readonly Dictionary<string, string> VehiclesDict = new Dictionary<string, string> {
            {"Cart", "Cart"},
            {"Raft", "Raft"},
            {"Karve", "Karve" },
            {"VikingShip_Ashlands", "Drakkar" },
            {"VikingShip", "Longship" },
            {"Trailership", "Trailer Ship" }
        };


        private static readonly HashSet<string> VehiclePrefabNames = [
            ];

        /// <summary>
        ///     Adds AutoPinner to prefab if it is actually a vehicle and not already modified.
        /// </summary>
        /// <param name="prefab"></param>
        internal static void TryAddAutoPinnerToVehicle(GameObject prefab)
        {
            if (IsVehiclePrefab(prefab, out string VehicleName) && !VehiclePrefabNames.Contains(prefab.name))
            {
                VehiclePrefabNames.Add(prefab.name);
                AutoPinner.AddAutoPinner(prefab, VehicleName, AutoPins.AutoPinCategory.Vehicle);
            }
        }


        /// <summary>
        ///     Check if it is a vehicle prefab and get vehicle name
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="VehicleName"></param>
        /// <returns></returns>
        private static bool IsVehiclePrefab(GameObject gameObject, out string VehicleName)
        {
            bool isVehiclePrefab = false;
            VehicleName = null;

            if (VehiclesDict.Keys.Contains(gameObject.name, StringComparer.OrdinalIgnoreCase))
            {
                VehiclesDict.TryGetValue(gameObject.name, out VehicleName);
                isVehiclePrefab = true;
            }

            return isVehiclePrefab;
        }
    }
}
