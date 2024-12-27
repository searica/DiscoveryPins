using UnityEngine;

namespace DiscoveryPins.Extensions
{
    internal static class GameObjectExtensions
    {
        /// <summary>
        ///     Checks if this is a prefab without an existing parent transform.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        internal static bool IsTopLevelPrefab(this GameObject gameObject)
        {
            return !gameObject.transform.parent;
        }


        internal static bool IsTarPit(this GameObject gameObject)
        {
            return gameObject.transform.Find("TarLiquid");
        }
    }
}
