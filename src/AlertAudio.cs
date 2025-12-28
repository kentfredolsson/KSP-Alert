using System.Collections.Generic;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace KSPAlert
{
    public class AlertAudio : MonoBehaviour
    {
        private AudioSource audioSource;
        private Dictionary<AlertPriority, AudioClip> toneClips;
        private Dictionary<AlertType, AudioClip> customClips;

        private float lastPlayTime;
        private const float MIN_PLAY_INTERVAL = 0.5f;

        private static readonly string SoundsPath =
            KSPUtil.ApplicationRootPath + "GameData/KSP-Alert/Sounds/";

        void Start()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f; // 2D sound

            customClips = new Dictionary<AlertType, AudioClip>();

            GenerateTones();
            StartCoroutine(LoadCustomSounds());

            Debug.Log("[KSP-Alert] Audio system initialized");
        }

        private IEnumerator LoadCustomSounds()
        {
            // Create sounds directory if it doesn't exist
            if (!Directory.Exists(SoundsPath))
            {
                Directory.CreateDirectory(SoundsPath);
                Debug.Log($"[KSP-Alert] Created sounds directory: {SoundsPath}");
            }

            // Try to load custom terrain warning sound
            yield return StartCoroutine(LoadSound("terrain.wav", AlertType.Terrain));
            yield return StartCoroutine(LoadSound("gear.wav", AlertType.GearUp));
            yield return StartCoroutine(LoadSound("fuel.wav", AlertType.LowFuel));
            yield return StartCoroutine(LoadSound("power.wav", AlertType.LowPower));
            yield return StartCoroutine(LoadSound("overheat.wav", AlertType.Overheat));
            yield return StartCoroutine(LoadSound("stall.wav", AlertType.Stall));
            yield return StartCoroutine(LoadSound("gforce.wav", AlertType.HighG));
            yield return StartCoroutine(LoadSound("comms.wav", AlertType.CommsLost));

            // Radio altitude callouts
            yield return StartCoroutine(LoadSound("50.wav", AlertType.Altitude50));
            yield return StartCoroutine(LoadSound("40.wav", AlertType.Altitude40));
            yield return StartCoroutine(LoadSound("30.wav", AlertType.Altitude30));
            yield return StartCoroutine(LoadSound("20.wav", AlertType.Altitude20));
            yield return StartCoroutine(LoadSound("10.wav", AlertType.Altitude10));
            yield return StartCoroutine(LoadSound("5.wav", AlertType.Altitude5));
            yield return StartCoroutine(LoadSound("retard.wav", AlertType.Retard));
        }

        private IEnumerator LoadSound(string filename, AlertType alertType)
        {
            string filePath = SoundsPath + filename;

            if (!File.Exists(filePath))
            {
                // Don't log for altitude callouts since we have generated fallbacks
                if (!IsAltitudeCallout(alertType))
                {
                    Debug.Log($"[KSP-Alert] No custom sound file: {filename}");
                }
                yield break;
            }

            string uri = "file://" + filePath;
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV))
            {
                yield return www.SendWebRequest();

                // Use older API compatible with KSP's Unity version
                if (string.IsNullOrEmpty(www.error))
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    if (clip != null)
                    {
                        customClips[alertType] = clip;  // Overrides generated fallback
                        Debug.Log($"[KSP-Alert] Loaded custom sound: {filename}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[KSP-Alert] Failed to load {filename}: {www.error}");
                }
            }
        }

        private bool IsAltitudeCallout(AlertType type)
        {
            return type == AlertType.Altitude50 || type == AlertType.Altitude40 ||
                   type == AlertType.Altitude30 || type == AlertType.Altitude20 ||
                   type == AlertType.Altitude10 || type == AlertType.Altitude5 ||
                   type == AlertType.Retard;
        }

        private void GenerateTones()
        {
            toneClips = new Dictionary<AlertPriority, AudioClip>();

            // Warning: Fire bell alarm
            toneClips[AlertPriority.Warning] = GenerateWarningTone();

            // Caution: Single attention tone
            toneClips[AlertPriority.Caution] = GenerateCautionTone();

            // Advisory: Soft chime
            toneClips[AlertPriority.Advisory] = GenerateAdvisoryTone();

            // Generate altitude callout beeps as fallbacks
            // These will be used if no custom WAV files are provided
            GenerateAltitudeCallouts();
        }

        private void GenerateAltitudeCallouts()
        {
            // Generate distinct beep patterns for each altitude
            // Higher altitude = lower pitch, lower altitude = higher pitch
            customClips[AlertType.Altitude50] = GenerateAltitudeBeep(5, 600f);   // 5 beeps, low pitch
            customClips[AlertType.Altitude40] = GenerateAltitudeBeep(4, 700f);   // 4 beeps
            customClips[AlertType.Altitude30] = GenerateAltitudeBeep(3, 800f);   // 3 beeps
            customClips[AlertType.Altitude20] = GenerateAltitudeBeep(2, 900f);   // 2 beeps
            customClips[AlertType.Altitude10] = GenerateAltitudeBeep(1, 1000f);  // 1 beep, high pitch
            customClips[AlertType.Altitude5] = GenerateAltitudeBeep(1, 1200f);   // 1 higher beep
            customClips[AlertType.Retard] = GenerateRetardTone();                // Special retard tone
        }

        private AudioClip GenerateAltitudeBeep(int beepCount, float frequency)
        {
            int sampleRate = 44100;
            float beepDuration = 0.08f;
            float gapDuration = 0.06f;
            float totalDuration = beepCount * beepDuration + (beepCount - 1) * gapDuration + 0.05f;
            int samples = (int)(sampleRate * totalDuration);
            float[] data = new float[samples];

            for (int b = 0; b < beepCount; b++)
            {
                float beepStart = b * (beepDuration + gapDuration);
                int startSample = (int)(beepStart * sampleRate);
                int endSample = (int)((beepStart + beepDuration) * sampleRate);

                for (int i = startSample; i < endSample && i < samples; i++)
                {
                    float t = (float)(i - startSample) / sampleRate;
                    float localT = t / beepDuration;

                    // Soft envelope
                    float envelope = Mathf.Sin(Mathf.PI * localT);

                    data[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * 0.4f * envelope;
                }
            }

            AudioClip clip = AudioClip.Create($"Altitude{beepCount}", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateRetardTone()
        {
            // Two-tone "retard" warning: descending pitch pattern
            int sampleRate = 44100;
            float duration = 0.6f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            float freq1 = 800f;
            float freq2 = 600f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;

                // Two segments
                float envelope;
                float freq;

                if (t < 0.25f)
                {
                    envelope = Mathf.Sin(Mathf.PI * t / 0.25f);
                    freq = freq1;
                }
                else if (t < 0.35f)
                {
                    envelope = 0f; // gap
                    freq = 0f;
                }
                else if (t < 0.6f)
                {
                    float localT = t - 0.35f;
                    envelope = Mathf.Sin(Mathf.PI * localT / 0.25f);
                    freq = freq2;
                }
                else
                {
                    envelope = 0f;
                    freq = 0f;
                }

                if (freq > 0)
                {
                    data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.5f * envelope;
                }
            }

            AudioClip clip = AudioClip.Create("Retard", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateWarningTone()
        {
            // Fire bell alarm: rapid ding-ding-ding-ding pattern
            int sampleRate = 44100;
            float duration = 2.0f;  // Longer duration for multiple dings
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            // Bell parameters
            float bellFreq = 2400f;      // High pitched bell
            float bellFreq2 = 3000f;     // Second harmonic
            float bellFreq3 = 3600f;     // Third harmonic
            float dingInterval = 0.12f;  // Time between each ding (fast!)
            float dingDuration = 0.1f;   // Each ding length

            int numDings = (int)(duration / dingInterval);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = 0f;

                // Generate each ding
                for (int d = 0; d < numDings; d++)
                {
                    float dingStart = d * dingInterval;
                    float dingTime = t - dingStart;

                    if (dingTime >= 0 && dingTime < dingDuration)
                    {
                        // Sharp attack, fast decay (bell-like)
                        float attack = Mathf.Min(1f, dingTime / 0.002f);  // 2ms attack
                        float decay = Mathf.Exp(-dingTime * 40f);         // Fast decay
                        float envelope = attack * decay;

                        // Bell sound with harmonics
                        float bell = Mathf.Sin(2 * Mathf.PI * bellFreq * dingTime) * 0.5f;
                        bell += Mathf.Sin(2 * Mathf.PI * bellFreq2 * dingTime) * 0.3f;
                        bell += Mathf.Sin(2 * Mathf.PI * bellFreq3 * dingTime) * 0.15f;

                        // Add metallic shimmer
                        bell += Mathf.Sin(2 * Mathf.PI * (bellFreq * 2.4f) * dingTime) * 0.1f * envelope;

                        data[i] += bell * envelope * 0.6f;
                    }
                }

                // Clamp to prevent clipping
                data[i] = Mathf.Clamp(data[i], -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("WarningBell", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateCautionTone()
        {
            int sampleRate = 44100;
            float duration = 0.4f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            float freq = 880f; // A5

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;

                // Bell-like envelope
                float envelope = Mathf.Exp(-t * 5f);
                if (t < 0.01f) envelope *= t / 0.01f;

                // Main tone with harmonics
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.5f * envelope;
                data[i] += Mathf.Sin(2 * Mathf.PI * freq * 2 * t) * 0.25f * envelope;
                data[i] += Mathf.Sin(2 * Mathf.PI * freq * 3 * t) * 0.1f * envelope;
            }

            AudioClip clip = AudioClip.Create("CautionTone", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateAdvisoryTone()
        {
            int sampleRate = 44100;
            float duration = 0.25f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            float freq = 660f; // E5

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;

                // Soft envelope
                float envelope = Mathf.Sin(Mathf.PI * t / duration);

                data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.3f * envelope;
            }

            AudioClip clip = AudioClip.Create("AdvisoryTone", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public bool IsPlaying()
        {
            return audioSource != null && audioSource.isPlaying;
        }

        public void PlayAlert(Alert alert)
        {
            var config = AlertManager.Instance?.Config;
            if (config == null || !config.AudioEnabled) return;

            // Don't play if audio is already playing (prevents overlap)
            if (audioSource.isPlaying) return;

            // Prevent audio spam
            if (Time.time - lastPlayTime < MIN_PLAY_INTERVAL) return;
            lastPlayTime = Time.time;

            AudioClip clip = null;

            // First check for custom sound file for this alert type
            if (customClips.TryGetValue(alert.Type, out AudioClip customClip))
            {
                clip = customClip;
                Debug.Log($"[KSP-Alert] Playing custom sound for {alert.Type}");
            }
            // Fall back to generated tone based on priority
            else if (toneClips.TryGetValue(alert.Priority, out AudioClip priorityClip))
            {
                clip = priorityClip;
            }

            if (clip != null)
            {
                audioSource.volume = config.MasterVolume;
                audioSource.PlayOneShot(clip);
                Debug.Log($"[KSP-Alert] Playing {alert.Priority} alert: {alert.Message}");
            }
        }

        public void PlayTone(AlertPriority priority)
        {
            var config = AlertManager.Instance?.Config;
            if (config == null || !config.AudioEnabled) return;

            if (toneClips.TryGetValue(priority, out AudioClip clip))
            {
                audioSource.volume = config.MasterVolume;
                audioSource.PlayOneShot(clip);
            }
        }

        public void StopAll()
        {
            audioSource.Stop();
        }
    }
}
