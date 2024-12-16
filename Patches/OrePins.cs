using System;
using System.Collections.Generic;
using DiscoveryPins.Pins;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using DiscoveryPins.Extensions;


namespace DiscoveryPins.Patches
{
    [HarmonyPatch]
    internal static class OrePins
    {
        private static readonly List<string> OreNames = new () { "Tin", "Copper", "Silver" };
        //private static Dictionary<string, GameObject> OrePrefabs = new();
        private static HashSet<string> OrePrefabNames = new();

        /// <summary>
        ///     Add Autopinner component to all ores in the game via editing their prefabs.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
        private static void ZoneSystem_Start_Prefix()
        {
            // If loading into game world and prefabs have not been added
            if (SceneManager.GetActiveScene().name != "main")
            {
                return;
            }

            // Get prefabs that are mineable ores and modify them
            foreach (var prefab in ZNetScene.instance.m_prefabs)
            {
                if (!prefab.transform.parent && IsOrePrefab(prefab, out string OreName) && !OrePrefabNames.Contains(prefab.name))
                {
                    OrePrefabNames.Add(prefab.name);
                    AutoPinner.AddAutoPinner(prefab, OreName, AutoPins.AutoPinCategory.Ore);
                }
            }
        }

        private static void AddAutoPinnerIfOre(GameObject gameObject)
        {
            if (gameObject.GetComponent<Destructible>() || gameObject.GetComponent<MineRock5>())
            {
                if (TryGetOreName(gameObject, out string OreName))
                {
                    AutoPinner.AddAutoPinner(gameObject, OreName, AutoPins.AutoPinCategory.Ore);
                }
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

            if (gameObject.GetComponent<Destructible>() || gameObject.GetComponent<MineRock5>())
            {
                return TryGetOreName(gameObject, out OreName);
            }
            OreName = null;
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
                    OreName = name;
                    return true;
                }
            }

            OreName = null;
            return false;
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(Destructible), nameof(Destructible.Awake))]
        //public static void Destructible_Awake_Postfix(Destructible __instance)
        //{
        //    AddAutoPinnerIfOre(__instance.gameObject);
        //}

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Awake))]
        //public static void MineRock5_Awake_Postfix(MineRock5 __instance)
        //{
        //    AddAutoPinnerIfOre(__instance.gameObject);
        //}

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
