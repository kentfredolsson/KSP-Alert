using UnityEngine;

namespace KSPAlert
{
    public class AlertConfig
    {
        // Master toggle
        public bool Enabled = true;
        public bool AudioEnabled = true;
        public float MasterVolume = 0.8f;

        // Individual alert toggles
        public bool TerrainEnabled = true;
        public bool GearEnabled = true;
        public bool FuelEnabled = true;
        public bool PowerEnabled = true;
        public bool OverheatEnabled = true;
        public bool StallEnabled = true;
        public bool HighGEnabled = true;
        public bool CommsEnabled = true;

        // Thresholds
        public float TerrainWarningTime = 6.5f;          // seconds to impact
        public float TerrainWarningAltitude = 500f;      // meters AGL (fallback)
        public float TerrainCautionAltitude = 1000f;     // meters AGL
        public float TerrainMinDescentRate = -50f;       // m/s (negative = descending)

        public float GearWarningAltitude = 300f;         // meters AGL
        public float GearWarningSpeed = 100f;            // m/s

        public float FuelWarningPercent = 10f;           // percent
        public float FuelCautionPercent = 25f;           // percent

        public float PowerWarningPercent = 10f;          // percent
        public float PowerCautionPercent = 25f;          // percent

        public float OverheatWarningPercent = 90f;       // percent of max temp

        public float StallMarginPercent = 10f;           // percent above stall speed

        public float HighGWarning = 8f;                  // G-force
        public float HighGCaution = 6f;                  // G-force

        // Landing mode settings (radio altitude callouts)
        public bool LandingCalloutsEnabled = true;
        public float LandingModeMaxDescentRate = 100f;   // m/s - must be descending slower than this
        public float LandingModeMaxSpeed = 150f;         // m/s - must be slower than this

        // Visual settings
        public bool ScreenFlashEnabled = true;
        public float FlashIntensity = 0.3f;

        // Keybind settings
        public KeyCode ToggleKey = KeyCode.F12;

        // Display position
        public float DisplayX = 0.5f;                    // Screen center X (0-1)
        public float DisplayY = 0.15f;                   // Near top (0-1)

        private static readonly string ConfigPath =
            KSPUtil.ApplicationRootPath + "GameData/KSP-Alert/settings.cfg";

        public void Save()
        {
            ConfigNode root = new ConfigNode("KSP_ALERT_CONFIG");

            root.AddValue("Enabled", Enabled);
            root.AddValue("AudioEnabled", AudioEnabled);
            root.AddValue("MasterVolume", MasterVolume);

            root.AddValue("TerrainEnabled", TerrainEnabled);
            root.AddValue("GearEnabled", GearEnabled);
            root.AddValue("FuelEnabled", FuelEnabled);
            root.AddValue("PowerEnabled", PowerEnabled);
            root.AddValue("OverheatEnabled", OverheatEnabled);
            root.AddValue("StallEnabled", StallEnabled);
            root.AddValue("HighGEnabled", HighGEnabled);
            root.AddValue("CommsEnabled", CommsEnabled);

            root.AddValue("TerrainWarningTime", TerrainWarningTime);
            root.AddValue("TerrainWarningAltitude", TerrainWarningAltitude);
            root.AddValue("TerrainCautionAltitude", TerrainCautionAltitude);
            root.AddValue("TerrainMinDescentRate", TerrainMinDescentRate);
            root.AddValue("GearWarningAltitude", GearWarningAltitude);
            root.AddValue("GearWarningSpeed", GearWarningSpeed);
            root.AddValue("FuelWarningPercent", FuelWarningPercent);
            root.AddValue("FuelCautionPercent", FuelCautionPercent);
            root.AddValue("PowerWarningPercent", PowerWarningPercent);
            root.AddValue("PowerCautionPercent", PowerCautionPercent);
            root.AddValue("OverheatWarningPercent", OverheatWarningPercent);
            root.AddValue("StallMarginPercent", StallMarginPercent);
            root.AddValue("HighGWarning", HighGWarning);
            root.AddValue("HighGCaution", HighGCaution);

            root.AddValue("LandingCalloutsEnabled", LandingCalloutsEnabled);
            root.AddValue("LandingModeMaxDescentRate", LandingModeMaxDescentRate);
            root.AddValue("LandingModeMaxSpeed", LandingModeMaxSpeed);

            root.AddValue("ScreenFlashEnabled", ScreenFlashEnabled);
            root.AddValue("FlashIntensity", FlashIntensity);
            root.AddValue("ToggleKey", ToggleKey.ToString());
            root.AddValue("DisplayX", DisplayX);
            root.AddValue("DisplayY", DisplayY);

            root.Save(ConfigPath);
        }

