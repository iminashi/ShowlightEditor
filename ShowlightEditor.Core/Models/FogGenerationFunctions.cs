using Rocksmith2014Xml;
using ShowlightEditor.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShowlightEditor.Core.Models
{
    public static class FogGenerationFunctions
    {
        private static readonly Random randomizer = new Random();

        private static bool ShouldRandomize { get; set; }
        private static List<MidiNote> MidiNotes { get; set; }

        public static void Initialize(List<MidiNote> midiNotes, bool shouldRandomize)
        {
            MidiNotes = midiNotes;
            ShouldRandomize = shouldRandomize;
        }

        public static int GetFogNote(int note)
            => Showlight.FogMin + (note % 12);

        public static int GetRandomFogNote(int excludeNote = -1)
        {
            int rNote = randomizer.Next(Showlight.FogMin, Showlight.FogMax + 1);
            while (rNote == excludeNote)
            {
                rNote = randomizer.Next(Showlight.FogMin, Showlight.FogMax + 1);
            }

            return rNote;
        }

        public static IEnumerable<Showlight> FromBarNumbers(EbeatCollection ebeats, int barChangeNumber)
        {
            int barCounter = 0;
            int previousNote = -1;

            foreach (var beat in ebeats)
            {
                float beatTime = beat.Time;
                if (beat.Measure != -1)
                    barCounter++;

                if (barCounter < barChangeNumber)
                    continue;

                // Stop if the arrangement doesn't have any more notes
                if (beatTime > MidiNotes[MidiNotes.Count - 1].Time)
                    break;

                var midiNote = MidiNotes.Find(n => n.Time >= beatTime);
                int fogNote = ShouldRandomize ? GetRandomFogNote(previousNote) : GetFogNote(midiNote.Note);

                // Skip if same color as previous
                if (previousNote == fogNote)
                {
                    barCounter = 0;
                    continue;
                }

                yield return new Showlight(fogNote, beatTime);

                barCounter = 0;
                previousNote = fogNote;
            }
        }

        public static IEnumerable<Showlight> FromMinTime(float minTime)
        {
            float previousTime = 0f;
            int previousNote = -1;

            foreach (var midiNote in MidiNotes)
            {
                if (midiNote.Time - previousTime < minTime)
                    continue;

                int fogNote = ShouldRandomize ? GetRandomFogNote(previousNote) : GetFogNote(midiNote.Note);
                if (fogNote == previousNote)
                    continue;

                yield return new Showlight(fogNote, midiNote.Time);

                previousNote = fogNote;
                previousTime = midiNote.Time;
            }
        }

        public static IEnumerable<Showlight> FromSections(SectionCollection sections)
        {
            Dictionary<string, int> sectionFogNotes = new Dictionary<string, int>();
            string previousSectionName = string.Empty;
            int previousNote = -1;

            // Don't generate a note for the last noguitar section (END)
            foreach (Section section in sections.SkipLast())
            {
                string sectionName = section.Name;

                if (previousSectionName == sectionName)
                    continue;

                float sectionStartTime = section.Time;

                if (sectionFogNotes.ContainsKey(sectionName))
                {
                    if (previousNote == sectionFogNotes[sectionName])
                        continue;

                    yield return new Showlight(sectionFogNotes[sectionName], sectionStartTime);
                }
                else
                {
                    int midiNote = MidiNotes.Find(m => m.Time >= sectionStartTime).Note;
                    if (midiNote == 0) // No more notes in the arrangement
                        midiNote = GetRandomFogNote();

                    int fogNote = ShouldRandomize ? GetRandomFogNote() : GetFogNote(midiNote);
                    if (fogNote == previousNote)
                    {
                        fogNote = GetRandomFogNote(fogNote);
                    }

                    sectionFogNotes[sectionName] = fogNote;

                    yield return new Showlight(sectionFogNotes[sectionName], sectionStartTime);
                }

                previousSectionName = sectionName;
                previousNote = sectionFogNotes[sectionName];
            }
        }

        public static IEnumerable<Showlight> ConditionalGenerate(Func<MidiNote, bool> predicate)
        {
            int previousNote = -1;

            foreach (var midiNote in MidiNotes.Where(predicate))
            {
                int fogNote = ShouldRandomize ? GetRandomFogNote(previousNote) : GetFogNote(midiNote.Note);
                if (fogNote == previousNote)
                    continue;

                yield return new Showlight(fogNote, midiNote.Time);

                previousNote = fogNote;
            }
        }
    }
}
