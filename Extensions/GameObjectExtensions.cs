using System.Collections.Generic;
using UnityEngine;

namespace DiscoveryPins.Extensions
{
    internal static class GameObjectExtensions
    {
        private static readonly Dictionary<string, bool> IsPrefabLocationProxyMap = [];
        private static readonly Dictionary<string, bool> IsPrefabTarPitMap = [];
        /// <summary>
        ///     Checks if this is a prefab without an existing parent transform.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        internal static bool IsTopLevelPrefab(this GameObject gameObject)
        {
            return !gameObject.transform.parent;
        }

        /// <summary>
        ///     Checks if object is a tar pit based on whether it has a "TarLiquid" child.
        ///     Caches results as a map of prefab name to booleans to avoid extra calls
        ///     to transform.Find
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        internal static bool IsTarPit(this GameObject gameObject)
        {
            string prefabName = Utils.GetPrefabName(gameObject);
            if (!IsPrefabTarPitMap.TryGetValue(prefabName, out bool isTar)) 
            {
                isTar = gameObject.transform.Find("TarLiquid");
                IsPrefabTarPitMap[prefabName] = isTar;
            }
            return isTar;
        }


        /// <summary>
        ///     Checks if object is a tar pit based on whether it has a "TarLiquid" child.
        ///     Caches results as a map of prefab name to booleans to avoid extra calls
        ///     to GetComponent
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        internal static bool IsLocationProxy(this GameObject gameObject)
        {
            string prefabName = Utils.GetPrefabName(gameObject);
            if (!IsPrefabLocationProxyMap.TryGetValue(prefabName, out bool isLocationProxy))
            {
                isLocationProxy = gameObject.GetComponent<LocationProxy>();
                IsPrefabTarPitMap[prefabName] = isLocationProxy;
            }
            return isLocationProxy;
        }
    }
}