        public void Load()
        {
            if (!System.IO.File.Exists(ConfigPath)) return;

            ConfigNode root = ConfigNode.Load(ConfigPath);
            if (root == null) return;

            bool.TryParse(root.GetValue("Enabled"), out Enabled);
            bool.TryParse(root.GetValue("AudioEnabled"), out AudioEnabled);
            float.TryParse(root.GetValue("MasterVolume"), out MasterVolume);

            bool.TryParse(root.GetValue("TerrainEnabled"), out TerrainEnabled);
            bool.TryParse(root.GetValue("GearEnabled"), out GearEnabled);
            bool.TryParse(root.GetValue("FuelEnabled"), out FuelEnabled);
            bool.TryParse(root.GetValue("PowerEnabled"), out PowerEnabled);
            bool.TryParse(root.GetValue("OverheatEnabled"), out OverheatEnabled);
            bool.TryParse(root.GetValue("StallEnabled"), out StallEnabled);
            bool.TryParse(root.GetValue("HighGEnabled"), out HighGEnabled);
            bool.TryParse(root.GetValue("CommsEnabled"), out CommsEnabled);

            float.TryParse(root.GetValue("TerrainWarningTime"), out TerrainWarningTime);
            float.TryParse(root.GetValue("TerrainWarningAltitude"), out TerrainWarningAltitude);
            float.TryParse(root.GetValue("TerrainCautionAltitude"), out TerrainCautionAltitude);
            float.TryParse(root.GetValue("TerrainMinDescentRate"), out TerrainMinDescentRate);
            float.TryParse(root.GetValue("GearWarningAltitude"), out GearWarningAltitude);
            float.TryParse(root.GetValue("GearWarningSpeed"), out GearWarningSpeed);
            float.TryParse(root.GetValue("FuelWarningPercent"), out FuelWarningPercent);
            float.TryParse(root.GetValue("FuelCautionPercent"), out FuelCautionPercent);
            float.TryParse(root.GetValue("PowerWarningPercent"), out PowerWarningPercent);
            float.TryParse(root.GetValue("PowerCautionPercent"), out PowerCautionPercent);
            float.TryParse(root.GetValue("OverheatWarningPercent"), out OverheatWarningPercent);
            float.TryParse(root.GetValue("StallMarginPercent"), out StallMarginPercent);
            float.TryParse(root.GetValue("HighGWarning"), out HighGWarning);
            float.TryParse(root.GetValue("HighGCaution"), out HighGCaution);

            bool.TryParse(root.GetValue("LandingCalloutsEnabled"), out LandingCalloutsEnabled);
            float.TryParse(root.GetValue("LandingModeMaxDescentRate"), out LandingModeMaxDescentRate);
            float.TryParse(root.GetValue("LandingModeMaxSpeed"), out LandingModeMaxSpeed);

            bool.TryParse(root.GetValue("ScreenFlashEnabled"), out ScreenFlashEnabled);
            float.TryParse(root.GetValue("FlashIntensity"), out FlashIntensity);
            float.TryParse(root.GetValue("DisplayX"), out DisplayX);
            float.TryParse(root.GetValue("DisplayY"), out DisplayY);

            // Load toggle key
            string toggleKeyStr = root.GetValue("ToggleKey");
            if (!string.IsNullOrEmpty(toggleKeyStr))
            {
                try
                {
                    ToggleKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), toggleKeyStr);
                }
                catch
                {
                    ToggleKey = KeyCode.F12;
                }
            }
        }
    }
}
