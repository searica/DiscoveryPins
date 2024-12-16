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
        // Want to add an autopinner component to all ores in the game by editing their prefabs
        // Trigger pins via MineRock5 and Destructible component
        private static readonly List<string> OreNames = new () { "Tin", "Copper", "Silver" };
        private static Dictionary<string, GameObject> OrePrefabs = new();

        /// <summary>
        ///     Hook to edit prefabs for ores
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
        public static void ZoneSystemStartPrefix()
        {
            // If loading into game world and prefabs have not been added
            if (SceneManager.GetActiveScene().name != "main")
            {
                return;
            }

            foreach (var prefab in ZNetScene.instance.m_prefabs)
            {
                if (!prefab.transform.parent && IsOrePrefab(prefab, out string OreName) && !OrePrefabs.ContainsKey(prefab.name))
                {
                    Log.LogInfo($"Found ore prefab: {prefab.name}");
                    OrePrefabs.Add(prefab.name, prefab);
                    AutoPinner.AddAutoPinner(prefab, OreName, AutoPins.AutoPinCategory.Ore);
                    // AutoPinner component is ending up with nonsense values for names on prefabs
                    // since it's getting cloned and the default values aren't set
                }
            }
        }

        internal static bool IsOrePrefab(GameObject prefab, out string OreName)
        {
            OreName = string.Empty;
            if (prefab.GetComponent<Destructible>())
            {
                if (TryGetOreName(prefab, out OreName))
                {
                    return true;
                }
            }

            if (prefab.GetComponent<MineRock5>()){
                if (TryGetOreName(prefab, out OreName))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TryGetOreName(GameObject prefab, out string OreName)
        {
            OreName = string.Empty;  
            foreach (var name in OreNames)
            {
                if (prefab.name.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    OreName = name;
                    return true;
                }
            }
            return false;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.RPC_Damage))]
        public static void MineRock5_RPCDamage_Prefix(MineRock5 __instance)
        {
            if (__instance.TryGetComponent(out AutoPinner autoPinner)){
                Log.LogInfo($"MineRock5_RPC_Damage: pin name: {autoPinner.PinName}, category: {autoPinner.AutoPinCategory.ToString()}");
                autoPinner.AddAutoPin();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Destructible), nameof(Destructible.RPC_Damage))]
        public static void Destructible_RPCDamage_Prefix(Destructible __instance)
        {
            if (__instance.TryGetComponent(out AutoPinner autoPinner))
            {
                Log.LogInfo($"Destructible_RPC_Damage: pin name: {autoPinner.PinName}, category: {autoPinner.AutoPinCategory.ToString()}");
                autoPinner.AddAutoPin();
            }
        }

//[Info: DiscoveryPins] Destructible_RPC_Damage: pin name: , category: Dungeon
//[Info   :DiscoveryPins] Adding Auto Pin with name: , category: Cave

    }
}
