using Jotunn;
using System.Collections.Generic;
using UnityEngine;
using static DiscoveryPins.DiscoveryPins;
using static Minimap;

namespace DiscoveryPins.Pins
{
    internal class AutoPinner : MonoBehaviour
    {
        internal string PinName;
        internal AutoPins.AutoPinCategory AutoPinCategory;
        internal Vector3 LastPosition;

        /// <summary>
        ///     Update position on Awake.
        /// </summary>
        public void Awake()
        {
            LastPosition = transform.position;
        }


        internal static void AddAutoPinner(GameObject target, string pinName, AutoPins.AutoPinCategory pinCategory)
        {
            var autoPinner = target.GetOrAddComponent<AutoPinner>();
            autoPinner.PinName = pinName;
            autoPinner.AutoPinCategory = pinCategory;
            autoPinner.LastPosition = target.transform.position;
            Log.LogInfo($"Added AutoPinner with name: {pinName} and category: {pinCategory.ToString()}");
        }

        public bool TryGetAutoPinConfig(out DiscoveryPins.AutoPinConfig autoPinConfig)
        {
            if (DiscoveryPins.Instance.AutoPinConfigs.TryGetValue(AutoPinCategory, out autoPinConfig))
            {
                return true;
            }
            return false;
        }
        public bool IsAutoPinEnabled(out DiscoveryPins.AutoPinConfig autoPinConfig)
        {
            if (TryGetAutoPinConfig(out autoPinConfig))
            {
                return autoPinConfig.Enabled.Value;
            }
            return false;
        }

        public bool TryGetAutoPinIcon(out PinType icon)
        {   
            icon = PinType.None;
            if (TryGetAutoPinConfig(out AutoPinConfig autoPinConfig))
            {
                icon = PinNames.PinNameToType(autoPinConfig.Icon.Value);
                return true;
            }
            return false;
        }

        /// <summary>
        ///     Add pin based on configs
        /// </summary>
        /// <returns></returns>
        public bool AddAutoPin()
        {
            if (!IsAutoPinEnabled(out DiscoveryPins.AutoPinConfig autoPinConfig))
            {
                return false;
            }
            PinType icon = PinNames.PinNameToType(autoPinConfig.Icon.Value);
            if (FindPin(this.transform.position, icon, PinName) != null)
            {
                return false;
            }
            Log.LogInfo($"Adding Auto Pin with name: {PinName}, icon: {autoPinConfig.Icon.Value}, pinType: {icon.ToString()}");
            AddPin(this.transform.position, icon, PinName);
            return true;
        }

        private static void AddPin(Vector3 pos, PinType type, string name = null)
        {
            Minimap.instance.AddPin(pos, type, name ?? "", true, false, 0L);
        }

        public bool RemoveAutoPin()
        {
            if (!TryGetAutoPinIcon(out PinType icon))
            {
                return false;
            }
            if (this.transform)
            {
                LastPosition = this.transform.position;
            }
            return RemovePin(LastPosition, icon, PinName);
        }

        public static bool RemovePin(Vector3 pos, PinType pinType = PinType.None, string name = null)
        {
            if (FindPin(pos, pinType, name) is PinData pin)
            {
                Minimap.instance.RemovePin(pin);
                return true; 
            }
            return false;
        }


        public static PinData FindPin(Vector3 pos, PinType type = PinType.None, string name = null)
        {
            List<(PinData pin, float dis)> pins = new();

            foreach (var pin in Minimap.instance.m_pins)
            {
                if (type == PinType.None || pin.m_type == type)
                {
                    pins.Add((pin, Utils.DistanceXZ(pos, pin.m_pos)));
                }   
            }

            PinData closest = null;
            float closestDis = float.MaxValue;

            foreach (var pairs in pins)
            {
                if (closest == null || pairs.dis < closestDis)
                {
                    closest = pairs.pin;
                    closestDis = pairs.dis;
                }
            }
                

            if (closestDis > 1f)
            {
                return null;
            }
                

            if (!string.IsNullOrEmpty(name) && closest.m_name != name)
            {
                return null;
            }
                
            return closest;
        }
    }
}
