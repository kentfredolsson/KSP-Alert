using UnityEngine;
using KSP.UI.Screens;

namespace KSPAlert
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AlertToolbar : MonoBehaviour
    {
        private ApplicationLauncherButton toolbarButton;
        private Texture2D buttonTexture;
        private bool buttonAdded = false;

        void Start()
        {
            CreateButtonTexture();
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);

            if (ApplicationLauncher.Ready)
            {
                OnGUIAppLauncherReady();
            }
        }

        void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGUIAppLauncherDestroyed);
            RemoveButton();
        }

        private void CreateButtonTexture()
        {
            // Create a 38x38 texture for the toolbar button
            // GPWS-style logo: brown mountain, black aircraft, red exclamation
            int size = 38;
            buttonTexture = new Texture2D(size, size, TextureFormat.ARGB32, false);

            Color transparent = new Color(0, 0, 0, 0);
            Color brown = new Color(0.55f, 0.27f, 0.07f, 1f);      // Mountain brown
            Color darkBrown = new Color(0.45f, 0.22f, 0.05f, 1f);  // Mountain shadow
            Color black = new Color(0.05f, 0.05f, 0.05f, 1f);      // Aircraft
            Color red = new Color(0.85f, 0.1f, 0.1f, 1f);          // Exclamation
            Color white = new Color(1f, 1f, 1f, 1f);               // Outline/highlights

            // Clear to transparent
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    buttonTexture.SetPixel(x, y, transparent);
                }
            }

            // Draw mountain range (background) - main peak
            DrawFilledTriangle(buttonTexture, 10, 6, 28, 6, 19, 32, brown);
            // Secondary peak (left)
            DrawFilledTriangle(buttonTexture, 2, 10, 16, 10, 9, 26, darkBrown);
            // Snow caps (white highlights)
            DrawFilledTriangle(buttonTexture, 17, 28, 21, 28, 19, 32, white);
            DrawFilledTriangle(buttonTexture, 7, 22, 11, 22, 9, 26, white);

            // Draw aircraft silhouette (black) - side view flying left
            DrawAircraftSilhouette(buttonTexture, black);

            // Draw exclamation mark (red with white outline) in center
            DrawExclamationMark(buttonTexture, red, white);

            buttonTexture.Apply();
        }

        private void DrawFilledTriangle(Texture2D tex, int x1, int y1, int x2, int y2, int x3, int y3, Color color)
        {
            // Sort vertices by y
            if (y1 > y2) { Swap(ref x1, ref x2); Swap(ref y1, ref y2); }
            if (y1 > y3) { Swap(ref x1, ref x3); Swap(ref y1, ref y3); }
            if (y2 > y3) { Swap(ref x2, ref x3); Swap(ref y2, ref y3); }

            for (int y = y1; y <= y3; y++)
            {
                if (y < 0 || y >= tex.height) continue;

                float xa, xb;
                if (y < y2)
                {
                    xa = Interpolate(x1, y1, x3, y3, y);
                    xb = Interpolate(x1, y1, x2, y2, y);
                }
                else
                {
                    xa = Interpolate(x1, y1, x3, y3, y);
                    xb = Interpolate(x2, y2, x3, y3, y);
                }

                if (xa > xb) { float t = xa; xa = xb; xb = t; }

                for (int x = Mathf.Max(0, (int)xa); x <= Mathf.Min(tex.width - 1, (int)xb); x++)
                {
                    tex.SetPixel(x, y, color);
                }
            }
        }

        private void Swap(ref int a, ref int b) { int t = a; a = b; b = t; }

        private float Interpolate(int x1, int y1, int x2, int y2, int y)
        {
            if (y2 == y1) return x1;
            return x1 + (float)(x2 - x1) * (y - y1) / (y2 - y1);
        }

        private void DrawAircraftSilhouette(Texture2D tex, Color color)
        {
            // Aircraft silhouette - simplified fighter jet shape flying toward mountain
            // Fuselage (angled down toward ground)
            for (int i = 0; i < 18; i++)
            {
                int x = 4 + i;
                int y = 16 - i / 4;
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (y + dy >= 0 && y + dy < tex.height && x >= 0 && x < tex.width)
                        tex.SetPixel(x, y + dy, color);
                }
            }

            // Wings (swept back)
            for (int i = 0; i < 8; i++)
            {
                int baseX = 10 + i;
                int baseY = 14 - i / 4;
                // Upper wing
                for (int dy = 2; dy <= 6 - i / 2; dy++)
                {
                    if (baseY + dy < tex.height && baseX < tex.width)
                        tex.SetPixel(baseX, baseY + dy, color);
                }
                // Lower wing
                for (int dy = -2; dy >= -5 + i / 2; dy--)
                {
                    if (baseY + dy >= 0 && baseX < tex.width)
                        tex.SetPixel(baseX, baseY + dy, color);
                }
            }

            // Tail fin
            for (int i = 0; i < 5; i++)
            {
                int x = 4 + i;
                int y = 16 + 1 + i;
                if (y < tex.height && x < tex.width)
                    tex.SetPixel(x, y, color);
                if (y + 1 < tex.height && x < tex.width)
                    tex.SetPixel(x, y + 1, color);
            }

            // Nose
            tex.SetPixel(22, 12, color);
            tex.SetPixel(23, 12, color);
        }

        private void DrawExclamationMark(Texture2D tex, Color fillColor, Color outlineColor)
        {
            int cx = 26;  // Center X of exclamation

            // White outline first
            for (int y = 14; y <= 32; y++)
            {
                for (int x = cx - 3; x <= cx + 3; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        // Tapered shape - wider at top
                        int halfWidth = (y > 26) ? 2 : 3;
                        if (Mathf.Abs(x - cx) <= halfWidth)
                            tex.SetPixel(x, y, outlineColor);
                    }
                }
            }

            // Red fill (slightly smaller)
            for (int y = 15; y <= 31; y++)
            {
                for (int x = cx - 2; x <= cx + 2; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        int halfWidth = (y > 26) ? 1 : 2;
                        if (Mathf.Abs(x - cx) <= halfWidth)
                            tex.SetPixel(x, y, fillColor);
                    }
                }
            }

            // Dot outline (white circle)
            for (int dy = -3; dy <= 3; dy++)
            {
                for (int dx = -3; dx <= 3; dx++)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= 3f)
                    {
                        int x = cx + dx;
                        int y = 9 + dy;
                        if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                            tex.SetPixel(x, y, outlineColor);
                    }
                }
            }

            // Dot fill (red circle)
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= 2f)
                    {
                        int x = cx + dx;
                        int y = 9 + dy;
                        if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                            tex.SetPixel(x, y, fillColor);
                    }
                }
            }
        }

        private void OnGUIAppLauncherReady()
        {
            if (buttonAdded) return;

            toolbarButton = ApplicationLauncher.Instance.AddModApplication(
                OnButtonTrue,      // onTrue
                OnButtonFalse,     // onFalse
                null,              // onHover
                null,              // onHoverOut
                null,              // onEnable
                null,              // onDisable
                ApplicationLauncher.AppScenes.FLIGHT,
                buttonTexture
            );

            buttonAdded = true;
            Debug.Log("[KSP-Alert] Toolbar button added");
        }

        private void OnGUIAppLauncherDestroyed()
        {
            RemoveButton();
        }

        private void RemoveButton()
        {
            if (toolbarButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(toolbarButton);
                toolbarButton = null;
                buttonAdded = false;
            }
        }

        private void OnButtonTrue()
        {
            if (AlertMainWindow.Instance != null)
            {
                AlertMainWindow.Instance.ShowWindow();
            }
        }

        private void OnButtonFalse()
        {
            if (AlertMainWindow.Instance != null)
            {
                AlertMainWindow.Instance.HideWindow();
            }
        }
    }
}
