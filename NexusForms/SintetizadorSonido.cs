using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NexusForms
{
    public static class SintetizadorSonido
    {
        public static bool Silenciado { get; set; } = false;

        private static readonly Dictionary<string, byte[]> cachePcm = new Dictionary<string, byte[]>();
        private static readonly Dictionary<string, DateTime> ultimoDisparo = new Dictionary<string, DateTime>();
        private static readonly List<CanalActivo> canales = new List<CanalActivo>();
        private static readonly object lockAudio = new object();
        private static bool inicializado = false;

        private class CanalActivo
        {
            public IntPtr Handle;
            public GCHandle HeaderHandle;
            public GCHandle DataHandle;
            public DateTime Expira;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WAVEHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        [DllImport("winmm.dll")]
        private static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WAVEFORMATEX lpFormat, IntPtr dwCallback, IntPtr dwInstance, uint dwFlags);

        [DllImport("winmm.dll")]
        private static extern int waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutWrite(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, uint uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutReset(IntPtr hWaveOut);

        [DllImport("winmm.dll")]
        private static extern int waveOutClose(IntPtr hWaveOut);

        public static void Inicializar()
        {
            if (inicializado) return;
            inicializado = true;

            cachePcm["disparo"] = GenerarBarrido(1250, 340, 0.055, 0.38, "cuadrada");
            cachePcm["metralleta"] = GenerarBarrido(980, 260, 0.045, 0.35, "sierra");
            cachePcm["laser_continuo"] = GenerarBarrido(1450, 1180, 0.055, 0.25, "seno");
            cachePcm["canon"] = GenerarExplosion(0.18, 0.65, 220);
            cachePcm["misil"] = GenerarBarrido(380, 1050, 0.13, 0.42, "sierra");
            cachePcm["explosion"] = GenerarExplosion(0.32, 0.72, 115);
            cachePcm["powerup"] = GenerarArpegio(new double[] { 587.33, 783.99, 1046.50, 1318.51 }, 0.045, 0.45);
            cachePcm["moneda"] = GenerarArpegio(new double[] { 987.77, 1318.51 }, 0.045, 0.42);
            cachePcm["salto"] = GenerarBarrido(260, 660, 0.09, 0.40, "seno");
            cachePcm["paso"] = GenerarBarrido(190, 95, 0.03, 0.22, "seno");
            cachePcm["danio"] = GenerarBarrido(240, 65, 0.14, 0.60, "sierra");
            cachePcm["supernova"] = GenerarExplosion(0.65, 0.85, 75);
            cachePcm["alerta"] = GenerarArpegio(new double[] { 523.25, 261.63, 523.25, 261.63 }, 0.08, 0.48);
            cachePcm["victoria"] = GenerarArpegio(new double[] { 523.25, 659.25, 783.99, 1046.50, 1318.51 }, 0.075, 0.45);
            cachePcm["gameover"] = GenerarArpegio(new double[] { 392.00, 349.23, 311.13, 261.63 }, 0.12, 0.45);
            cachePcm["click"] = GenerarBarrido(880, 1320, 0.03, 0.30, "seno");
        }

        public static void Reproducir(string nombre, int cooldownMs = 35)
        {
            if (Silenciado) return;
            if (!inicializado) Inicializar();

            lock (lockAudio)
            {
                LimpiarCanalesTerminados();

                if (ultimoDisparo.TryGetValue(nombre, out DateTime prev))
                {
                    if ((DateTime.Now - prev).TotalMilliseconds < cooldownMs)
                        return;
                }
                ultimoDisparo[nombre] = DateTime.Now;

                if (!cachePcm.TryGetValue(nombre, out byte[]? pcm))
                    pcm = cachePcm["disparo"];

                if (canales.Count >= 12) return;

                try
                {
                    WAVEFORMATEX fmt = new WAVEFORMATEX
                    {
                        wFormatTag = 1,
                        nChannels = 1,
                        nSamplesPerSec = 22050,
                        wBitsPerSample = 16,
                        nBlockAlign = 2,
                        nAvgBytesPerSec = 44100,
                        cbSize = 0
                    };

                    if (waveOutOpen(out IntPtr hWave, -1, ref fmt, IntPtr.Zero, IntPtr.Zero, 0) != 0)
                        return;

                    GCHandle hData = GCHandle.Alloc(pcm, GCHandleType.Pinned);
                    WAVEHDR hdr = new WAVEHDR
                    {
                        lpData = hData.AddrOfPinnedObject(),
                        dwBufferLength = (uint)pcm.Length,
                        dwFlags = 0
                    };

                    GCHandle hHdr = GCHandle.Alloc(hdr, GCHandleType.Pinned);
                    uint hdrSize = (uint)Marshal.SizeOf(typeof(WAVEHDR));

                    waveOutPrepareHeader(hWave, hHdr.AddrOfPinnedObject(), hdrSize);
                    waveOutWrite(hWave, hHdr.AddrOfPinnedObject(), hdrSize);

                    double duracionMs = (pcm.Length / 44.1) + 40;
                    canales.Add(new CanalActivo
                    {
                        Handle = hWave,
                        HeaderHandle = hHdr,
                        DataHandle = hData,
                        Expira = DateTime.Now.AddMilliseconds(duracionMs)
                    });
                }
                catch { }
            }
        }

        private static void LimpiarCanalesTerminados()
        {
            DateTime ahora = DateTime.Now;
            uint hdrSize = (uint)Marshal.SizeOf(typeof(WAVEHDR));

            for (int i = canales.Count - 1; i >= 0; i--)
            {
                if (ahora >= canales[i].Expira)
                {
                    try
                    {
                        waveOutReset(canales[i].Handle);
                        waveOutUnprepareHeader(canales[i].Handle, canales[i].HeaderHandle.AddrOfPinnedObject(), hdrSize);
                        waveOutClose(canales[i].Handle);
                    }
                    catch { }
                    finally
                    {
                        if (canales[i].HeaderHandle.IsAllocated) canales[i].HeaderHandle.Free();
                        if (canales[i].DataHandle.IsAllocated) canales[i].DataHandle.Free();
                        canales.RemoveAt(i);
                    }
                }
            }
        }

        private static byte[] GenerarBarrido(double freqInicio, double freqFin, double duracionSeg, double volumen, string tipoOnda)
        {
            int sampleRate = 22050;
            int muestras = (int)(sampleRate * duracionSeg);
            byte[] bytes = new byte[muestras * 2];
            double fase = 0;

            for (int i = 0; i < muestras; i++)
            {
                double t = (double)i / muestras;
                double freq = freqInicio + (freqFin - freqInicio) * t;
                double envolvente = 1.0 - t;
                fase += (2.0 * Math.PI * freq) / sampleRate;

                double muestra = tipoOnda switch
                {
                    "cuadrada" => Math.Sin(fase) >= 0 ? 1.0 : -1.0,
                    "sierra" => 2.0 * ((fase / (2.0 * Math.PI)) % 1.0) - 1.0,
                    _ => Math.Sin(fase)
                };

                short val = (short)(muestra * envolvente * volumen * short.MaxValue);
                bytes[i * 2] = (byte)(val & 0xFF);
                bytes[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
            }
            return bytes;
        }

        private static byte[] GenerarExplosion(double duracionSeg, double volumen, double corteGrave)
        {
            int sampleRate = 22050;
            int muestras = (int)(sampleRate * duracionSeg);
            byte[] bytes = new byte[muestras * 2];
            Random rnd = new Random(42);
            double filtro = 0;
            double alpha = corteGrave / (corteGrave + sampleRate);

            for (int i = 0; i < muestras; i++)
            {
                double t = (double)i / muestras;
                double envolvente = Math.Pow(1.0 - t, 1.5);
                double ruido = rnd.NextDouble() * 2.0 - 1.0;
                filtro += alpha * (ruido - filtro);
                short val = (short)(Math.Clamp(filtro * 4.8, -1.0, 1.0) * envolvente * volumen * short.MaxValue);
                bytes[i * 2] = (byte)(val & 0xFF);
                bytes[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
            }
            return bytes;
        }

        private static byte[] GenerarArpegio(double[] notas, double durNotaSeg, double volumen)
        {
            int sampleRate = 22050;
            int muestrasPorNota = (int)(sampleRate * durNotaSeg);
            byte[] bytes = new byte[muestrasPorNota * notas.Length * 2];

            for (int n = 0; n < notas.Length; n++)
            {
                double freq = notas[n];
                for (int i = 0; i < muestrasPorNota; i++)
                {
                    double t = (double)i / muestrasPorNota;
                    double env = 1.0 - (t * 0.65);
                    double muestra = Math.Sin(2.0 * Math.PI * freq * i / sampleRate);
                    short val = (short)(muestra * env * volumen * short.MaxValue);
                    int idx = (n * muestrasPorNota + i) * 2;
                    bytes[idx] = (byte)(val & 0xFF);
                    bytes[idx + 1] = (byte)((val >> 8) & 0xFF);
                }
            }
            return bytes;
        }
    }
}