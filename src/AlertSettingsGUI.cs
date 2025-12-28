using UnityEngine;

namespace KSPAlert
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AlertSettingsGUI : MonoBehaviour
    {
        private bool showWindow = false;
        private Rect windowRect = new Rect(100, 100, 400, 500);
        private Vector2 scrollPosition = Vector2.zero;

        private GUIStyle headerStyle;
        private GUIStyle sectionStyle;
        private GUIStyle toggleStyle;
        private bool stylesInitialized = false;

        private const KeyCode TOGGLE_KEY = KeyCode.F10;
        private int windowId;

        void Awake()
        {
            windowId = GetInstanceID();
        }

        void Update()
        {
            if (Input.GetKeyDown(TOGGLE_KEY))
            {
                showWindow = !showWindow;
            }
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = 12
            };

            stylesInitialized = true;
        }

        void OnGUI()
        {
            if (!showWindow) return;
            if (!HighLogic.LoadedSceneIsFlight) return;

            InitializeStyles();

            windowRect = GUILayout.Window(
                windowId,
                windowRect,
                DrawWindow,
                "KSP-Alert Settings",
                GUILayout.Width(400),
                GUILayout.Height(500)
            );
        }

        private void DrawWindow(int id)
        {
            var config = AlertManager.Instance?.Config;
            if (config == null)
            {
                GUILayout.Label("Alert system not initialized");
                return;
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            // Master controls
            GUILayout.Label("Master Controls", sectionStyle);
            GUILayout.BeginHorizontal();
            config.Enabled = GUILayout.Toggle(config.Enabled, "Alerts Enabled", toggleStyle);
            config.AudioEnabled = GUILayout.Toggle(config.AudioEnabled, "Audio Enabled", toggleStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Volume:", GUILayout.Width(60));
            config.MasterVolume = GUILayout.HorizontalSlider(config.MasterVolume, 0f, 1f, GUILayout.Width(150));
            GUILayout.Label($"{config.MasterVolume:P0}", GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            // Alert toggles
            GUILayout.Label("Alert Types", sectionStyle);
            config.TerrainEnabled = GUILayout.Toggle(config.TerrainEnabled, "Terrain Proximity (GPWS)", toggleStyle);
            config.GearEnabled = GUILayout.Toggle(config.GearEnabled, "Landing Gear Warning", toggleStyle);
            config.FuelEnabled = GUILayout.Toggle(config.FuelEnabled, "Low Fuel Warning", toggleStyle);
            config.PowerEnabled = GUILayout.Toggle(config.PowerEnabled, "Low Power Warning", toggleStyle);
            config.OverheatEnabled = GUILayout.Toggle(config.OverheatEnabled, "Overheat Warning", toggleStyle);
            config.HighGEnabled = GUILayout.Toggle(config.HighGEnabled, "High G-Force Warning", toggleStyle);
            config.CommsEnabled = GUILayout.Toggle(config.CommsEnabled, "Communications Lost", toggleStyle);

            GUILayout.Space(15);

            // Terrain settings
            GUILayout.Label("Terrain Warning", sectionStyle);
            DrawSlider("Warning Altitude (m):", ref config.TerrainWarningAltitude, 100f, 2000f);
            DrawSlider("Min Descent Rate (m/s):", ref config.TerrainMinDescentRate, -200f, -10f);

            GUILayout.Space(10);

            // Gear settings
            GUILayout.Label("Gear Warning", sectionStyle);
            DrawSlider("Trigger Altitude (m):", ref config.GearWarningAltitude, 50f, 1000f);
            DrawSlider("Max Speed (m/s):", ref config.GearWarningSpeed, 50f, 300f);

            GUILayout.Space(10);

            // Resource settings
            GUILayout.Label("Resource Warnings", sectionStyle);
            DrawSlider("Fuel Warning (%):", ref config.FuelWarningPercent, 1f, 50f);
            DrawSlider("Power Warning (%):", ref config.PowerWarningPercent, 1f, 50f);

            GUILayout.Space(10);

            // Other thresholds
            GUILayout.Label("Other Thresholds", sectionStyle);
            DrawSlider("Overheat Threshold (%):", ref config.OverheatWarningPercent, 50f, 99f);
            DrawSlider("G-Force Warning:", ref config.HighGWarning, 3f, 15f);

            GUILayout.Space(10);

            // Visual settings
            GUILayout.Label("Visual Settings", sectionStyle);
            config.ScreenFlashEnabled = GUILayout.Toggle(config.ScreenFlashEnabled, "Screen Flash Effect", toggleStyle);
            DrawSlider("Flash Intensity:", ref config.FlashIntensity, 0.1f, 0.5f);

            GUILayout.Space(15);

            // Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Settings"))
            {
                config.Save();
                ScreenMessages.PostScreenMessage("KSP-Alert settings saved", 2f);
            }
            if (GUILayout.Button("Reset Defaults"))
            {
                ResetToDefaults(config);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Test Warning Sound"))
            {
                AlertManager.Instance?.TriggerAlert(AlertType.Terrain);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);
            GUILayout.Label("Press F10 to toggle this window", GUI.skin.label);

            GUI.DragWindow();
        }

        private void DrawSlider(string label, ref float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(150));
            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(150));
            GUILayout.Label($"{value:F1}", GUILayout.Width(60));
            GUILayout.EndHorizontal();
        }

        private void ResetToDefaults(AlertConfig config)
        {
            var defaults = new AlertConfig();

            config.Enabled = defaults.Enabled;
            config.AudioEnabled = defaults.AudioEnabled;
            config.MasterVolume = defaults.MasterVolume;

            config.TerrainEnabled = defaults.TerrainEnabled;
            config.GearEnabled = defaults.GearEnabled;
            config.FuelEnabled = defaults.FuelEnabled;
            config.PowerEnabled = defaults.PowerEnabled;
            config.OverheatEnabled = defaults.OverheatEnabled;
            config.HighGEnabled = defaults.HighGEnabled;
            config.CommsEnabled = defaults.CommsEnabled;

            config.TerrainWarningAltitude = defaults.TerrainWarningAltitude;
            config.TerrainMinDescentRate = defaults.TerrainMinDescentRate;
            config.GearWarningAltitude = defaults.GearWarningAltitude;
            config.GearWarningSpeed = defaults.GearWarningSpeed;
            config.FuelWarningPercent = defaults.FuelWarningPercent;
            config.PowerWarningPercent = defaults.PowerWarningPercent;
            config.OverheatWarningPercent = defaults.OverheatWarningPercent;
            config.HighGWarning = defaults.HighGWarning;

            config.ScreenFlashEnabled = defaults.ScreenFlashEnabled;
            config.FlashIntensity = defaults.FlashIntensity;

            ScreenMessages.PostScreenMessage("Settings reset to defaults", 2f);
        }
    }
}
