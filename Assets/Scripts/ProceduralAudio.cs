using UnityEngine;

// Sounds made in code (no files) - same idea as the phone's buzz/ring. Placeholders until there are
// real recordings: swap a clip in by giving Ambience / Classmate an AudioClip instead.
public static class ProceduralAudio
{
    const int Rate = 44100;

    // Whispering: noise shaped into syllables, each with its own "vowel" (band-pass centre), short
    // gaps between words. Length = seconds.
    public static AudioClip Whisper(float seconds, int seed)
    {
        var rng = new System.Random(seed);
        int n = Mathf.Max(1, (int)(seconds * Rate));
        float[] data = new float[n];

        // State-variable band-pass filter
        float low = 0f, band = 0f;
        int i = 0;
        while (i < n)
        {
            bool gap = rng.NextDouble() < 0.22;
            int len = (int)(Rate * (gap ? 0.06 + rng.NextDouble() * 0.12 : 0.11 + rng.NextDouble() * 0.16));
            float centre = 1300f + (float)rng.NextDouble() * 2600f;
            float f = 2f * Mathf.Sin(Mathf.PI * centre / Rate);
            float q = 0.55f;
            float peak = gap ? 0f : 0.55f + (float)rng.NextDouble() * 0.45f;
            bool hiss = !gap && rng.NextDouble() < 0.25;   // an "s"/"sh" now and then
            for (int k = 0; k < len && i < n; k++, i++)
            {
                float x = (float)(rng.NextDouble() * 2.0 - 1.0);
                low += f * band;
                float high = x - low - q * band;
                band += f * high;
                float env = Mathf.Sin(Mathf.PI * k / len);
                env *= env;
                data[i] = (hiss ? high * 0.35f : band) * env * peak;
            }
        }

        Normalize(data, 0.8f);
        FadeEdges(data, 0.02f);
        var clip = AudioClip.Create("Whisper", n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Quiet room: ventilation rumble + air + a faint mains hum. Loops seamlessly.
    public static AudioClip RoomTone(float seconds = 12f)
    {
        var rng = new System.Random(7);
        float[] data = MakeLoop(seconds, out int n);
        float brown = 0f, lp = 0f, air = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float w = (float)(rng.NextDouble() * 2.0 - 1.0);
            brown = brown * 0.995f + w * 0.03f;           // deep rumble
            lp += (w - lp) * 0.08f;                       // soft air
            air += (lp - air) * 0.5f;
            float t = (float)i / Rate;
            float hum = Mathf.Sin(2f * Mathf.PI * 50f * t) * 0.04f + Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.02f;
            data[i] = brown * 1.2f + air * 0.25f + hum;
        }
        return FinishLoop(data, n, "RoomTone");
    }

    // Distant city: low traffic rumble with cars swelling past every few seconds, a little wind hiss.
    public static AudioClip CityTraffic(float seconds = 24f)
    {
        var rng = new System.Random(11);
        float[] data = MakeLoop(seconds, out int n);

        // Car pass-bys: smooth bumps in loudness at random times
        int cars = Mathf.RoundToInt(seconds / 4f);
        float[] carAt = new float[cars], carLen = new float[cars], carGain = new float[cars];
        for (int c = 0; c < cars; c++)
        {
            carAt[c] = (float)rng.NextDouble() * seconds;
            carLen[c] = 2f + (float)rng.NextDouble() * 3f;
            carGain[c] = 0.5f + (float)rng.NextDouble();
        }

        float brown = 0f, lp = 0f, hiss = 0f, swell = 0.35f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / Rate;
            float w = (float)(rng.NextDouble() * 2.0 - 1.0);
            brown = brown * 0.997f + w * 0.025f;
            lp += (w - lp) * 0.02f;
            hiss += (w - hiss) * 0.3f;

            // Loudness changes slowly - only worked out every 128 samples (keeps generation fast in WebGL)
            if ((i & 127) == 0)
            {
                swell = 0.35f;
                for (int c = 0; c < cars; c++)
                {
                    float d = Mathf.Abs(t - carAt[c]);
                    d = Mathf.Min(d, seconds - d);          // wraps around so the loop stays seamless
                    swell += carGain[c] * Mathf.Exp(-(d * d) / (carLen[c] * 0.25f));
                }
            }
            data[i] = (brown * 1.5f + lp * 0.6f) * swell + hiss * 0.02f;
        }
        return FinishLoop(data, n, "CityTraffic");
    }

    // --- helpers ---

    // Makes the buffer a bit longer than the loop so the tail can be cross-faded into the start.
    static float[] MakeLoop(float seconds, out int n)
    {
        n = (int)(seconds * Rate);
        return new float[n + Rate];   // +1 s for the cross-fade
    }

    static AudioClip FinishLoop(float[] data, int n, string name)
    {
        int fade = data.Length - n;
        float[] loop = new float[n];
        System.Array.Copy(data, loop, n);
        for (int k = 0; k < fade; k++)
        {
            float g = (float)k / fade;
            loop[k] = loop[k] * g + data[n + k] * (1f - g);
        }
        RemoveDc(loop);
        Normalize(loop, 0.7f);
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(loop, 0);
        return clip;
    }

    static void RemoveDc(float[] d)
    {
        double sum = 0; foreach (float v in d) sum += v;
        float mean = (float)(sum / d.Length);
        for (int i = 0; i < d.Length; i++) d[i] -= mean;
    }

    static void Normalize(float[] d, float to)
    {
        float peak = 0.0001f;
        foreach (float v in d) peak = Mathf.Max(peak, Mathf.Abs(v));
        float g = to / peak;
        for (int i = 0; i < d.Length; i++) d[i] *= g;
    }

    static void FadeEdges(float[] d, float seconds)
    {
        int f = Mathf.Min(d.Length / 2, (int)(seconds * Rate));
        for (int k = 0; k < f; k++)
        {
            float g = (float)k / f;
            d[k] *= g;
            d[d.Length - 1 - k] *= g;
        }
    }
}
