using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using DiscoveryPins.Pins;

namespace DiscoveryPins.Patches;

[HarmonyPatch]
internal static class PortalPins
{
    private const string DefaultPortalName = "Portal";
    private static readonly HashSet<string> PortalPrefabNames = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="prefab"></param>
    internal static void TryAddAutoPinnerToPortal(GameObject prefab)
    {
        if (IsPortal(prefab) && !PortalPrefabNames.Contains(prefab.name))
        {
            PortalPrefabNames.Add(prefab.name);

            // Don't worry about picking a good name yet since tag may not be set
            AutoPinner.AddAutoPinner(prefab, DefaultPortalName, AutoPins.AutoPinCategory.Portal);
        }
    }

    /// <summary>
    ///     Checks if this has a TeleportWorld as currently portals 
    ///     are the only prefabs with that as a top level component.
    /// </summary>
    /// <param name="prefab"></param>
    /// <returns></returns>
    internal static bool IsPortal(GameObject prefab)
    {
        return prefab.GetComponent<TeleportWorld>();
    }


    /// <summary>
    ///     Update AutoPin name to use portal tag when portal is first loaded.
    ///     Also pin portals you built yourself when they load in.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Awake))]
    internal static void TeleportWorld_Awake_Postfix(TeleportWorld __instance)
    {
        if (__instance && __instance.TryGetComponent(out AutoPinner autoPinner))
        {
            autoPinner.UpdatePinName(GetPortalAutoPinName(__instance), markAsChanged: false);
        }
    }

    /// <summary>
    ///     Update AutoPin name whenever portal tag is changed.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.RPC_SetTag))]
    internal static void TeleportWorld_RPC_SetTag_Postfix(TeleportWorld __instance)
    {
        if (__instance && __instance.TryGetComponent(out AutoPinner autoPinner))
        {
            autoPinner.UpdatePinName(GetPortalAutoPinName(__instance));
        }
    }

    /// <summary>
    ///     Get portal tag text if set and return default name otherwise.
    /// </summary>
    /// <param name="teleportWorld"></param>
    /// <returns></returns>
    internal static string GetPortalAutoPinName(TeleportWorld teleportWorld)
    {
        string pinName = teleportWorld.GetText();
        return string.IsNullOrWhiteSpace(pinName) ? DefaultPortalName : pinName;
    }

    /// <summary>
    ///     This should pin portal when entering them (even if you don't teleport). 
    ///     Hopefully pins them when exiting as well since walking into a portal
    ///     during their teleport cooldown still triggers this method.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
    internal static void TeleportWorld_Teleport_Prefix(TeleportWorld __instance)
    {
        TriggerAutoPinPortal(__instance);
    }

    /// <summary>
    ///     Trigger auto pin when hovering to read portal text.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverText))]
    internal static void TeleportWorld_GetHoverText_Prefix(TeleportWorld __instance)
    {
        TriggerAutoPinPortal(__instance);
    }

    //[HarmonyPostfix]
    //[HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
    //private static void TriggerAutoPinDestination(Player __instance, Vector3 pos, bool __result) 
    //{
    //    // Check for AutoPinner at that location and trigger it
    //}

    /// <summary>
    ///     Trigger auto pin if possible.
    /// </summary>
    /// <param name="teleportWorld"></param>
    private static void TriggerAutoPinPortal(TeleportWorld teleportWorld)
    {
        if (teleportWorld && teleportWorld.TryGetComponent(out AutoPinner autoPinner))
        {
            autoPinner.AddAutoPin();
        }
    }
}
