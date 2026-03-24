using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;


namespace radio8it
{

    public class WaveSynth
    {
        private const int SAMPLE_RATE = 44100;

        [DllImport("winmm.dll")]
        private static extern int waveOutOpen(out IntPtr hWaveOut, int deviceID, ref WAVEFORMATEX format, IntPtr callback, IntPtr instance, int flags);

        [DllImport("winmm.dll")]
        private static extern int waveOutPrepareHeader(IntPtr hWaveOut, ref WAVEHDR header, int size);

        [DllImport("winmm.dll")]
        private static extern int waveOutWrite(IntPtr hWaveOut, ref WAVEHDR header, int size);

        [DllImport("winmm.dll")]
        private static extern int waveOutUnprepareHeader(IntPtr hWaveOut, ref WAVEHDR header, int size);

        [DllImport("winmm.dll")]
        private static extern int waveOutClose(IntPtr hWaveOut);

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

        private IntPtr _hWaveOut;

        public WaveSynth()
        {
            var format = new WAVEFORMATEX
            {
                wFormatTag = 1, // PCM
                nChannels = 1,
                nSamplesPerSec = SAMPLE_RATE,
                wBitsPerSample = 16,
                nBlockAlign = 2,
                nAvgBytesPerSec = SAMPLE_RATE * 2
            };

            waveOutOpen(out _hWaveOut, -1, ref format, IntPtr.Zero, IntPtr.Zero, 0);
        }

        public void Play(List<SynthNote> notes)
        {
            if (notes == null || notes.Count == 0) return;

            int totalMs = 0;
            foreach (var n in notes)
            {
                totalMs = Math.Max(totalMs, n.StartMs + n.DurationMs);
            }

            //changed to long to prevent overflow on norm length songs
            long totalSamplesLong = (long)SAMPLE_RATE * totalMs / 1000;
            int totalSamples = (int)totalSamplesLong;

            if (totalSamples <= 0) return;

            double[] mixBuffer = new double[totalSamples];

            foreach (var note in notes)
            {
                int startSample = (int)((long)SAMPLE_RATE * note.StartMs / 1000);
                int durationSamples = (int)((long)SAMPLE_RATE * note.DurationMs / 1000);

                for (int i = 0; i < durationSamples; i++)
                {
                    int idx = startSample + i;

                    // Bounds checking for index
                    if (idx >= 0 && idx < mixBuffer.Length)
                    {
                        double t = (double)i / SAMPLE_RATE;

                        // 🎸 GUITAR-LIKE WAVE  <--- Gemeni suggested guitar synth for riff heavy songs when spitballing idea. I like it
                        double wave = Math.Sin(2 * Math.PI * note.Frequency * t);
                        wave = Math.Tanh(wave * 3); // distortion

                        // Wrap/Mix sound
                        double attack = Math.Min(1.0, i / (SAMPLE_RATE * 0.01));
                        double decay = 1.0 - ((double)i / durationSamples);

                        mixBuffer[idx] += wave * attack * decay * 0.3;
                    }
                }
            }

            // Normalize 
            short[] finalBuffer = new short[totalSamples];
            double max = 0;
            foreach (var v in mixBuffer)
            {
                if (Math.Abs(v) > max) max = Math.Abs(v);
            }

            // Prevent zero division <== annoying ass problem
            if (max > 0)
            {
                for (int i = 0; i < totalSamples; i++)
                {
                    finalBuffer[i] = (short)(mixBuffer[i] / max * short.MaxValue);
                }
            }

            PlayBuffer(finalBuffer);
        }

        private void PlayBuffer(short[] samples)
        {
            int byteSize = samples.Length * 2;
            IntPtr ptr = Marshal.AllocHGlobal(byteSize);
            Marshal.Copy(samples, 0, ptr, samples.Length);

            var header = new WAVEHDR
            {
                lpData = ptr,
                dwBufferLength = (uint)byteSize,
                dwFlags = 0
            };

            waveOutPrepareHeader(_hWaveOut, ref header, Marshal.SizeOf(header));
            waveOutWrite(_hWaveOut, ref header, Marshal.SizeOf(header));

            // Wait for song finish to prevent early release 
            int sleepTime = (samples.Length * 1000 / SAMPLE_RATE) + 100;
            Thread.Sleep(sleepTime);

            waveOutUnprepareHeader(_hWaveOut, ref header, Marshal.SizeOf(header));
            Marshal.FreeHGlobal(ptr);
        }

        ~WaveSynth()
        {
            if (_hWaveOut != IntPtr.Zero)
            {
                waveOutClose(_hWaveOut);
            }
        }
    }
}
