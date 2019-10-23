using Rocksmith2014Xml;
using ShowlightEditor.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShowlightEditor.Core.Models
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

        private static readonly Dictionary<int, int[]> CompatibleColors = new Dictionary<int, int[]>
        {
            // Dark green
            { 24, new int[] { 42, 48, 50, 52, 53, 55, 57 } },

            // Dark red
            { 25, new int[] { 42, 49, 51, 52, 54, 56, 59 } },

            // Greenish blue
            { 26, new int[] { 42, 48, 49, 50, 52, 53, 55, 57, 58, 59 } },

            // Orange
            { 27, new int[] { 42, 51, 53, 56, 58  } },

            // Dark blue
            { 28, new int[] { 42, 49, 50, 52, 54, 55, 57, 59 } },

            // Yellow
            { 29, new int[] { 42, 48, 51, 53, 56, 58 } },

            // Purple
            { 30, new int[] { 42, 49, 50, 52, 54, 56, 57, 59 } },

            // Light green
            { 31, new int[] { 42, 48, 50, 52, 55, 57 } },

            // Light red
            { 32, new int[] { 42, 49, 51, 53, 54, 56, 58, 59 } },

            // Light blue
            { 33, new int[] { 42, 48, 50, 51, 52, 53, 54, 55, 57, 59 } },

            // Light yellow
            { 34, new int[] { 42, 51, 53, 56, 58 } },

            // Dark purple
            { 35, new int[] { 42, 49, 50, 52, 54, 56, 57, 59 } },
        };

        // TODO: Add off note?
        public static int GetBeamNote(int note)
            => Showlight.BeamMin + (note % 12);

        public static int GetRandomBeamNote(int excludeNote = -1)
        {
            int rNote = randomizer.Next(Showlight.BeamMin, Showlight.BeamMax + 2);

            // Randomly turn beams off
            if (rNote == Showlight.BeamMax + 1)
            {
                if (excludeNote != Showlight.BeamOff)
                    return Showlight.BeamOff;
                else
                    rNote = randomizer.Next(Showlight.BeamMin, Showlight.BeamMax + 1);
            }

            while (rNote == excludeNote)
            {
                rNote = randomizer.Next(Showlight.BeamMin, Showlight.BeamMax + 1);
            }

            return rNote;
        }

        public IEnumerable<Showlight> FromFogNotes(List<Showlight> currentShowlights)
        {
            return (from sl in currentShowlights
                    where sl.ShowlightType == ShowlightType.Fog
                    select new Showlight(sl.Note + 24, sl.Time))
                    .ToList();
        }

        public IEnumerable<Showlight> FromMinTime(
            List<Showlight> currentShowlights,
            SectionCollection sections,
            float minTime,
            bool useCompatible)
        {
            int previousNote = -1;
            float previousTime = 0f;

            foreach (var midiNote in MidiNotes)
            {
                if (midiNote.Time - previousTime < minTime || midiNote.Time == previousTime)
                    continue;

                int beamNote;

                if (useCompatible)
                {
                    Showlight activeFog = currentShowlights
                        .LastOrDefault(sl => sl.Time <= midiNote.Time && sl.ShowlightType == ShowlightType.Fog);

                    int[] compColorTable = (activeFog is null) ?
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

                yield return new Showlight(beamNote, midiNote.Time);

                previousNote = beamNote;
                previousTime = midiNote.Time;
            }

            // Turn beams off during noguitar sections (Skip last section (END))
            foreach (var ngSection in sections.Where(s => s.Name == "noguitar").SkipLast())
            {
                yield return new Showlight(Showlight.BeamOff, ngSection.Time);
            }
        }
    }
}
