
using System.Linq;
using UnityEngine.SceneManagement;
using HarmonyLib;

using DiscoveryPins.Pins;
using DiscoveryPins.Extensions;

namespace DiscoveryPins.Patches
{
    [HarmonyPatch]
    internal static class ZoneSystemPatches
    {

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
                if (!prefab.IsTopLevelPrefab())
                {
                    continue;
                }

                OrePins.TryAddAutoPinnerToOre(prefab);
                PortalPins.TryAddAutoPinnerToPortal(prefab);
            }
        }
    }
}
