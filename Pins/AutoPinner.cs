using Jotunn;
using System.Collections.Generic;
using UnityEngine;
using static DiscoveryPins.DiscoveryPins;
using static Minimap;
using Logging;
using DiscoveryPins.Extensions;

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

            if (!Player.m_localPlayer || !GameCamera.instance) {
                return;
            }

            // check current field of view to get allowable angle deviation
            var dotTol = Mathf.Acos(Mathf.Deg2Rad * (GameCamera.instance.m_fov / 2f));
            var playerPosition = Player.m_localPlayer.transform.position;
            var lookDir = Player.m_localPlayer.m_lookDir;

            if (!TryGetPointsToCheck(out List<Vector3> pointsToCheck))
            {
                return;
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
        ///     Tries to determine an appropriate bounding box and use that
        ///     to get points to check for visibility of.
        /// </summary>
        /// <param name="pointsToCheck"></param>
        /// <returns></returns>
        private bool TryGetPointsToCheck(out List<Vector3> pointsToCheck)
        {
            // Get bounding points via mesh renderer if one is present.
            MeshRenderer meshRenderer = null;
            Location location = null;
            pointsToCheck = null;
            Bounds bounds;

            if (this.TryGetComponent(out MineRock5 mineRock5) && mineRock5.m_meshRenderer)
            {
                meshRenderer = mineRock5.m_meshRenderer;
            }
            else if (this.GetComponent<Destructible>())
            {
                meshRenderer = this.GetComponentInChildren<MeshRenderer>();
            }
            else if (gameObject.TryGetComponent(out location))
            {
                 // just want to get location and move on if true.
            }
            else if (gameObject.IsLocationProxy())
            {
                location = gameObject.GetComponentInChildren<Location>();
            }

            if (meshRenderer)
            {
                // only auto-pin if it's actually being rendered
                if (!meshRenderer.isVisible)
                {
                    return false;
                }
                bounds = meshRenderer.bounds;
            }
            else if (location)
            {
                // estimate bounds from location radius
                float size = location.m_exteriorRadius * 2f;
                bounds = new(location.transform.position, new Vector3(size, size, size));
            }
            else
            {
                // just check the center
                pointsToCheck = [Position];
                return true;
            }

            // create grid of points throughout the volume based on close enough spacing
            // if the object has size smaller than close enough then it just adds the vertices
            GetPointSpacing(bounds.size.x, CloseEnoughXZ, out int nXpts, out float xSpacing);
            GetPointSpacing(bounds.size.y, CloseEnoughY, out int nYpts, out float ySpacing);
            GetPointSpacing(bounds.size.z, CloseEnoughXZ, out int nZpts, out float zSpacing);
            pointsToCheck = [Position, bounds.center]; // start with center points
            for (int i = 0; i < nXpts; i++)
            {
                for (int j = 0; j < nYpts; j++)
                {
                    for (int k = 0; k < nZpts; k++)
                    {
                        pointsToCheck.Add(bounds.min + new Vector3(i*xSpacing, j*ySpacing, k*zSpacing));
                    }
                }
            }
            return true;
        }

        private static void GetPointSpacing(float size, float targetSpacing, out int nPts, out float spacing)
        {
            nPts = Mathf.CeilToInt(size / targetSpacing);
            spacing = size / nPts;

            nPts++; // account for end point
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


            if (AutoPinNameChanged && FindExistingPin(Position, icon, null) is PinData oldPin)
            {
                // Remove and replace pin with new name
                Log.LogDebug($"Removing old pin with name {oldPin.m_NamePinData.PinNameText} because auto pin name changed.");
                Minimap.instance.RemovePin(oldPin);
                AutoPinNameChanged = false;
                AddPin(Position, icon, PinName);
                return true;
            }

            // Don't auto-pin if other pins are too close.
            var closestPin = FindClosestPin(Position, out float closestDis);
            if (closestPin != null && closestDis < DiscoveryPins.Instance.PinSpacing.Value)
            {
                return false;
            }

            // Don't auto-pin if the pin already exists
            if (FindExistingPin(Position, icon, PinName) != null)
            {
                return false;
            }

            Log.LogDebug($"Adding Auto Pin with name: {PinName}, icon: {autoPinConfig.Icon.Value}, pinType: {icon}");
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
            if (FindExistingPin(pos, pinType, name) is PinData pin)
            {
                Minimap.instance.RemovePin(pin);
                return true; 
            }
            return false;
        }


        /// <summary>
        ///     Find pin on minimap based on pos, type, and optionally the name
        /// </summary>
        /// <param name="pos">Position to check for closest pin relative to.</param>
        /// <param name="type">Pin icon type</param>
        /// <param name="name">Name of the pin to find.</param>
        /// <returns></returns>
        public static PinData FindExistingPin(Vector3 pos, PinType type = PinType.None, string name = null)
        {
            var pin = FindClosestPin(pos, out float closestDis, type);
            if (closestDis > FindPinPrecision)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(name) && pin.m_name != name)
            {
                return null;
            }
            return pin;
        }


        /// <summary>
        ///     Find closest pin on minimap based on pos and type. 
        /// </summary>
        /// <param name="pos">Position to check for closest pin relative to.</param>
        /// <param name="type">Pin icon type</param>
        /// <param name="closestDis">The distanec to the closest pin.</param>
        /// <returns></returns>
        public static PinData FindClosestPin(Vector3 pos, out float closestDis, PinType type = PinType.None)
        {
            List<(PinData pin, float dis)> pins = [];
            foreach (var pin in Minimap.instance.m_pins)
            {
                if (type == PinType.None || pin.m_type == type){
                    pins.Add((pin, Utils.DistanceXZ(pos, pin.m_pos)));
                }   
            }
   
            PinData closest = null;
            closestDis = float.MaxValue;
            foreach (var (pin, dis) in pins)
            {
                if (closest == null || dis < closestDis)
                {
                    closest = pin;
                    closestDis = dis;
                }
            }

            return closest;
        }
    }
}
