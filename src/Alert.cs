using System;

namespace KSPAlert
{
    public enum AlertPriority
    {
        Advisory = 0,    // Blue - informational
        Caution = 1,     // Amber - action needed soon
        Warning = 2      // Red - immediate action required
    }

    public enum AlertType
    {
        // Warning level (Red)
        Terrain,
        Overheat,
        Stall,
        Impact,

        // Caution level (Amber)
        GearUp,
        LowFuel,
        LowPower,
        HighG,
        CommsLost,

        // Advisory level (Blue)
        GearDown,
        OrbitAchieved,
        SASChange,
        Staging,

        // Radio altitude callouts (landing mode)
        Altitude50,
        Altitude40,
        Altitude30,
        Altitude20,
        Altitude10,
        Altitude5,
        Retard
    }

    public class Alert
    {
        public AlertType Type { get; private set; }
        public AlertPriority Priority { get; private set; }
        public string Message { get; private set; }
        public string VoiceCallout { get; private set; }
        public float Timestamp { get; private set; }
        public bool IsActive { get; set; }
        public float LastTriggered { get; set; }
        public float CooldownSeconds { get; set; }

        public Alert(AlertType type, AlertPriority priority, string message, string voiceCallout, float cooldown = 3f)
        {
            Type = type;
            Priority = priority;
            Message = message;
            VoiceCallout = voiceCallout;
            Timestamp = UnityEngine.Time.time;
            IsActive = false;
            LastTriggered = 0f;
            CooldownSeconds = cooldown;
        }

        public bool CanTrigger()
        {
            return UnityEngine.Time.time - LastTriggered >= CooldownSeconds;
        }

        public void Trigger()
        {
            IsActive = true;
            LastTriggered = UnityEngine.Time.time;
            Timestamp = UnityEngine.Time.time;
        }

        public void Clear()
        {
            IsActive = false;
        }

        public static Alert CreateTerrain()
        {
            return new Alert(AlertType.Terrain, AlertPriority.Warning,
                "TERRAIN", "TERRAIN TERRAIN PULL UP", 2f);
        }

        public static Alert CreateGearUp()
        {
            return new Alert(AlertType.GearUp, AlertPriority.Caution,
                "GEAR", "TOO LOW GEAR", 5f);
        }

        public static Alert CreateLowFuel()
        {
            return new Alert(AlertType.LowFuel, AlertPriority.Caution,
                "FUEL LOW", "FUEL LOW", 30f);
        }

        public static Alert CreateLowPower()
        {
            return new Alert(AlertType.LowPower, AlertPriority.Caution,
                "POWER LOW", "ELECTRIC LOW", 30f);
        }

        public static Alert CreateOverheat()
        {
            return new Alert(AlertType.Overheat, AlertPriority.Warning,
                "OVERHEAT", "OVERHEAT", 5f);
        }

        public static Alert CreateStall()
        {
            return new Alert(AlertType.Stall, AlertPriority.Warning,
                "STALL", "STALL STALL", 2f);
        }

        public static Alert CreateHighG()
        {
            return new Alert(AlertType.HighG, AlertPriority.Caution,
                "HIGH G", "G LIMIT", 5f);
        }

        public static Alert CreateCommsLost()
        {
            return new Alert(AlertType.CommsLost, AlertPriority.Caution,
                "NO SIGNAL", "SIGNAL LOST", 30f);
        }

        // Radio altitude callouts - short cooldown since they're one-shot per altitude
        public static Alert CreateAltitude50()
        {
            return new Alert(AlertType.Altitude50, AlertPriority.Advisory,
                "50", "FIFTY", 0.5f);
        }

        public static Alert CreateAltitude40()
        {
            return new Alert(AlertType.Altitude40, AlertPriority.Advisory,
                "40", "FORTY", 0.5f);
        }

        public static Alert CreateAltitude30()
        {
            return new Alert(AlertType.Altitude30, AlertPriority.Advisory,
                "30", "THIRTY", 0.5f);
        }

        public static Alert CreateAltitude20()
        {
            return new Alert(AlertType.Altitude20, AlertPriority.Advisory,
                "20", "TWENTY", 0.5f);
        }

        public static Alert CreateAltitude10()
        {
            return new Alert(AlertType.Altitude10, AlertPriority.Advisory,
                "10", "TEN", 0.5f);
        }

        public static Alert CreateAltitude5()
        {
            return new Alert(AlertType.Altitude5, AlertPriority.Advisory,
                "5", "FIVE", 0.5f);
        }

        public static Alert CreateRetard()
        {
            return new Alert(AlertType.Retard, AlertPriority.Advisory,
                "RETARD", "RETARD RETARD", 0.5f);
        }
    }
}
