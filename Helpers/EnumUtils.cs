using System;
using System.Collections.Generic;
using System.Linq;


namespace DiscoveryPins.Helpers;

internal static class EnumUtils
{
    internal static IEnumerable<T> GetEnumValues<T>()
    {
        return Enum.GetValues(typeof(T)).Cast<T>();
    }

    public static T ParseEnum<T>(string value)
    {
        return (T)Enum.Parse(typeof(T), value, true);
    }
}
