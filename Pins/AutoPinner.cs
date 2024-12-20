using BepInEx.Configuration;
using Jotunn;
using MonoMod.Utils;
using System.Collections.Generic;
using UnityEngine;
using static DiscoveryPins.DiscoveryPins;
using static Minimap;

namespace DiscoveryPins.Pins
{
    internal class AutoPinner : MonoBehaviour
    {
        private const float CloseEnoughXZ = 5f;
        private const float CloseEnoughY = 15f;
        private const float FindPinPrecision = 1.0f;
        private const float InvokeRepeatingTime = 0.1f;
        public string PinName;
        private bool AutoPinNameChanged = false;


        public AutoPins.AutoPinCategory AutoPinCategory;
        private Vector3 LastPosition;
        public Vector3 Position {  
            get
            {
                if (this.transform)
                {
                    LastPosition = transform.position;
                }
                return LastPosition;
            }
        }

        /// <summary>
        ///     Add AutoPinner to target gameObject and initialize PinName and AutoPinCategory
        /// </summary>
        /// <param name="target"></param>
        /// <param name="pinName"></param>
        /// <param name="pinCategory"></param>
        internal static void AddAutoPinner(GameObject target, string pinName, AutoPins.AutoPinCategory pinCategory)
        {
            var autoPinner = target.GetOrAddComponent<AutoPinner>();
            autoPinner.PinName = pinName;
            autoPinner.AutoPinCategory = pinCategory;
            //Log.LogDebug($"Added AutoPinner with name: {pinName} and category: {pinCategory.ToString()}");
        }


        /// <summary>
        ///     Update position on Awake.
        /// </summary>
        public void Awake()
        {
            LastPosition = transform.position;
        }

        public void Start()
        {
            InvokeRepeating(nameof(CheckForAutoPinShortcut), InvokeRepeatingTime, InvokeRepeatingTime);
        }

        /// <summary>
        ///     Update PinName and flag the name as changed if value changed.
        /// </summary>
        /// <param name="pinName"></param>
        public void UpdatePinName(string pinName)
        {
            AutoPinNameChanged = pinName != PinName;
            if (AutoPinNameChanged)
            {
                PinName = pinName;
            }
        }

        /// <summary>
        ///     Trigger autopin from shortcut if enabled and within range.
        /// </summary>
        public void CheckForAutoPinShortcut()
        {
            if (!DiscoveryPins.Instance.AutoPinShortcutConfigs.Enabled.Value)
            {
                return;
            }

            if (!DiscoveryPins.Instance.AutoPinShortcutConfigs.Shortcut.Value.IsPressed())
            {
                return;
            }

            if (!Player.m_localPlayer || !GameCamera.instance){
                return;
            }

            var playerPosition = Player.m_localPlayer.transform.position;
            float distToPlayer = Utils.DistanceXZ(Position, playerPosition);
            if (distToPlayer > DiscoveryPins.Instance.AutoPinShortcutConfigs.Range.Value)
            {
                return;
            }

            // Check if the player is standing very close to it and it is a bit above or below them
            // (for stuff like Copper Ore)
            bool isCloseEnough = distToPlayer <= CloseEnoughXZ && Mathf.Abs(Position.y - playerPosition.y) <= CloseEnoughY;

            // check current field of view to get allowable angle deviation
            // compute direction from camera to AutoPinner
            // get dot product of direction from camera to AutoPinner and camera look direction
            var dotTol = Mathf.Acos(Mathf.Deg2Rad * (GameCamera.instance.m_fov / 2f));
            var posDir = Vector3.Normalize(Position - GameCamera.instance.transform.position);
            var lookDir = Vector3.Normalize(Player.m_localPlayer.m_lookDir);
            var dirDot = Vector3.Dot(posDir, lookDir);

            // If dot product is negative then it is in the opposite direction from camera look direction
            // If dot product is less than dotTol then it is at an angle greater than FoV/2 away from look direction.
            if ((dirDot < 0 || dirDot < dotTol) && !isCloseEnough)
            {
                return;
            }

            // Autopinner is within range and within field of view
            AddAutoPin();
            
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

            if (AutoPinNameChanged)
            {
                var oldPin = FindPin(Position, icon, null);
                if (oldPin != null)
                {
                    Log.LogDebug($"Removing old pin with name {oldPin.m_NamePinData.PinNameText} because auto pin name changed.");
                    Minimap.instance.RemovePin(oldPin);
                }
                AutoPinNameChanged = false;
            }

            if (FindPin(Position, icon, PinName) != null)
            {
                return false;
            }

            Log.LogDebug($"Adding Auto Pin with name: {PinName}, icon: {autoPinConfig.Icon.Value}, pinType: {icon.ToString()}");
            AddPin(Position, icon, PinName);
            return true;
        }

        private static void AddPin(Vector3 pos, PinType type, string name = null)
        {
            Minimap.instance.AddPin(pos, type, name ?? "", true, false, 0L);
        }

        /// <summary>
        ///     Remove auto pin placed by Autopinner.
        /// </summary>
        /// <returns></returns>
        public bool RemoveAutoPin()
        {
            if (!TryGetAutoPinIcon(out PinType icon))
            {
                return false;
            }
            return RemovePin(Position, icon, PinName);
        }

        /// <summary>
        ///     Remove any matching pins from the minimap. 
        ///     If name is provided then only remove a pin if it has that name.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="pinType"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool RemovePin(Vector3 pos, PinType pinType = PinType.None, string name = null)
        {
            if (FindPin(pos, pinType, name) is PinData pin)
            {
                Minimap.instance.RemovePin(pin);
                return true; 
            }
            return false;
        }

        /// <summary>
        ///     Find pin on minimap based on pos and type. 
        ///     If name is provided then check if it matches the name.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
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
            foreach (var (pin, dis) in pins)
            {
                if (closest == null || dis < closestDis)
                {
                    closest = pin;
                    closestDis = dis;
                }
            }

            if (closestDis > FindPinPrecision) 
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
