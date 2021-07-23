using Rocksmith2014.XML;

using System;
using System.Collections.Generic;
using System.Linq;

namespace ShowLightGenerator
{
    public sealed class FogGenerationFunctions
    {
        private bool ShouldRandomize { get; }
        private List<MidiNote> MidiNotes { get; }

        public FogGenerationFunctions(List<MidiNote> midiNotes, bool shouldRandomize)
        {
            MidiNotes = midiNotes;
            ShouldRandomize = shouldRandomize;
        }

        public static byte GetFogNote(int note)
            => (byte)(ShowLight.FogMin + (note % 12));

        public static byte GetRandomFogNote(int excludeNote = -1)
        {
            int rNote;
            do
            {
                rNote = Randomizer.Next(ShowLight.FogMin, ShowLight.FogMax + 1);
            }
            while (rNote == excludeNote);

            return (byte)rNote;
        }

        public IEnumerable<ShowLight> FromBarNumbers(List<Ebeat> ebeats, int barChangeNumber)
        {
            int barCounter = 0;
            byte previousNote = 0;

            foreach (var beat in ebeats)
            {
                int beatTime = beat.Time;
                if (beat.Measure != -1)
                    barCounter++;

                if (barCounter < barChangeNumber)
                    continue;

                // Stop if the arrangement doesn't have any more notes
                if (beatTime > MidiNotes[^1].Time)
                    break;

                var midiNote = MidiNotes.Find(n => n.Time >= beatTime);
                byte fogNote = ShouldRandomize ? GetRandomFogNote(previousNote) : GetFogNote(midiNote.Note);

                // Skip if same color as previous
                if (previousNote == fogNote)
                {
                    barCounter = 0;
                    continue;
                }

                yield return new ShowLight(beatTime, fogNote);

                barCounter = 0;
                previousNote = fogNote;
            }
        }

        public IEnumerable<ShowLight> FromMinTime(float minTime)
        {
            int minTimeMs = (int)(minTime * 1000f);
            int previousTime = 0;
            byte previousNote = 0;

            foreach (var midiNote in MidiNotes)
            {
                if (midiNote.Time - previousTime < minTimeMs)
                    continue;

                byte fogNote = ShouldRandomize ? GetRandomFogNote(previousNote) : GetFogNote(midiNote.Note);
                if (fogNote == previousNote)
                    continue;

                yield return new ShowLight(midiNote.Time, fogNote);

                previousNote = fogNote;
                previousTime = midiNote.Time;
            }
        }

        public IEnumerable<ShowLight> FromSections(List<Section> sections)
        {
            var sectionFogNotes = new Dictionary<string, byte>();
            string previousSectionName = string.Empty;
            byte previousNote = 0;

            // Don't generate a note for the last noguitar section (END)
            foreach (Section section in sections.SkipLast())
            {
                string sectionName = section.Name;

                if (previousSectionName == sectionName)
                    continue;

                int sectionStartTime = section.Time;

                if (sectionFogNotes.ContainsKey(sectionName))
                {
                    if (previousNote == sectionFogNotes[sectionName])
                        continue;

                    yield return new ShowLight(sectionStartTime, sectionFogNotes[sectionName]);
                }
                else
                {
                    int midiNote = MidiNotes.Find(m => m.Time >= sectionStartTime).Note;
                    if (midiNote == 0)
                    {
                        // No more notes in the arrangement
                        midiNote = GetRandomFogNote();
                    }

                    byte fogNote = ShouldRandomize ? GetRandomFogNote() : GetFogNote(midiNote);
                    if (fogNote == previousNote)
                    {
                        fogNote = GetRandomFogNote(fogNote);
                    }

                    sectionFogNotes[sectionName] = fogNote;

                    yield return new ShowLight(sectionStartTime, sectionFogNotes[sectionName]);
                }

                previousSectionName = sectionName;
                previousNote = sectionFogNotes[sectionName];
            }
        }

        public IEnumerable<ShowLight> ConditionalGenerate(Func<MidiNote, bool> predicate)
        {
            byte previousNote = 0;

            foreach (var midiNote in MidiNotes.Where(predicate))
            {
                byte fogNote = ShouldRandomize ? GetRandomFogNote(previousNote) : GetFogNote(midiNote.Note);
                if (fogNote == previousNote)
                    continue;

                yield return new ShowLight(midiNote.Time, fogNote);

                previousNote = fogNote;
            }
        }
    }
}
