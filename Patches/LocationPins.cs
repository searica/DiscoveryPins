using HarmonyLib;
using System.Collections.Generic;
using DiscoveryPins.Extensions;
using DiscoveryPins.Pins;

namespace DiscoveryPins.Patches
{
    [HarmonyPatch]
    internal static class LocationPins
    {
        internal static List<Location> Locations = new();

        /// <summary>
        ///     Track locations with an interior
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Location), nameof(Location.Awake))]
        private static void Location_AwakePostfix(Location __instance)
        {

            if (__instance.TryGetInteriorEntrance(out Teleport entrance, out string name))
            {
                Log.LogInfo($"{name} has an interior entrance.");
                AutoPinner.AddAutoPinner(entrance.gameObject, name, AutoPins.AutoPinCategory.Dungeon);
            }

            else if (__instance.TryGetOverworldDungeon(out DungeonGenerator generator, out name))
            {
                Log.LogInfo($"{name} is an overworld dungeon.");
                AutoPinner.AddAutoPinner(generator.gameObject, name, AutoPins.AutoPinCategory.Location);
            }
            else
            {
                return;
            }
        }

        /// <summary>
        ///     Trigger AutoPinner when interacting with a teleport component on the same gameObject
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Teleport), nameof(Teleport.Interact))]
        private static void TeleportInteract_Prefix(Teleport __instance)
        {
            if (!__instance.TryGetComponent(out AutoPinner autoPinner))
            {
                return;
            }
            autoPinner.AddAutoPin();
        }
    }
}
