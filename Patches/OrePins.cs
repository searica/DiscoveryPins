using System;
using System.Collections.Generic;
using DiscoveryPins.Pins;
using HarmonyLib;
using UnityEngine;
using DiscoveryPins.Extensions;


namespace DiscoveryPins.Patches
{
    [HarmonyPatch]
    internal static class OrePins
    {
        private static readonly List<string> OreNames = [
            "Tin", 
            "Copper", 
            "Silver", 
            "Obsidian",
            "Giant",
            "Tar"
        ];

        private static readonly HashSet<string> OrePrefabNames = [];

        /// <summary>
        ///     Adds AutoPinner to prefab if it is actually Ore and not already modified.
        /// </summary>
        /// <param name="prefab"></param>
        internal static void TryAddAutoPinnerToOre(GameObject prefab)
        {
            if (IsOrePrefab(prefab, out string OreName) && !OrePrefabNames.Contains(prefab.name))
            {
                OrePrefabNames.Add(prefab.name);
                AutoPinner.AddAutoPinner(prefab, OreName, AutoPins.AutoPinCategory.Ore);
            }
        }


        /// <summary>
        ///     Check if it is an Ore prefab and get Ore name
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="OreName"></param>
        /// <returns></returns>
        private static bool IsOrePrefab(GameObject gameObject, out string OreName)
        {
            bool isOrePrefab = false;
            OreName = null;
            foreach (var name in OreNames)
            {
                if (gameObject.name.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    OreName = name;
                    isOrePrefab = true;
                    break;
                }
            }

            if (!isOrePrefab) 
            {
                return false;
            }

            if (gameObject.GetComponent<Destructible>())
            {
                if (gameObject.TryGetComponent(out HoverText hoverText))
                {
                    OreName = hoverText.m_text;
                }
                return true;
            }
            else if (gameObject.TryGetComponent(out MineRock5 mineRock5))
            {
                OreName = mineRock5.m_name;
                return true;
            }
            else if (gameObject.IsTarPit())
            {
                return true;
            }

            return false;
        }


        /// <summary>
        ///     Try getting the name of the Ore that is mined from this prefab
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="OreName"></param>
        /// <returns></returns>
        internal static bool TryGetOreName(GameObject prefab, out string OreName)
        {
            foreach (var name in OreNames)
            {
                if (prefab.name.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (prefab.TryGetComponent(out HoverText hoverText))
                    {
                        OreName = hoverText.m_text;
                        return true;
                    }
                    OreName = name;
                    return true;
                }
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
        public static void MineRock5_RPCDamage_Prefix(MineRock5 __instance)
        {
            if (__instance.TryGetComponent(out AutoPinner autoPinner)){
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
        public static void AllDestroyed(MineRock5 __instance, bool __result)
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
        public static void Destructible_RPCDamage_Prefix(Destructible __instance)
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
        public static void Destructible_Destroy_Prefix(Destructible __instance)
        {
            if (__instance.TryGetComponent(out AutoPinner autoPinner))
            {
                if (__instance.m_spawnWhenDestroyed &&
                    __instance.m_spawnWhenDestroyed.GetComponent<MineRock5>() &&
                    TryGetOreName(__instance.m_spawnWhenDestroyed, out string OreName))
                {
                    // Skip removing pin if it spawns a new ore prefab
                    return;
                }

                autoPinner.RemoveAutoPin();
            }
        }
    }
}
