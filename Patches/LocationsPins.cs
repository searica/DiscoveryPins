using HarmonyLib;
using System.Collections.Generic;
using DiscoveryPins.Extensions;
using DiscoveryPins.Pins;


namespace DiscoveryPins.Patches;

[HarmonyPatch]
internal static class LocationPins
{
    private static readonly HashSet<string> LocationNames = [];
    private static readonly Dictionary<string, string> OverworldLocationNamesMap = [];

    /// <summary>
    ///     Track locations that spawn an exterior dungeon
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Location), nameof(Location.Awake))]
    private static void Location_AwakePostfix(Location __instance)
    {
        if (!__instance)
        {
            return;
        }

        string locationName = Utils.GetPrefabName(__instance.gameObject);
        if (OverworldLocationNamesMap.ContainsKey(locationName))
        {
            return;
        }

        if (!__instance.TryGetOverworldDungeon(out DungeonGenerator _, out string name))
        {
            return;
        }
        OverworldLocationNamesMap[locationName] = name;
    }


    /// <summary>
    ///     Add auto-pinner to LocationProxy's Location instance if it's an exterior dungeon.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LocationProxy), nameof(LocationProxy.SpawnLocation))]
    private static void AddAutoPinnerToSpawnedLocationProxy(LocationProxy __instance)
    {
        if (!__instance || !__instance.m_instance)
        {
            return;
        }

        string locationName = Utils.GetPrefabName(__instance.m_instance);
        if (OverworldLocationNamesMap.ContainsKey(locationName))
        {
            AutoPinner.AddAutoPinner(__instance.gameObject, OverworldLocationNamesMap[locationName], AutoPins.AutoPinCategory.Location);
        }
    }
}
