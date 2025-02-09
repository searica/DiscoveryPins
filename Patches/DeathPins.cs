using DiscoveryPins.Pins;
using HarmonyLib;
using UnityEngine;
using static Minimap;
using Logging;

namespace DiscoveryPins.Patches;

[HarmonyPatch]
internal static class DeathPins
{
    public static bool InvIsEmpty;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    public static void PlayerOnDeath_Prefix(Player __instance)
    {
        InvIsEmpty = __instance.m_inventory.NrOfItems() < 1;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    public static void PlayerOnDeath_Postfix(Player __instance)
    {

        if (!InvIsEmpty || DiscoveryPins.Instance.DeathPinConfigs.PinWhenInvIsEmpty.Value)
        {
            return;
        }

        Vector3 pos = __instance.transform.position;
        Log.LogDebug($"Negating pin at '{pos.ToString("F0")}' because inventory was empty\n");
        AutoPinner.RemovePin(pos, PinType.Death);

        PlayerProfile pp = Game.instance.GetPlayerProfile();
        pp.GetWorldData(ZNet.instance.GetWorldUID()).m_haveDeathPoint = false;
        pp.GetWorldData(ZNet.instance.GetWorldUID()).m_deathPoint = Vector3.zero;
    }

    /// <summary>
    ///     Tombstone gives boost when it is destroyed.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TombStone), nameof(TombStone.GiveBoost))]
    public static void TombStoneGiveBoost_Postfix(TombStone __instance)
    {
        if (!DiscoveryPins.Instance.DeathPinConfigs.AutoRemoveEnabled.Value)
        {
            return;
        }
        AutoPinner.RemovePin(__instance.transform.position, PinType.Death);
    }
}
