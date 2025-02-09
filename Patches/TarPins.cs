using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace DiscoveryPins.Patches
{
    [HarmonyPatch]
    internal static class TarPins
    {
        public static string name = "Tar";

        // maybe awake on liquid volume and check if it is tar?
    }
}
