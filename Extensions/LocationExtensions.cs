using BepInEx;
using Jotunn.Extensions;
using System.Text.RegularExpressions;
using UnityEngine;


namespace DiscoveryPins.Extensions;

internal static class LocationExtensions
{
    private const string EntranceName = "GateWay";
    private const string ExteriorName = "exterior";
    private const string ExitGateWay = "ExteriorGateway";

    private static readonly Regex splitCamelCase = new Regex(@"
        (?<=[A-Z])(?=[A-Z][a-z]) |
        (?<=[^A-Z])(?=[A-Z]) |
        (?<=[A-Za-z])(?=[^A-Za-z])", 
        RegexOptions.IgnorePatternWhitespace
    );



    /// <summary>
    ///     Try getting entrance to interior and interior name.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="teleport"></param>
    /// <returns></returns>
    internal static bool TryGetInteriorEntrance(this Location location, out Teleport teleport, out string name)
    {
        if (!location.m_hasInterior)
        {
            teleport = null;
            name = string.Empty;
            return false;
        }

        // try getting the entrance via location script references
        if (TryGetDungeonEntrance(location, out teleport, out name))
        {
            return true;
        }

        // try getting the entrance via brute force search of top level children
        if (TryGetDungeonEntrance(location.transform, out teleport, out name))
        {
            return true;
        }

        // try getting the entrance via brute force search of top level children under
        // the exterior child transform.
        Transform exterior = location.transform.Find(ExteriorName);
        if (exterior && TryGetDungeonEntrance(exterior, out teleport, out name))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Tries to find the interior of the location to get a reference to the exit gateway. From there
    ///     it then gets a reference to the entrance gateway and updates the entrance and name outputs
    ///     if successfull.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="entrance"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    internal static bool TryGetDungeonEntrance(Location location, out Teleport entrance, out string name)
    {
        if (!location.m_interiorTransform)
        {
            name = string.Empty;
            entrance = null;
            return false;
        }

        // Get exit gateway of interior since that has a more reliable path
        Transform exitGate = location.m_interiorTransform.FindDeepChild(ExitGateWay);
        if (!exitGate || !exitGate.TryGetComponent(out Teleport teleportExit) || !teleportExit || !teleportExit.m_targetPoint)
        {
            name = string.Empty;
            entrance = null;
            return false;
        }

        entrance = teleportExit.m_targetPoint;

        if (!entrance.m_enterText.IsNullOrWhiteSpace())
        {
            name = entrance.m_enterText;
        }
        else if (!entrance.m_hoverText.IsNullOrWhiteSpace())
        {
            name = entrance.m_hoverText;
        }
        else
        {
            name = location.m_interiorEnvironment;
        }    
        return true;
    }

    /// <summary>
    ///     Tries to get the first top level child with a Teleport component and the enter 
    ///     text for that Teleport component with a fall back to the hover text if needed.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="entrance"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    internal static bool TryGetDungeonEntrance(Transform parent, out Teleport entrance, out string name)
    {
        name = string.Empty;
        entrance = null;

        // Check for a top level exterior Gateway with teleport component
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform child = parent.transform.GetChild(i);
            if (child.TryGetComponent(out entrance) && entrance)
            {
                if (!entrance.m_enterText.IsNullOrWhiteSpace())
                {
                    name = entrance.m_enterText;
                }
                else if (!entrance.m_hoverText.IsNullOrWhiteSpace())
                {
                    name = entrance.m_hoverText;
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    ///     If the location does not have an interior then try getting a dungeon generator 
    ///     and detecting the name of the location based on the theme of the generator.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="generator"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    internal static bool TryGetOverworldDungeon(this Location location, out DungeonGenerator generator, out string name)
    {
        
        if (location.m_hasInterior)
        {
            generator = null;
            name = string.Empty;
            return false;
        }

        if (!location.TryGetDungeonGenerator(out generator) || !generator)
        {
            generator = null;
            name = string.Empty;
            return false;
        }


        switch (generator.m_themes)
        {
            case Room.Theme.MeadowsVillage:
                name = "Draugr Village";
                break;
            case Room.Theme.AshlandRuins:
            case Room.Theme.FortressRuins:
                name = "Ruins";
                break;
            case Room.Theme.MorkHalla:
                name = "$location_morkhalla";
                break;
            default:
                name = splitCamelCase.Replace(generator.m_themes.ToString(), " ").Replace("Goblin", "Fuling");
                break;
        }
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
            Transform child = location.transform.GetChild(i);
            if (child.TryGetComponent(out generator) && generator is not null)
            {
                return true;
            }
        }

        return false;
    }


    /// <summary>
    ///     Tries to find out if this is a location that is effectively "Ore" such as a tar pit.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="generator"></param>
    /// <returns></returns>
    internal static bool TryGetLocationOreType(this Location location, out string oreType)
    {
        if (location.transform.Find("TarLiquid"))
        {
            oreType = "$item_tar";
            return true;
        }

        oreType = null;
        return false;
    }


    /// <summary>
    ///     Try to get a RuneStone component if it exists for this location.
    /// </summary>
    /// <param name="location"></param>
    /// <param name="runeStone"></param>
    /// <returns></returns>
    internal static bool TryGetRuneStone(this Location location, out RuneStone runeStone)
    {
        runeStone = location.GetComponentInChildren<RuneStone>();
        return runeStone;
    }
}
