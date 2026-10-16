using BepInEx;
using DiscoveryPins.Extensions;
using DiscoveryPins.Pins;
using HarmonyLib;
using Splatform;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static TeleportWorld;

namespace DiscoveryPins.Patches;

[HarmonyPatch]
internal static class PortalPins
{
    private const string DefaultPortalName = "Portal";
    private static readonly HashSet<string> PortalPrefabNames = new();
    private static float PrevNumPortalsUsed = 0;
    private static float CurrentNumPortalsUsed = 0;

    private static AutoPinner DestinationPortalPinner;
    private static GameObject gameObject;
    private static string DestinationPortalTag;
    private static string TagAuthor;
    private static ZDOID DestinationZDOID;



    internal static void Initialize()
    {
        gameObject = new GameObject("DestinationPortalPinner");
        DestinationPortalPinner = gameObject.AddComponent<AutoPinner>();
        DestinationPortalPinner.PinName = DefaultPortalName;
        DestinationPortalPinner.PinCategory = AutoPins.AutoPinCategory.Portal;
        GameObject.DontDestroyOnLoad(gameObject);           
    }


    /// <summary>
    ///     Add Autopinner component to all ores in the game via editing their prefabs.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    private static void AddAutoPinnerToPortalPrefabs()
    {
        // If loading into game world and prefabs have not been added
        if (SceneManager.GetActiveScene().name != "main")
        {
            return;
        }

        // Get prefabs that are mineable ores and modify them
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            if (!prefab.IsTopLevelPrefab())
            {
                continue;
            }
            TryAddAutoPinnerToPortal(prefab);
        }
    }

    /// <summary>
    ///     Add auto-pinner to prefab if it is a portal.
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
        if (TriggerAutoPinPortal(__instance))
        {
            DestinationZDOID =__instance.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
        }
        else
        {
            DestinationZDOID = ZDOID.None;
        }            
    }

    /// <summary>
    ///     Trigger pin at the destination portal when a player goes through one and their stats are incremented. 
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Game), nameof(Game.IncrementPlayerStat))]
    internal static void PinDestinationPatch(Game __instance, PlayerStatType stat)
    {
        if (stat != PlayerStatType.PortalsUsed)
        {
            return;
        }
        TriggerAutoPinDestination(DestinationZDOID);
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

    private static void TriggerAutoPinDestination(ZDOID zdoID)
    {
        if (zdoID == null || zdoID == ZDOID.None) { return; }

        ZDO zdo = ZDOMan.instance.GetZDO(zdoID);
        if (zdo != null)
        {
            // get destination portal tag with fallback to default portal name if no tag exists
            TagAuthor = zdo.GetString(ZDOVars.s_tagauthor);
            PlatformUserID userId = (string.IsNullOrEmpty(TagAuthor) ? PlatformUserID.None : new PlatformUserID(TagAuthor));
            DestinationPortalTag = CensorShittyWords.FilterUGC(zdo.GetString(ZDOVars.s_tag), UGCType.Text, userId, 0L);
            DestinationPortalPinner.PinName = string.IsNullOrEmpty(DestinationPortalTag) ? DefaultPortalName : DestinationPortalTag;

            // update position of auto pinner to destination portal
            gameObject.transform.localPosition = zdo.GetPosition();

            // trigger auto pin at destination
            DestinationPortalPinner.AddAutoPin();
        }
    }

    /// <summary>
    ///     Trigger auto pin if possible.
    /// </summary>
    /// <param name="teleportWorld"></param>
    private static bool TriggerAutoPinPortal(TeleportWorld teleportWorld)
    {
        if (teleportWorld && teleportWorld.TryGetComponent(out AutoPinner autoPinner))
        {
            autoPinner.AddAutoPin();
            return true;
        }
        return false;
    }
}
