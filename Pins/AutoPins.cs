using System.Collections.Generic;
using static Minimap;

namespace DiscoveryPins.Pins
{
    internal static class AutoPins
    {
        internal enum AutoPinCategory
        {
            Dungeon,
            Location,
            Ore,
            Portal
        }

        internal static Dictionary<AutoPinCategory, PinType> DefaultPinTypes = new()
        {
            { AutoPinCategory.Dungeon, PinType.Icon4 }, // Cave
            { AutoPinCategory.Location, PinType.Icon0  }, // Fireplace
            { AutoPinCategory.Ore, PinType.Icon2 }, // Hammer
            { AutoPinCategory.Portal, PinType.Icon3 } // Ball
        };     
    }
}
