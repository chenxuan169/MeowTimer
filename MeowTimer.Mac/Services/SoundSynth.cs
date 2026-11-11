using System;
using System.IO;

namespace MeowTimer.Services;

/// <summary>音效合成：直接从 Windows 版抽出来，纯数学，跨平台通用。</summary>
public static class SoundSynth
{
    public static byte[] Synth(string id, int volume)
    {
        var key = id.StartsWith("builtin:", StringComparison.OrdinalIgnoreCase)
            ? id.Substring(8).ToLowerInvariant()
            : id.ToLowerInvariant();

        return key switch
        {
            "chime" => Chime(volume),
            "bell" => Bell(volume),
            "marimba" => Marimba(volume),
            "drop" => Drop(volume),
            "notify" => Notify(volume),
            "soft" => Soft(volume),
            _ => Meow(volume)
        };
    }

    /// <summary>生成 16bit 单声道 44.1kHz 的 wav。</summary>
    private static byte[] Render(double duration, int volume, Func<double, double> gen)
    {
        const int rate = 44100;
        int n = (int)(rate * duration);
        double amp = Math.Clamp(volume, 0, 100) / 100.0 * 0.62;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        int dataSize = n * 2;
        bw.Write("RIFF"u8.ToArray());
        bw.Write(36 + dataSize);
        bw.Write("WAVE"u8.ToArray());
        bw.Write("fmt "u8.ToArray());
        bw.Write(16);
        bw.Write((short)1);        // PCM
        bw.Write((short)1);        // mono
        bw.Write(rate);
        bw.Write(rate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write("data"u8.ToArray());
        bw.Write(dataSize);

        for (int i = 0; i < n; i++)
        {
            double s = Math.Clamp(gen((double)i / rate) * amp, -1, 1);
            bw.Write((short)(s * 32767));
        }

        bw.Flush();
        return ms.ToArray();
    }

    private static byte[] Meow(int vol)
    {
        const double dur = 0.46;
        double phase = 0;
        return Render(dur, vol, t =>
        {
            double x = Math.Min(1, t / dur);
            double f = 505 + 430 * Math.Sin(Math.PI * Math.Pow(x, 0.82)) - 130 * x;
            f += 11 * Math.Sin(2 * Math.PI * 23 * t);
            phase += 2 * Math.PI * f / 44100.0;

            double env = Math.Pow(Math.Sin(Math.PI * Math.Min(1, x * 1.04)), 0.65) * Math.Min(1, t / 0.025);
            env *= Math.Max(0, 1 - Math.Pow(x, 6) * 0.5);

            return (Math.Sin(phase) * 0.70 + Math.Sin(phase * 2) * 0.20 + Math.Sin(phase * 3) * 0.07) * env;
        });
    }

    /// <summary>叮——咚，两个钟音。</summary>
    private static byte[] Chime(int vol)
    {
        const double dur = 1.25;
        double p1 = 0, p2 = 0;
        return Render(dur, vol, t =>
        {
            p1 += 2 * Math.PI * 1046.5 / 44100.0;
            double s = Math.Sin(p1) * Math.Exp(-6.2 * t) * Math.Min(1, t / 0.005);

            if (t >= 0.17)
            {
                double t2 = t - 0.17;
                p2 += 2 * Math.PI * 1318.5 / 44100.0;
                double e2 = Math.Exp(-4.4 * t2) * Math.Min(1, t2 / 0.005);
                s += (Math.Sin(p2) + Math.Sin(p2 * 2) * 0.22) * e2 * 0.92;
            }
            return s * 0.72;
        });
    }

    private static byte[] Bell(int vol)
    {
        const double dur = 1.7;
        double p = 0;
        return Render(dur, vol, t =>
        {
            p += 2 * Math.PI * 880 / 44100.0;
            double e = Math.Exp(-2.6 * t) * Math.Min(1, t / 0.004);
            return (Math.Sin(p) + Math.Sin(p * 2.01) * 0.45 + Math.Sin(p * 3.02) * 0.2) * e * 0.6;
        });
    }

    /// <summary>木琴三音上行。</summary>
    private static byte[] Marimba(int vol)
    {
        const double dur = 0.95;
        double[] freqs = { 523.25, 659.25, 783.99 };
        double[] p = new double[3];
        return Render(dur, vol, t =>
        {
            double s = 0;
            for (int k = 0; k < 3; k++)
            {
                double start = k * 0.135;
                if (t < start) continue;
                double tt = t - start;
                p[k] += 2 * Math.PI * freqs[k] / 44100.0;
                double e = Math.Exp(-8.5 * tt) * Math.Min(1, tt / 0.003);
                s += (Math.Sin(p[k]) + Math.Sin(p[k] * 4) * 0.22) * e;
            }
            return s * 0.5;
        });
    }

    private static byte[] Drop(int vol)
    {
        const double dur = 0.55;
        double p = 0;
        return Render(dur, vol, t =>
        {
            double f = 300 + 1200 * Math.Exp(-9 * t);
            p += 2 * Math.PI * f / 44100.0;
            double e = Math.Exp(-7 * t) * Math.Min(1, t / 0.003);
            return Math.Sin(p) * e * 0.85;
        });
    }

    /// <summary>三连短提示，最像系统提示音。</summary>
    private static byte[] Notify(int vol)
    {
        const double dur = 0.78;
        double p = 0;
        return Render(dur, vol, t =>
        {
            int idx = (int)(t / 0.15);
            if (idx > 2) return 0;
            double local = t - idx * 0.15;
            if (local > 0.085) return 0;

            double f = idx == 2 ? 1174.7 : 880;
            p += 2 * Math.PI * f / 44100.0;
            double e = Math.Sin(Math.PI * local / 0.085);
            return Math.Sin(p) * e * 0.55;
        });
    }

    /// <summary>轻柔双音，适合"回来啦"这种场景。</summary>
    private static byte[] Soft(int vol)
    {
        const double dur = 1.1;
        double p1 = 0, p2 = 0;
        return Render(dur, vol, t =>
        {
            p1 += 2 * Math.PI * 659.25 / 44100.0;
            p2 += 2 * Math.PI * 987.77 / 44100.0;

            double e1 = Math.Exp(-3.4 * t) * Math.Min(1, t / 0.006);
            double e2 = 0;
            if (t >= 0.12)
            {
                double t2 = t - 0.12;
                e2 = Math.Exp(-3.4 * t2) * Math.Min(1, t2 / 0.006);
            }
            return (Math.Sin(p1) * e1 + Math.Sin(p2) * e2 * 0.62) * 0.45;
        });
    }

    public static byte[] BuildMeowWav(int volume) => Meow(volume);
}
