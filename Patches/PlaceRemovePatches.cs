using HarmonyLib;
using DiscoveryPins.Pins;

namespace DiscoveryPins.Patches;

[HarmonyPatch]
internal static class PlaceRemovePatches
{
    /// <summary>
    ///     Trigger auto-pinner when local player builds an auto-pinnable piece.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="piece"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Piece), nameof(Piece.OnPlaced))]
    public static void Piece_OnPlaced_Postfix(Piece __instance)
    {
        if (__instance && __instance.TryGetComponent(out AutoPinner autoPinner))
        {
            autoPinner.AddAutoPin();
        }
    }


    /// <summary>
    ///     Remove auto-pin if it exists whenever a piece is removed.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
    public static void Piece_DropResources_Postfix(Piece __instance)
    {
        if (__instance && __instance.TryGetComponent(out AutoPinner autoPinner))
        {
            autoPinner.RemoveAutoPin();
        }
    }
}
