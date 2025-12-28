using System.Collections.Generic;
using UnityEngine;

namespace KSPAlert
{
    public class AlertDisplay : MonoBehaviour
    {
        private List<Alert> currentAlerts = new List<Alert>();
        private GUIStyle warningStyle;
        private GUIStyle cautionStyle;
        private GUIStyle advisoryStyle;
        private GUIStyle boxStyle;

        private Texture2D warningBg;
        private Texture2D cautionBg;
        private Texture2D advisoryBg;
        private Texture2D flashTexture;

        private float flashPhase = 0f;
        private bool stylesInitialized = false;

        private const float FLASH_SPEED = 4f;
        private const float BOX_WIDTH = 200f;
        private const float BOX_HEIGHT = 50f;
        private const float SPACING = 5f;

        void Awake()
        {
            CreateTextures();
        }

        private void CreateTextures()
        {
            // Warning - Red
            warningBg = new Texture2D(1, 1);
            warningBg.SetPixel(0, 0, new Color(0.8f, 0.1f, 0.1f, 0.9f));
            warningBg.Apply();

            // Caution - Amber
            cautionBg = new Texture2D(1, 1);
            cautionBg.SetPixel(0, 0, new Color(0.9f, 0.6f, 0.1f, 0.9f));
            cautionBg.Apply();

            // Advisory - Blue
            advisoryBg = new Texture2D(1, 1);
            advisoryBg.SetPixel(0, 0, new Color(0.1f, 0.4f, 0.8f, 0.9f));
            advisoryBg.Apply();

            // Screen flash overlay
            flashTexture = new Texture2D(1, 1);
            flashTexture.SetPixel(0, 0, new Color(1f, 0f, 0f, 0.3f));
            flashTexture.Apply();
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            warningStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white, background = warningBg }
            };

            cautionStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.black, background = cautionBg }
            };

            advisoryStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white, background = advisoryBg }
            };

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 5, 5)
            };

            stylesInitialized = true;
        }

        public void UpdateAlerts(List<Alert> alerts)
        {
            currentAlerts = new List<Alert>(alerts);
        }

        void Update()
        {
            flashPhase += Time.deltaTime * FLASH_SPEED;
            if (flashPhase > Mathf.PI * 2) flashPhase -= Mathf.PI * 2;
        }

        void OnGUI()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;
            if (currentAlerts.Count == 0) return;

            var config = AlertManager.Instance?.Config;
            if (config == null || !config.Enabled) return;

            InitializeStyles();

            // Draw screen flash for warnings
            if (config.ScreenFlashEnabled && HasWarningLevel())
            {
                DrawScreenFlash(config.FlashIntensity);
            }

            // Draw alert boxes
            float startX = Screen.width * config.DisplayX - BOX_WIDTH / 2;
            float startY = Screen.height * config.DisplayY;

            for (int i = 0; i < currentAlerts.Count && i < 5; i++)
            {
                var alert = currentAlerts[i];
                float y = startY + i * (BOX_HEIGHT + SPACING);

                DrawAlertBox(alert, startX, y);
            }
        }

        private bool HasWarningLevel()
        {
            foreach (var alert in currentAlerts)
            {
                if (alert.Priority == AlertPriority.Warning)
                    return true;
            }
            return false;
        }

        private void DrawScreenFlash(float intensity)
        {
            float alpha = (Mathf.Sin(flashPhase) * 0.5f + 0.5f) * intensity;

            Color flashColor = new Color(1f, 0f, 0f, alpha);
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, flashColor);
            tex.Apply();

            // Draw border flash
            float border = 50f;

            // Top
            GUI.DrawTexture(new Rect(0, 0, Screen.width, border), tex);
            // Bottom
            GUI.DrawTexture(new Rect(0, Screen.height - border, Screen.width, border), tex);
            // Left
            GUI.DrawTexture(new Rect(0, 0, border, Screen.height), tex);
            // Right
            GUI.DrawTexture(new Rect(Screen.width - border, 0, border, Screen.height), tex);

            Destroy(tex);
        }

        private void DrawAlertBox(Alert alert, float x, float y)
        {
            GUIStyle style = GetStyleForPriority(alert.Priority);

            // Flash effect for warnings
            if (alert.Priority == AlertPriority.Warning)
            {
                float flash = Mathf.Sin(flashPhase) * 0.5f + 0.5f;
                Color bgColor = new Color(
                    Mathf.Lerp(0.5f, 1f, flash),
                    0.1f,
                    0.1f,
                    0.95f
                );
                Texture2D flashBg = new Texture2D(1, 1);
                flashBg.SetPixel(0, 0, bgColor);
                flashBg.Apply();
                style.normal.background = flashBg;
            }

            Rect boxRect = new Rect(x, y, BOX_WIDTH, BOX_HEIGHT);

            // Draw shadow
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.Box(new Rect(x + 2, y + 2, BOX_WIDTH, BOX_HEIGHT), "", boxStyle);
            GUI.color = Color.white;

            // Draw main box
            GUI.Box(boxRect, alert.Message, style);

            // Draw border
            DrawBorder(boxRect, GetBorderColor(alert.Priority), 2);
        }

        private GUIStyle GetStyleForPriority(AlertPriority priority)
        {
            switch (priority)
            {
                case AlertPriority.Warning:
                    return warningStyle;
                case AlertPriority.Caution:
                    return cautionStyle;
                case AlertPriority.Advisory:
                    return advisoryStyle;
                default:
                    return advisoryStyle;
            }
        }

        private Color GetBorderColor(AlertPriority priority)
        {
            switch (priority)
            {
                case AlertPriority.Warning:
                    return new Color(1f, 0.3f, 0.3f, 1f);
                case AlertPriority.Caution:
                    return new Color(1f, 0.8f, 0.3f, 1f);
                case AlertPriority.Advisory:
                    return new Color(0.3f, 0.6f, 1f, 1f);
                default:
                    return Color.white;
            }
        }

        private void DrawBorder(Rect rect, Color color, int thickness)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();

            // Top
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), tex);
            // Bottom
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), tex);
            // Left
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), tex);
            // Right
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), tex);

            Destroy(tex);
        }
    }
}
