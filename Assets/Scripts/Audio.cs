using System.Collections.Generic;
using UnityEngine;

namespace EiraGame
{
    /// <summary>Efectos de sonido generados proceduralmente (sin assets externos).</summary>
    public static class Sfx
    {
        private static AudioSource source;
        private static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        public static void Init()
        {
            if (source != null) return;
            var go = new GameObject("Sfx");
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 0.55f;

            AddTone("jump", 720, 980, 0.09f);
            AddTone("pulse", 950, 1450, 0.14f);
            AddTone("hit", 220, 120, 0.16f);
            AddTone("heart", 140, 90, 0.28f);
            AddTone("pickup", 1180, 1560, 0.10f);
            AddTone("chip", 1320, 880, 0.12f);
            AddTone("beep", 620, 620, 0.05f);
            AddTone("door", 300, 420, 0.35f);
            AddTone("boom", 120, 60, 0.5f);
            AddTone("boss", 150, 60, 0.9f);
            AddTone("victory", 523, 1046, 0.9f);
            AddTone("lose", 130, 55, 0.8f);
            AddTone("error", 100, 80, 0.35f);
        }

        private static void AddTone(string name, float f0, float f1, float dur)
        {
            int sr = 44100;
            int n = (int)(dur * sr);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr;
                float f = Mathf.Lerp(f0, f1, t / dur);
                float env = 1f - (float)i / n;
                data[i] = Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.75f;
            }
            var c = AudioClip.Create(name, n, 1, sr, false);
            c.SetData(data, 0);
            clips[name] = c;
        }

        public static void Play(string name)
        {
            if (source == null) return;
            if (clips.TryGetValue(name, out var c)) source.PlayOneShot(c);
        }

        public static void Shutdown()
        {
            if (source != null)
            {
                Object.Destroy(source.gameObject);
                source = null;
            }
            clips.Clear();
        }
    }
}