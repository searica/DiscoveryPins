using DiscoveryPins.Extensions;
using DiscoveryPins.Pins;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Minimap;

namespace DiscoveryPins.Patches;

[HarmonyPatch]
internal static class RuneStonePins
{

    /// <summary>
    ///     Track locations with a runestone
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Location), nameof(Location.Awake))]
    private static void Location_AwakePostfix(Location __instance)
    {
        if (__instance.TryGetRuneStone(out RuneStone runeStone))
        {
            AutoPinner.AddAutoPinner(runeStone.gameObject, runeStone.m_name, AutoPins.AutoPinCategory.RuneStone);
        }
    }

    /// <summary>
    ///     Add auto-pinner to LocationProxy's Location instance if it is a dungeon (needed for TheHole01)
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LocationProxy), nameof(LocationProxy.SpawnLocation))]
    private static void AddDungeonAutoPinnerToSpawnedLocationProxy(LocationProxy __instance)
    {
        if (!__instance || !__instance.m_instance || !__instance.TryGetComponent(out Location location) || !location)
        {
            return;
        }

        if (location.TryGetRuneStone(out RuneStone runeStone))
        {
            AutoPinner.AddAutoPinner(runeStone.gameObject, runeStone.m_name, AutoPins.AutoPinCategory.RuneStone);
        }
    }

    /// <summary>
    ///     Trigger AutoPinner when interacting with a rune stone on the same gameObject
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(RuneStone), nameof(RuneStone.Interact))]
    private static void ReadRuneStone(RuneStone __instance, Humanoid character)
    {
        if (!__instance.TryGetComponent(out AutoPinner autoPinner))
        {
            return;
        }
        autoPinner.AddAutoPin();
    }

}
