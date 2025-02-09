using HarmonyLib;
using DiscoveryPins.Extensions;
using DiscoveryPins.Pins;

namespace DiscoveryPins.Patches;

[HarmonyPatch]
internal static class DungeonPins
{
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
            AutoPinner.AddAutoPinner(entrance.gameObject, name, AutoPins.AutoPinCategory.Dungeon);
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
