using BepInEx.Configuration;
using System.Collections.Generic;
using System.Linq;
using static Minimap;

namespace DiscoveryPins.Pins;

/// <summary>
///     Manages info about pins placeable by players
/// </summary>
internal static class PlaceablePins
{

    private static readonly List<PinType> PlaceablePinTypes = new() { PinType.Icon0, PinType.Icon1, PinType.Icon2, PinType.Icon3, PinType.Icon4 };
    private static List<string> _PlaceablePinNames;

    /// <summary>
    ///     Friendly names of placeable pins.
    /// </summary>
    private static List<string> PlaceablePinNames => _PlaceablePinNames ??= PlaceablePinTypes.Select(x => PinNames.PinTypeToName(x)).ToList();


    /// <summary>
    ///     Allowed placeable pin names in config settings.
    /// </summary>
    internal static AcceptableValueList<string> AllowedPlaceablePinNames
    {
        get
        {
            return new AcceptableValueList<string>(PlaceablePinNames.ToArray());
        }
    }
}
