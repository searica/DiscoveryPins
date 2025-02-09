using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace DiscoveryPins.Extensions;

[HarmonyPatch]
internal static class GameObjectExtensions
{
    private const string MineRock5Name = "___MineRock5";
    private static readonly Dictionary<string, bool> IsPrefabLocationProxyMap = [];
    private static readonly Dictionary<string, bool> IsPrefabTarPitMap = [];


    internal static string GetPrefabName(this GameObject gameObject)
    {
        if (gameObject.name.Contains(MineRock5Name))
        {
            if (gameObject.TryGetComponent(out MineRock5Tracker mineRock5Tracker))
            {
                return mineRock5Tracker.m_prefabName;
            }
        }
        return Utils.GetPrefabName(gameObject);
    }

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

[HarmonyPatch]
internal class MineRock5Tracker : MonoBehaviour
{
    public string m_prefabName;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Awake))]
    private static void MineRock5TrackOnAwake(MineRock5 __instance)
    {
        if (!__instance)
        {
            return;
        }
        AddMineRock5Tracker(__instance);
    }

    public static void AddMineRock5Tracker(MineRock5 mineRock5)
    {
        if (!mineRock5.GetComponent<MineRock5Tracker>())
        {
            MineRock5Tracker tracker = mineRock5.gameObject.AddComponent<MineRock5Tracker>();
            tracker.m_prefabName = Utils.GetPrefabName(mineRock5.gameObject);
        }
    }
}
