using DiscoveryPins.Helpers;
using System.Collections.Generic;
using static Minimap;

namespace DiscoveryPins.Pins
{
    internal class PinNames
    {
            
        /// <summary>
        ///     Map unfriendly names to friendly names
        /// </summary>
        private static readonly Dictionary<PinType, string> PinTypeToNameMap = new()
        {
            { PinType.Icon0, "Fireplace" },
            { PinType.Icon1, "House" },
            { PinType.Icon2, "Hammer" },
            { PinType.Icon3, "Ball" },
            { PinType.Icon4, "Cave" },

        };

        private static Dictionary<string, PinType> _PinNameToTypeMap;  // cache

        /// <summary>
        ///     Map friendly names to enum type
        /// </summary>
        private static Dictionary<string, PinType> PinNameToTypeMap
        {
            get
            {
                if (_PinNameToTypeMap == null)
                {
                    _PinNameToTypeMap = new Dictionary<string, PinType>();
                    foreach (KeyValuePair<PinType, string> pair in PinTypeToNameMap)
                    {
                        _PinNameToTypeMap.Add(pair.Value, pair.Key);
                    }
                }
                return _PinNameToTypeMap;
            }
        }

        /// <summary>
        ///     Get friendly name from PinType
        /// </summary>
        /// <param name="pinType"></param>
        /// <returns></returns>
        internal static string PinTypeToName(PinType pinType)
        {
            if (PinTypeToNameMap.TryGetValue(pinType, out var name))
            {
                return name;
            }
            return pinType.ToString();
        }

        /// <summary>
        ///     Convert friendly name to PinType
        /// </summary>
        /// <param name="pinName"></param>
        /// <returns></returns>
        internal static PinType PinNameToType(string pinName)
        {
            if (PinNameToTypeMap.TryGetValue(pinName, out var pinType))
            {
                return pinType;
            }
            return EnumUtils.ParseEnum<PinType>(pinName);

        }
    }
}



