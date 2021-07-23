using Rocksmith2014.XML;

using System;
using System.Collections.Generic;
using System.Linq;

namespace ShowLightGenerator
{
    public sealed class BeamGenerationFunctions
    {
        private static readonly Random randomizer = new Random();

        private bool ShouldRandomize { get; }
        private List<MidiNote> MidiNotes { get; }

        public BeamGenerationFunctions(List<MidiNote> midiNotes, bool shouldRandomize)
        {
            MidiNotes = midiNotes;
            ShouldRandomize = shouldRandomize;
        }

        private static readonly Dictionary<byte, byte[]> CompatibleColors = new Dictionary<byte, byte[]>
        {
            // Dark green
            { 24, new byte[] { 42, 48, 50, 52, 53, 55, 57 } },

            // Dark red
            { 25, new byte[] { 42, 49, 51, 52, 54, 56, 59 } },

            // Greenish blue
            { 26, new byte[] { 42, 48, 49, 50, 52, 53, 55, 57, 58, 59 } },

            // Orange
            { 27, new byte[] { 42, 51, 53, 56, 58  } },

            // Dark blue
            { 28, new byte[] { 42, 49, 50, 52, 54, 55, 57, 59 } },

            // Yellow
            { 29, new byte[] { 42, 48, 51, 53, 56, 58 } },

            // Purple
            { 30, new byte[] { 42, 49, 50, 52, 54, 56, 57, 59 } },

            // Light green
            { 31, new byte[] { 42, 48, 50, 52, 55, 57 } },

            // Light red
            { 32, new byte[] { 42, 49, 51, 53, 54, 56, 58, 59 } },

            // Light blue
            { 33, new byte[] { 42, 48, 50, 51, 52, 53, 54, 55, 57, 59 } },

            // Light yellow
            { 34, new byte[] { 42, 51, 53, 56, 58 } },

            // Dark purple
            { 35, new byte[] { 42, 49, 50, 52, 54, 56, 57, 59 } },
        };

        // TODO: Add off note?
        public static byte GetBeamNote(int note)
            => (byte)(ShowLight.BeamMin + (note % 12));

        public static byte GetRandomBeamNote(int excludeNote = -1)
        {
            int rNote = randomizer.Next(ShowLight.BeamMin, ShowLight.BeamMax + 2);

            // Randomly turn beams off
            if (rNote == ShowLight.BeamMax + 1)
            {
                if (excludeNote != ShowLight.BeamOff)
                    return ShowLight.BeamOff;
                else
                    rNote = randomizer.Next(ShowLight.BeamMin, ShowLight.BeamMax + 1);
            }

            while (rNote == excludeNote)
            {
                rNote = randomizer.Next(ShowLight.BeamMin, ShowLight.BeamMax + 1);
            }

            return (byte)rNote;
        }

        public static IEnumerable<ShowLight> FromFogNotes(List<ShowLight> currentShowlights)
        {
            return (from sl in currentShowlights
                    where sl.IsFog()
                    select new ShowLight(sl.Time, (byte)(sl.Note + 24)))
                    .ToList();
        }

        public IEnumerable<ShowLight> FromMinTime(
            List<ShowLight> currentShowlights,
            List<Section> sections,
            float minTime,
            bool useCompatible)
        {
            int minTimeMs = (int)(minTime * 1000f);
            int previousNote = -1;
            int previousTime = 0;

            foreach (var midiNote in MidiNotes)
            {
                if (midiNote.Time - previousTime < minTimeMs || midiNote.Time == previousTime)
                    continue;

                byte beamNote;

                if (useCompatible)
                {
                    var activeFog =
                        currentShowlights
                        .LastOrDefault(sl => sl.Time <= midiNote.Time && sl.IsFog());

                    byte[] compColorTable = (activeFog is null) ?
                        CompatibleColors[FogGenerationFunctions.GetRandomFogNote()] :
                        CompatibleColors[activeFog.Note];

                    // Check if normally retrieved beam is usable
                    if (!ShouldRandomize && Array.IndexOf(compColorTable, GetBeamNote(midiNote.Note)) >= 0)
                    {
                        beamNote = GetBeamNote(midiNote.Note);
                    }
                    else
                    {
                        int index = ShouldRandomize ? randomizer.Next(0, compColorTable.Length) : midiNote.Note % compColorTable.Length;
                        beamNote = compColorTable[index];
                    }
                }
                else
                {
                    beamNote = ShouldRandomize ? GetRandomBeamNote(previousNote) : GetBeamNote(midiNote.Note);
                }

                if (beamNote == previousNote)
                    continue;

                yield return new ShowLight(midiNote.Time, beamNote);

                previousNote = beamNote;
                previousTime = midiNote.Time;
            }

            // Turn beams off during noguitar sections (Skip last section (END))
            foreach (var ngSection in sections.Where(s => s.Name == "noguitar").SkipLast())
            {
                yield return new ShowLight(ngSection.Time, ShowLight.BeamOff);
            }
        }
    }
}
