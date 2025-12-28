using System.Collections.Generic;
using UnityEngine;

namespace KSPAlert
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AlertManager : MonoBehaviour
    {
        public static AlertManager Instance { get; private set; }

        public AlertConfig Config { get; private set; }

        private Dictionary<AlertType, Alert> alerts;
        private List<Alert> activeAlerts;

        private AlertDisplay display;
        private AlertAudio audio;

        // Vessel data cache
        private Vessel vessel;
        private double altitudeAGL;
        private double verticalSpeed;
        private double surfaceSpeed;
        private double fuelPercent;
        private double powerPercent;
        private double gForce;
        private double maxPartTemp;
        private bool hasGear;
        private bool gearDeployed;
        private bool hasComms;

        // Landing mode state
        private bool isInLandingMode;
        private int lastAltitudeCallout;  // Track which callout was last triggered

        private float lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.1f; // 10 Hz update rate

        void Awake()
        {
            Instance = this;
            Config = new AlertConfig();
            Config.Load();

            InitializeAlerts();
            activeAlerts = new List<Alert>();
        }

        void Start()
        {
            display = gameObject.AddComponent<AlertDisplay>();
            audio = gameObject.AddComponent<AlertAudio>();

            Debug.Log("[KSP-Alert] Alert system initialized");
        }

        void OnDestroy()
        {
            Config.Save();
            Instance = null;
        }

        private void InitializeAlerts()
        {
            alerts = new Dictionary<AlertType, Alert>
            {
                { AlertType.Terrain, Alert.CreateTerrain() },
                { AlertType.GearUp, Alert.CreateGearUp() },
                { AlertType.LowFuel, Alert.CreateLowFuel() },
                { AlertType.LowPower, Alert.CreateLowPower() },
                { AlertType.Overheat, Alert.CreateOverheat() },
                { AlertType.Stall, Alert.CreateStall() },
                { AlertType.HighG, Alert.CreateHighG() },
                { AlertType.CommsLost, Alert.CreateCommsLost() },
                // Radio altitude callouts
                { AlertType.Altitude50, Alert.CreateAltitude50() },
                { AlertType.Altitude40, Alert.CreateAltitude40() },
                { AlertType.Altitude30, Alert.CreateAltitude30() },
                { AlertType.Altitude20, Alert.CreateAltitude20() },
                { AlertType.Altitude10, Alert.CreateAltitude10() },
                { AlertType.Altitude5, Alert.CreateAltitude5() },
                { AlertType.Retard, Alert.CreateRetard() }
            };

            lastAltitudeCallout = 999;  // Start with high value
        }

        void Update()
        {
            if (!Config.Enabled) return;
            if (!HighLogic.LoadedSceneIsFlight) return;

            vessel = FlightGlobals.ActiveVessel;
            if (vessel == null) return;

            // Throttle updates
            if (Time.time - lastUpdateTime < UPDATE_INTERVAL) return;
            lastUpdateTime = Time.time;

            UpdateVesselData();
            CheckAllAlerts();
        }

        private void UpdateVesselData()
        {
            altitudeAGL = vessel.radarAltitude;
            verticalSpeed = vessel.verticalSpeed;
            surfaceSpeed = vessel.srfSpeed;
            gForce = vessel.geeForce;

            // Calculate fuel percentage
            double totalFuel = 0;
            double maxFuel = 0;
            foreach (var part in vessel.parts)
            {
                foreach (var resource in part.Resources)
                {
                    if (resource.resourceName == "LiquidFuel" ||
                        resource.resourceName == "Oxidizer" ||
                        resource.resourceName == "MonoPropellant")
                    {
                        totalFuel += resource.amount;
                        maxFuel += resource.maxAmount;
                    }
                }
            }
            fuelPercent = maxFuel > 0 ? (totalFuel / maxFuel) * 100 : 100;

            // Calculate power percentage
            double totalPower = 0;
            double maxPower = 0;
            foreach (var part in vessel.parts)
            {
                foreach (var resource in part.Resources)
                {
                    if (resource.resourceName == "ElectricCharge")
                    {
                        totalPower += resource.amount;
                        maxPower += resource.maxAmount;
                    }
                }
            }
            powerPercent = maxPower > 0 ? (totalPower / maxPower) * 100 : 100;

            // Check for landing gear using action groups
            hasGear = false;
            gearDeployed = vessel.ActionGroups[KSPActionGroup.Gear];

            // Check if vessel has any gear parts
            foreach (var part in vessel.parts)
            {
                foreach (var module in part.Modules)
                {
                    if (module.moduleName == "ModuleWheelDeployment" ||
                        module.moduleName == "ModuleLandingGear" ||
                        module.moduleName == "ModuleLandingLeg")
                    {
                        hasGear = true;
                        break;
                    }
                }
                if (hasGear) break;
            }

            // Check max part temperature
            maxPartTemp = 0;
            foreach (var part in vessel.parts)
            {
                double tempPercent = part.temperature / part.maxTemp * 100;
                if (tempPercent > maxPartTemp)
                {
                    maxPartTemp = tempPercent;
                }
            }

            // Check comms
            hasComms = vessel.Connection != null && vessel.Connection.IsConnected;

            // Determine if we're in landing mode
            // Landing mode: gear deployed, descending (any rate), low altitude
            // This suppresses terrain and gear warnings when you're intentionally landing
            isInLandingMode = gearDeployed &&
                              verticalSpeed < 0 &&
                              altitudeAGL < 60;  // Below 60m with gear down = landing

            // Reset altitude callout tracking if we climb back up above 60m
            if (altitudeAGL > 60)
            {
                lastAltitudeCallout = 999;
            }
        }

        private void CheckAllAlerts()
        {
            activeAlerts.Clear();

            // In landing mode, skip terrain and gear warnings, show altitude callouts instead
            if (isInLandingMode)
            {
                CheckLandingCallouts();
            }
            else
            {
                CheckTerrainAlert();
                CheckGearAlert();
            }

            CheckFuelAlert();
            CheckPowerAlert();
            CheckOverheatAlert();
            CheckHighGAlert();
            CheckCommsAlert();

            // Sort by priority (highest first)
            activeAlerts.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            // Update display
            display?.UpdateAlerts(activeAlerts);
        }

        private bool IsAlertSilenced(AlertType type)
        {
            if (AlertMainWindow.Instance != null && AlertMainWindow.Instance.IsAlertSilenced(type))
                return true;
            return false;
        }

        private void CheckLandingCallouts()
        {
            if (!Config.LandingCalloutsEnabled) return;

            // Only call out each altitude once per approach, in descending order
            int currentAlt = (int)altitudeAGL;
            AlertType? calloutType = null;

            // Check altitude thresholds - only trigger if we haven't called this one yet
            if (currentAlt <= 5 && lastAltitudeCallout > 5)
            {
                calloutType = AlertType.Retard;  // RETARD at 5m and below
                lastAltitudeCallout = 5;
            }
            else if (currentAlt <= 10 && currentAlt > 5 && lastAltitudeCallout > 10)
            {
                calloutType = AlertType.Altitude10;
                lastAltitudeCallout = 10;
            }
            else if (currentAlt <= 20 && currentAlt > 10 && lastAltitudeCallout > 20)
            {
                calloutType = AlertType.Altitude20;
                lastAltitudeCallout = 20;
            }
            else if (currentAlt <= 30 && currentAlt > 20 && lastAltitudeCallout > 30)
            {
                calloutType = AlertType.Altitude30;
                lastAltitudeCallout = 30;
            }
            else if (currentAlt <= 40 && currentAlt > 30 && lastAltitudeCallout > 40)
            {
                calloutType = AlertType.Altitude40;
                lastAltitudeCallout = 40;
            }
            else if (currentAlt <= 50 && currentAlt > 40 && lastAltitudeCallout > 50)
            {
                calloutType = AlertType.Altitude50;
                lastAltitudeCallout = 50;
            }

            if (calloutType.HasValue && alerts.ContainsKey(calloutType.Value))
            {
                var alert = alerts[calloutType.Value];
                if (alert.CanTrigger())
                {
                    alert.Trigger();
                    audio?.PlayAlert(alert);
                }
                activeAlerts.Add(alert);
            }
        }

        private void CheckTerrainAlert()
        {
            if (!Config.TerrainEnabled) return;

            var alert = alerts[AlertType.Terrain];

            // Calculate time to impact based on current descent rate
            // Only valid if descending (negative vertical speed)
            double timeToImpact = double.MaxValue;
            if (verticalSpeed < -1) // Descending at least 1 m/s
            {
                timeToImpact = altitudeAGL / (-verticalSpeed);
            }

            // Trigger if:
            // 1. Time to impact is less than configured seconds (default 6.5s), OR
            // 2. Below minimum altitude AND descending fast (fallback)
            bool timeBasedDanger = timeToImpact <= Config.TerrainWarningTime &&
                                   vessel.situation == Vessel.Situations.FLYING;

            bool altitudeBasedDanger = altitudeAGL < Config.TerrainWarningAltitude &&
                                       verticalSpeed < Config.TerrainMinDescentRate &&
                                       vessel.situation == Vessel.Situations.FLYING;

            bool inDanger = timeBasedDanger || altitudeBasedDanger;

            if (inDanger)
            {
                if (alert.CanTrigger() && !IsAlertSilenced(AlertType.Terrain))
                {
                    alert.Trigger();
                    audio?.PlayAlert(alert);
                }
                activeAlerts.Add(alert);
            }
            else
            {
                alert.Clear();
            }
        }

        private void CheckGearAlert()
        {
            if (!Config.GearEnabled) return;
            if (!hasGear) return;

            var alert = alerts[AlertType.GearUp];

            // Warn if low, slow, descending, and gear not deployed
            bool needsGear = altitudeAGL < Config.GearWarningAltitude &&
                            surfaceSpeed < Config.GearWarningSpeed &&
                            verticalSpeed < -1 &&
                            !gearDeployed &&
                            vessel.situation == Vessel.Situations.FLYING;

            if (needsGear)
            {
                if (alert.CanTrigger() && !IsAlertSilenced(AlertType.GearUp))
                {
                    alert.Trigger();
                    audio?.PlayAlert(alert);
                }
                activeAlerts.Add(alert);
            }
            else
            {
                alert.Clear();
            }
        }

        private void CheckFuelAlert()
        {
            if (!Config.FuelEnabled) return;

            var alert = alerts[AlertType.LowFuel];

            if (fuelPercent < Config.FuelWarningPercent)
            {
                if (alert.CanTrigger() && !IsAlertSilenced(AlertType.LowFuel))
                {
                    alert.Trigger();
                    audio?.PlayAlert(alert);
                }
                activeAlerts.Add(alert);
            }
            else
            {
                alert.Clear();
            }
        }

        private void CheckPowerAlert()
        {
            if (!Config.PowerEnabled) return;

            var alert = alerts[AlertType.LowPower];

            if (powerPercent < Config.PowerWarningPercent)
            {
                if (alert.CanTrigger() && !IsAlertSilenced(AlertType.LowPower))
                {
                    alert.Trigger();
                    audio?.PlayAlert(alert);
                }
                activeAlerts.Add(alert);
            }
            else
            {
                alert.Clear();
            }
        }

        private void CheckOverheatAlert()
        {
            if (!Config.OverheatEnabled) return;

            var alert = alerts[AlertType.Overheat];

            if (maxPartTemp > Config.OverheatWarningPercent)
            {
                if (alert.CanTrigger() && !IsAlertSilenced(AlertType.Overheat))
                {
                    alert.Trigger();
                    audio?.PlayAlert(alert);
                }
                activeAlerts.Add(alert);
            }
            else
            {
                alert.Clear();
            }
        }

        private void CheckHighGAlert()
        {
            if (!Config.HighGEnabled) return;

            var alert = alerts[AlertType.HighG];

            if (gForce > Config.HighGWarning)
            {
                if (alert.CanTrigger() && !IsAlertSilenced(AlertType.HighG))
                {
                    alert.Trigger();
                    audio?.PlayAlert(alert);
                }
                activeAlerts.Add(alert);
            }
            else
            {
                alert.Clear();
            }
        }

        private void CheckCommsAlert()
        {
            if (!Config.CommsEnabled) return;

            var alert = alerts[AlertType.CommsLost];

            if (!hasComms && vessel.situation != Vessel.Situations.PRELAUNCH)
            {
                if (alert.CanTrigger() && !IsAlertSilenced(AlertType.CommsLost))
                {
                    alert.Trigger();
                    audio?.PlayAlert(alert);
                }
                activeAlerts.Add(alert);
            }
            else
            {
                alert.Clear();
            }
        }

        public void TriggerAlert(AlertType type)
        {
            if (alerts.ContainsKey(type))
            {
                var alert = alerts[type];
                alert.Trigger();
                audio?.PlayAlert(alert);
            }
        }

        public void TriggerTestAlert(AlertType type)
        {
            // Force trigger for testing, bypass cooldown
            if (alerts.ContainsKey(type))
            {
                var alert = alerts[type];
                alert.LastTriggered = 0f; // Reset cooldown
                alert.Trigger();
                audio?.PlayAlert(alert);
                activeAlerts.Add(alert);
                display?.UpdateAlerts(activeAlerts);
            }
        }

        // Public properties for UI display
        public double AltitudeAGL => altitudeAGL;
        public double VerticalSpeed => verticalSpeed;
        public bool IsLandingMode => isInLandingMode;
        public bool GearDeployed => gearDeployed;
        public int LastAltitudeCallout => lastAltitudeCallout;

        // Check if audio is currently playing
        public bool IsAudioPlaying => audio != null && audio.IsPlaying();
    }
}
