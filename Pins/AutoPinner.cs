using Jotunn;
using System.Collections.Generic;
using UnityEngine;
using static DiscoveryPins.DiscoveryPins;
using static Minimap;

namespace DiscoveryPins.Pins
{
    internal class AutoPinner : MonoBehaviour
    {
        public const float CloseEnoughXZ = 5f;
        public const float CloseEnoughY = 5f;
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
        public void UpdatePinName(string pinName, bool markAsChanged = true)
        {
            AutoPinNameChanged = pinName != PinName;
            if (AutoPinNameChanged)
            {
                PinName = pinName;
            }
            // Force to be false is markAsChanged is false.
            AutoPinNameChanged = markAsChanged && AutoPinNameChanged;
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

            // check current field of view to get allowable angle deviation
            var dotTol = Mathf.Acos(Mathf.Deg2Rad * (GameCamera.instance.m_fov / 2f));
            var playerPosition = Player.m_localPlayer.transform.position;
            var lookDir = Player.m_localPlayer.m_lookDir;

            // Get bounding points via mesh renderer if one is present.
            MeshRenderer meshRenderer = null;
            if (this.TryGetComponent(out MineRock5 mineRock5) && mineRock5.m_meshRenderer)
            {
                meshRenderer = mineRock5.m_meshRenderer;
            }
            else if (this.GetComponent<Destructible>())
            {
                meshRenderer = this.GetComponentInChildren<MeshRenderer>();
            }

            // Get points to check for visibilty
            Vector3[] pointsToCheck;
            if (meshRenderer != null)
            {
                if (!meshRenderer.isVisible)  // only auto-pin if it's actually being rendered
                {
                    return;
                }
                 
                var bounds = meshRenderer.bounds;
                var bounds0p25 = bounds.min + (bounds.size / 4f);
                var boundsMid = bounds.min + (bounds.size / 2f);
                var bounds0p75 = bounds.min + (3f * bounds.size / 4f);

                pointsToCheck = new[]
                {
                    // center (2 pts)
                    Position,
                    bounds.center,

                    // Vertices (6 pts)
                    bounds.min,
                    bounds.max,
                    new(bounds.min.x, bounds.min.y, bounds.max.z),
                    new(bounds.min.x, bounds.max.y, bounds.min.z),
                    new(bounds.max.x, bounds.min.y, bounds.min.z),
                    new(bounds.min.x, bounds.max.y, bounds.max.z),
                    new(bounds.max.x, bounds.min.y, bounds.max.z),
                    new(bounds.max.x, bounds.max.y, bounds.min.z),

                    // Face center points (6 pts)
                    new(bounds.min.x, boundsMid.y, boundsMid.z),
                    new(bounds.max.x, boundsMid.y, boundsMid.z),
                    new(boundsMid.x, bounds.min.y, boundsMid.z),
                    new(boundsMid.x, bounds.max.y, boundsMid.z),
                    new(boundsMid.x, boundsMid.y, bounds.min.z),
                    new(boundsMid.x, boundsMid.y, bounds.max.z),

                    // Mid edge points (12 pts)
                    new(boundsMid.x, bounds.max.y,bounds.max.z),
                    new(boundsMid.x, bounds.min.y,bounds.max.z),
                    new(boundsMid.x, bounds.max.y,bounds.min.z),
                    new(boundsMid.x, bounds.min.y,bounds.min.z),

                    new(bounds.max.x, boundsMid.y, bounds.max.z),
                    new(bounds.min.x, boundsMid.y, bounds.max.z),
                    new(bounds.max.x, boundsMid.y, bounds.min.z),
                    new(bounds.min.x, boundsMid.y, bounds.min.z),
         
                    new(bounds.max.x, bounds.max.y, boundsMid.z),
                    new(bounds.min.x, bounds.max.y, boundsMid.z),
                    new(bounds.max.x, bounds.min.y, boundsMid.z),
                    new(bounds.min.x, bounds.min.y, boundsMid.z),

                    // Diagonal face points (24 pts)
                    new(bounds0p25.x, bounds.max.y, bounds0p25.z),
                    new(bounds0p75.x, bounds.max.y, bounds0p25.z),
                    new(bounds0p25.x, bounds.max.y, bounds0p75.z),
                    new(bounds0p75.x, bounds.max.y, bounds0p75.z),

                    new(bounds0p25.x, bounds.min.y, bounds0p25.z),
                    new(bounds0p75.x, bounds.min.y, bounds0p25.z),
                    new(bounds0p25.x, bounds.min.y, bounds0p75.z),
                    new(bounds0p75.x, bounds.min.y, bounds0p75.z),

                    new(bounds.max.x, bounds0p25.y, bounds0p25.z),
                    new(bounds.max.x, bounds0p75.y, bounds0p25.z),
                    new(bounds.max.x, bounds0p25.y, bounds0p75.z),
                    new(bounds.max.x, bounds0p75.y, bounds0p75.z),

                    new(bounds.min.x, bounds0p25.y, bounds0p25.z),
                    new(bounds.min.x, bounds0p75.y, bounds0p25.z),
                    new(bounds.min.x, bounds0p25.y, bounds0p75.z),
                    new(bounds.min.x, bounds0p75.y, bounds0p75.z),

                    new(bounds0p25.x, bounds0p25.y, bounds.max.z),
                    new(bounds0p75.x, bounds0p25.y, bounds.max.z),
                    new(bounds0p25.x, bounds0p75.y, bounds.max.z),
                    new(bounds0p75.x, bounds0p75.y, bounds.max.z),

                    new(bounds0p25.x, bounds0p25.y, bounds.min.z),
                    new(bounds0p75.x, bounds0p25.y, bounds.min.z),
                    new(bounds0p25.x, bounds0p75.y, bounds.min.z),
                    new(bounds0p75.x, bounds0p75.y, bounds.min.z),
                };
            }
            else
            {
                pointsToCheck = new[] { Position };
            }
            
            // Check all points of interest to see if they are being looked at or are close enough
            foreach (var point in pointsToCheck)
            {
                if (IsPointVisibleOrCloseEnough(point, playerPosition, lookDir, dotTol))
                {
                    AddAutoPin();
                    return;
                }
            }
        }


