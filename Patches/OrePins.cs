using DiscoveryPins.Extensions;
using DiscoveryPins.Pins;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;


namespace DiscoveryPins.Patches;

[HarmonyPatch]
internal static class OrePins
{
    private static readonly HashSet<string> OreDropNames = [
        "BlackMarble",
        "Softtissue",
        "TinOre",
        "CopperOre",
        "SilverOre",
        "Obsidian",
        "FlametalOreNew",
    ];
    private static readonly List<string> OreNames = [
        "Tin",
        "Copper",
        "Silver",
        "Obsidian",
        "Giant",
        "LeviathanLava",
        "Meteorite",
    ];

    private static readonly HashSet<string> OrePrefabNames = [];

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MineRock), nameof(MineRock.Start))]
    private static void AddOreAutoPinnerOnStartMineRock(MineRock __instance)
    {
        if (DropsOre(__instance) && TryGetOreName(__instance, out string OreName))
        {
            AutoPinner.AddAutoPinner(__instance.gameObject, OreName, AutoPins.AutoPinCategory.Ore);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Awake))]
    private static void AddOreAutoPinnerOnAwakeMineRock5(MineRock5 __instance)
    {
        if (DropsOre(__instance) && TryGetOreName(__instance, out string OreName))
        {
            AutoPinner.AddAutoPinner(__instance.gameObject, OreName, AutoPins.AutoPinCategory.Ore);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Awake))]
    private static void AddOreAutoPinnerOnAwakeDestructible(Destructible __instance)
    {
        if (DropsOre(__instance) && TryGetOreName(__instance, out string OreName))
        {
            AutoPinner.AddAutoPinner(__instance.gameObject, OreName, AutoPins.AutoPinCategory.Ore);
        }
    }


    /// <summary>
    ///     Check if m_spawnWhenDestroyed is an ore drop. Then check
    ///     of m_spawnWhenDestroyed is a MineRock5 or MineRock and if
    ///     it drops ore.
    /// </summary>
    /// <param name="mineRock5"></param>
    /// <returns></returns>
    private static bool DropsOre(Destructible destructible)
    {
        if (!destructible || !destructible.m_spawnWhenDestroyed)
        {
            return false;
        }

        if (IsOreDrop(destructible.m_spawnWhenDestroyed))
        {
            return true;
        }

        if (destructible.m_spawnWhenDestroyed.TryGetComponent(out MineRock5 mineRock5))
        {
            return DropsOre(mineRock5);
        }

        if (destructible.m_spawnWhenDestroyed.TryGetComponent(out MineRock mineRock))
        {
            return DropsOre(mineRock);
        }

        return false;
    }


    /// <summary>
    ///     Scan drop table for any ore drop prefabs.
    /// </summary>
    /// <param name="mineRock5"></param>
    /// <returns></returns>
    private static bool DropsOre(MineRock5 mineRock5)
    {
        if (!mineRock5 || mineRock5.m_dropItems == null)
        {
            return false;
        }
        return DropTableContainsOreDrop(mineRock5.m_dropItems);
    }


    /// <summary>
    ///     Scan drop table for any ore drop prefabs.
    /// </summary>
    /// <param name="mineRock"></param>
    /// <returns></returns>
    private static bool DropsOre(MineRock mineRock)
    {
        if (!mineRock || mineRock.m_dropItems == null)
        {
            return false;
        }
        return DropTableContainsOreDrop(mineRock.m_dropItems);
    }


    /// <summary>
    ///     Check if any drops are in OreDropNames
    /// </summary>
    /// <param name="dropTable"></param>
    /// <returns></returns>
    private static bool DropTableContainsOreDrop(DropTable dropTable)
    {
        if (dropTable.m_drops == null || dropTable.m_drops.Count < 1)
        {
            return false;
        }

        foreach (DropTable.DropData dropData in dropTable.m_drops)
        {
            if (IsOreDrop(dropData.m_item))
            {
                return true;
            }
        }
        return false;
    }


    /// <summary>
    ///     Check if gameObject prefab name is in OreDropNames
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    private static bool IsOreDrop(GameObject gameObject)
    {
        string prefabName = gameObject.GetPrefabName();
        return OreDropNames.Contains(prefabName);
    }


    /// <summary>
    ///     Try getting the name of the Ore that is mined from this prefab
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="OreName"></param>
    /// <returns></returns>
    internal static bool TryGetOreName(Component oreComponent, out string OreName)
    {

        if (oreComponent is Destructible)
        {
            if (oreComponent.TryGetComponent(out HoverText hoverText))
            {
                OreName = hoverText.m_text;
                return true;
            }
        }
        else if (oreComponent is MineRock5 mineRock5)
        {
            OreName = mineRock5.m_name;
            return true;
        }
        else if (oreComponent is MineRock mineRock)
        {
            OreName = mineRock.m_name;
            return true;
        }

        OreName = null;
        return false;
    }


    /// <summary>
    ///     Trigger autopin when damaging the mineable rock.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.RPC_Damage))]
    private static void PinMineRock5OnDamage(MineRock5 __instance)
    {
        if (__instance.TryGetComponent(out AutoPinner autoPinner))
        {
            autoPinner.AddAutoPin();
        }
    }


    /// <summary>
    ///     Remove autopin when mineable rock destroyed.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.AllDestroyed))]
    private static void RemoveMineRock5PinOnDestroy(MineRock5 __instance, bool __result)
    {
        if (__result && __instance.TryGetComponent(out AutoPinner autoPinner))
        {
            autoPinner.RemoveAutoPin();
        }
    }


    /// <summary>
    ///     Trigger autopin when damaging the mineable rock.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MineRock), nameof(MineRock.RPC_Hit))]
    private static void PinMineRockOnHit(MineRock __instance)
    {
        if (__instance.TryGetComponent(out AutoPinner autoPinner))
        {
            autoPinner.AddAutoPin();
        }
    }


    /// <summary>
    ///     Remove autopin when mineable rock destroyed.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(MineRock), nameof(MineRock.AllDestroyed))]
    private static void RemoveMineRockPinOnDestroy(MineRock __instance, bool __result)
    {
        if (__result && __instance.TryGetComponent(out AutoPinner autoPinner))
        {
            autoPinner.RemoveAutoPin();
        }
    }


    /// <summary>
    ///     Trigger autopin when mineable rock is damaged.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Destructible), nameof(Destructible.RPC_Damage))]
    private static void PinDestructibleOnDamage(Destructible __instance)
    {
        if (__instance.TryGetComponent(out AutoPinner autoPinner))
        {
            autoPinner.AddAutoPin();
        }
    }


    /// <summary>
    ///     Remove autopin when mineable rock is destroyed, unless it spawns another mineable rock.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Destroy))]
    private static void RemoveDestructiblePineOnDestory(Destructible __instance)
    {
        if (!__instance.TryGetComponent(out AutoPinner autoPinner))
        {
            return;
        }

        // Skip removing pin if it spawns a new ore prefab
        if (__instance.m_spawnWhenDestroyed)
        {
            GameObject spawned = __instance.m_spawnWhenDestroyed;
            if (spawned.TryGetComponent(out MineRock5 mineRock5) && DropsOre(mineRock5))
            {
                return;
            }
            if (spawned.TryGetComponent(out MineRock mineRock) && DropsOre(mineRock))
            {
                return;
            }
                
        }
        autoPinner.RemoveAutoPin();
    }
}
