using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mono.Security.X509.X520;

namespace DiscoveryPins.Extensions
{
    internal static class LocationExtensions
    {

        /// <summary>
        ///     Try getting entrance to interior and interior name.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="teleport"></param>
        /// <returns></returns>
        internal static bool TryGetInteriorEntrance(this Location location, out Teleport teleport, out string name)
        {
            teleport = null;
            name = string.Empty;
            if (!location.m_hasInterior)
            {
                return false;
            }

            for (int i = 0; i < location.transform.childCount; i++)
            {
                var child = location.transform.GetChild(i);
                if (child.TryGetComponent(out teleport) && teleport is not null)
                {
                    if (!teleport.m_enterText.IsNullOrWhiteSpace())
                    {
                        name = teleport.m_enterText;
                    }
                    return true;
                }
            }
            return false;
        }

        internal static bool TryGetOverworldDungeon(this Location location, out DungeonGenerator generator, out string name)
        {
            generator = null;
            name = string.Empty;
            if (location.m_hasInterior)
            {
                return false;
            }

            if (!location.TryGetDungeonGenerator(out generator) || generator is null)
            {
                return false;
            }

            name = generator.m_themes.ToString();
            return true;
        }

        /// <summary>
        ///     Tries to find DungeonGenerator, checks top level gameobject and first layer of child objects.
        ///     Returns find DungeonGenerator it finds.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        internal static bool TryGetDungeonGenerator(this Location location, out DungeonGenerator generator)
        {
            generator = null;
            if (location.m_generator)
            {
                generator = location.m_generator;
                return true;
            }

            for (int i = 0; i < location.transform.childCount; i++)
            {
                var child = location.transform.GetChild(i);
                if (child.TryGetComponent(out generator) && generator is not null)
                {
                    return true;
                }
            }

            return false;
        }
     

        
    }
}
