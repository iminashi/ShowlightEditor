using System;

namespace ShowLightGenerator
{
    public readonly struct MidiNote : IEquatable<MidiNote>
    {
        public int Note { get; }
        public int Time { get; }
        public bool WasChord { get; }

        public MidiNote(int note, int time, bool wasChord = false)
        {
            Note = note;
            Time = time;
            WasChord = wasChord;
        }

        public override string ToString()
            => $"Time: {Time}, Note: {Note}, Was Chord: {WasChord}";

        public override bool Equals(object obj)
            => obj is MidiNote other && Equals(other);

        public override int GetHashCode()
            => (Note, Time, WasChord).GetHashCode();

        public bool Equals(MidiNote other)
        {
            return Note == other.Note
                && Time == other.Time
                && WasChord == other.WasChord;
        }

        public static bool operator ==(in MidiNote left, in MidiNote right)
            => left.Equals(right);

        public static bool operator !=(in MidiNote left, in MidiNote right)
            => !(left == right);
    }
}
