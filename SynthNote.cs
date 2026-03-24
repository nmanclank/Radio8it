namespace radio8it
{
    public class SynthNote
    {
        // Changed from int to double to support decimals from JSON
        public double Frequency { get; set; }
        public int StartMs { get; set; }
        public int DurationMs { get; set; }

        public SynthNote() { }

        public SynthNote(double freq, int start, int duration)
        {
            Frequency = freq;
            StartMs = start;
            DurationMs = duration;
        }
    }
}
