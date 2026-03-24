using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace radio8it
{
    public class PlaybackProgressArgs : EventArgs
    {
        public string SongName { get; set; }
        public int ElapsedMs { get; set; }
        public int TotalMs { get; set; }
        public double Progress => (double)ElapsedMs / TotalMs;
    }

    public class Radio8itPlayer
    {
        private readonly WaveSynth _synth = new WaveSynth();
        
        // Events for the UI to hook into
        public event EventHandler<string> SongStarted;
        public event EventHandler<PlaybackProgressArgs> ProgressUpdated;

        public async Task PlaySongAsync(string name, List<SynthNote> notes)
        {
            if (notes == null || notes.Count == 0) return;

            int totalMs = notes.Max(n => n.StartMs + n.DurationMs);
            SongStarted?.Invoke(this, name);

            // Start the synth on a background thread
            var playTask = Task.Run(() => _synth.Play(notes));

            // While the task is running, track time and update the UI
            Stopwatch sw = Stopwatch.StartNew();
            while (!playTask.IsCompleted)
            {
                ProgressUpdated?.Invoke(this, new PlaybackProgressArgs
                {
                    SongName = name,
                    ElapsedMs = (int)sw.ElapsedMilliseconds,
                    TotalMs = totalMs
                });

                await Task.Delay(100); // Update progress every 100ms
            }

            sw.Stop();
        }
    }
}
