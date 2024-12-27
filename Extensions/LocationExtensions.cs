using BepInEx;
using UnityEngine;


namespace DiscoveryPins.Extensions
{
    internal static class LocationExtensions
    {
        private const string EntranceName = "GateWay";
        private const string ExteriorName = "exterior";

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

            if (TryGetTeleportEntrance(location.transform, out teleport, out name))
            {
                return true;
            }

            // Check for a Gateway nested under the "exterior" child
            Transform exterior = location.transform.Find(ExteriorName);
            if (exterior && TryGetTeleportEntrance(exterior, out teleport, out name))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Tries to get the first top level child with a Teleport component 
        ///     and the enter text for that Teleport component.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="entrance"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static bool TryGetTeleportEntrance(Transform parent, out Teleport entrance, out string name)
        {
            name = string.Empty;
            entrance = null;

            // Check for a top level exterior Gateway with teleporting
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                var child = parent.transform.GetChild(i);
                if (child.TryGetComponent(out entrance) && entrance is not null)
                {
                    if (!entrance.m_enterText.IsNullOrWhiteSpace())
                    {
                        name = entrance.m_enterText;
                    }
                    return true;
                }
            }   
            return false;
        }

        /// <summary>
        ///     If the location does not have an interior then try getting a dungeon generator and detecting it's name.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="generator"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static bool TryGetOverworldDungeon(this Location location, out DungeonGenerator generator, out string name)
        {
            generator = null;
            name = string.Empty;
            if (location.m_hasInterior)
            {
                return false;
            }

            if (!location.TryGetDungeonGenerator(out generator) || !generator)
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