        /// <summary>
        ///     Rough check if the point is within camera cone or close enough.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="playerPosition"></param>
        /// <param name="lookDir"></param>
        /// <param name="dotTolerance"></param>
        /// <returns></returns>
        private bool IsPointVisibleOrCloseEnough(Vector3 pos, Vector3 playerPosition, Vector3 lookDir, float dotTolerance)
        {
            float distToPlayer = Utils.DistanceXZ(pos, playerPosition);
            if (distToPlayer > DiscoveryPins.Instance.AutoPinShortcutConfigs.Range.Value)
            {
                return false;
            }

            // Check if the player is standing very close to it and it is a bit above or below them
            bool isCloseEnough = distToPlayer <= CloseEnoughXZ && Mathf.Abs(Position.y - playerPosition.y) <= CloseEnoughY;

            // compute direction from camera to AutoPinner
            // get dot product of direction from camera to AutoPinner and camera look direction
            lookDir = Vector3.Normalize(lookDir);
            var posDir = Vector3.Normalize(pos - GameCamera.instance.transform.position);
            var dirDot = Vector3.Dot(posDir, lookDir);

            // If dot product is negative then it is in the opposite direction from camera look direction
            // If dot product is less than dotTol then it is at an angle greater than FoV/2 away from look direction.
            if ((dirDot < 0 || dirDot < dotTolerance) && !isCloseEnough)
            {
                return false;
            }

            // Check if point is underground after determining if it's close enough
            if (ZoneSystem.instance)
            {
                var groundHeight = ZoneSystem.instance.GetGroundHeight(pos);
                if (pos.y < groundHeight - 1f)
                {
                    return false;
                }
            }

            return true;
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
