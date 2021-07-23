using Rocksmith2014.XML;

using System;
using System.Collections.Generic;

namespace ShowLightGenerator
{
    public readonly struct MidiNote : IEquatable<MidiNote>
    {
        public static readonly int[] StandardMIDINotes = { 40, 45, 50, 55, 59, 64 };

        public int Note { get; }
        public int Time { get; }
        public bool WasChord { get; }

        public MidiNote(int note, int time, bool wasChord = false)
        {
            Note = note;
            Time = time;
            WasChord = wasChord;
        }

        public static int GetMIDINote(short[] tuning, int @string, int fret, int capo, bool bass = false)
        {
            int note = StandardMIDINotes[@string] + tuning[@string] + fret - (bass ? 12 : 0);

            if (capo > 0 && fret == 0)
                note += capo;

            return note;
        }

        public static int GetChordNote(short[] tuning, Chord chord, List<ChordTemplate> handShapes, int capo, bool bass = false)
        {
            ChordTemplate chordTemplate = handShapes[chord.ChordId];

            for (int @string = 0; @string < 6; @string++)
            {
                var fret = chordTemplate.Frets[@string];
                if (fret != -1)
                {
                    // Return lowest found note even if it is not the root
                    return GetMIDINote(tuning, @string, fret, capo, bass);
                }
            }

            return 35;
        }

        public static MidiNote FromNote(Note note, short[] tuning, int capo, bool isBass) =>
            new MidiNote(GetMIDINote(tuning, note.String, note.Fret, capo, isBass), note.Time);

        public static MidiNote FromChord(Chord chord, List<ChordTemplate> handShapes, short[] tuning, int capo, bool isBass) =>
            new MidiNote(GetChordNote(tuning, chord, handShapes, capo, isBass), chord.Time, wasChord: true);

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
