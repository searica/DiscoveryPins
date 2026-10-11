using HarmonyLib;
using System.Collections.Generic;
using DiscoveryPins.Extensions;
using DiscoveryPins.Pins;


namespace DiscoveryPins.Patches;

[HarmonyPatch]
internal static class LocationPins
{
    private static readonly HashSet<string> LocationNames = [];

    // various locations that are often useful to pin
    private static readonly Dictionary<string, string> OverworldLocationNamesMap = new Dictionary<string, string> { 
        { "CharredFortress", "$charredfortress" },  // include charred fortress because it feels like they should be in this category
        { "StoneTower1", "Fuling Tower" },
        { "StoneTower3", "Fuling Tower" },
        { "Mistlands_GuardTower1_new", "Dvergr Tower" },
        { "Mistlands_GuardTower1_ruined_new", "Dvergr Tower" },
        { "Mistlands_GuardTower1_ruined_new2", "Dvergr Tower" },
        { "Mistlands_GuardTower2_new", "Dvergr Tower" },
        { "Mistlands_GuardTower3_new", "Dvergr Tower" },
        { "Mistlands_GuardTower3_ruined_new", "Dvergr Tower" },
        { "Mistlands_Excavation1", "Dvergr Dig" },
        { "Mistlands_Excavation2", "Dvergr Dig" },
        { "Mistlands_Excavation3", "Dvergr Dig" },
        { "Mistlands_Harbour1", "Dvergr Dock" },
        { "Mistlands_Lighthouse1_new", "Lighthouse" },
        { "FireHole", "Surtlings" }

    };

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
