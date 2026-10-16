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
        { "StoneTower1", "Tower" },  // fuling
        { "StoneTower3", "Tower" },  // fuling
        { "Mistlands_GuardTower1_new", "Tower" },
        { "Mistlands_GuardTower1_ruined_new", "Tower" },
        { "Mistlands_GuardTower1_ruined_new2", "Tower" },
        { "Mistlands_GuardTower2_new", "Tower" },
        { "Mistlands_GuardTower3_new", "Tower" },
        { "Mistlands_GuardTower3_ruined_new", "Tower" },
        { "Mistlands_Excavation1", "Digsite" },
        { "Mistlands_Excavation2", "Digsite" },
        { "Mistlands_Excavation3", "Digsite" },
        { "Mistlands_Harbour1", "Harbour" },
        { "Mistlands_Lighthouse1_new", "Lighthouse" },
        { "FireHole", "Fire Spout" },
        { "CharredTowerRuins1", "Ruins" },
        { "CharredTowerRuins1_dvergr", "Ruins" },
        { "StoneTowerRuins03", "Ruins" },
        { "StoneTowerRuins04", "Ruins" },
        { "StoneTowerRuins05", "Ruins" },
        { "StoneTowerRuins05_leet", "Ruins" },
        { "StoneTowerRuins07", "Ruins" },
        { "StoneTowerRuins07_sunk", "Ruins" },
        { "StoneTowerRuins08", "Ruins" },
        { "StoneTowerRuins08_sunk", "Ruins" },
        { "StoneTowerRuins09", "Ruins" },
        { "StoneTowerRuins09_sunk", "Ruins" },
        { "StoneTowerRuins10", "Ruins" },
        { "StoneTowerRuins10_sunk", "Ruins" },
        { "NorthMemorialPlace", "Memorial" }
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
    private static void AddLocationAutoPinnerToSpawnedLocationProxy(LocationProxy __instance)
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
