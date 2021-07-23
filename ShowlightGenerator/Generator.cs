using Rocksmith2014.XML;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using static ShowLightGenerator.FogGenerationMethod;

namespace ShowLightGenerator
{
    public sealed class Generator
    {
        private readonly Dictionary<ShowLightType, string> arrangementFilenames = new Dictionary<ShowLightType, string>();

        private int? SoloSectionTime;
        private int FirstBeatTime;
        private int SongLength;

        private readonly FogGenerationOptions fogOptions;
        private readonly BeamGenerationOptions beamOptions;
        private readonly LaserGenerationOptions laserOptions;

        public Generator(
            string fileForFog,
            string fileForBeams,
            FogGenerationOptions fogGenerationOptions,
            BeamGenerationOptions beamGenerationOptions,
            LaserGenerationOptions laserGenerationOptions)
        {
            arrangementFilenames[ShowLightType.Fog] = fileForFog;
            arrangementFilenames[ShowLightType.Beam] = fileForBeams;

            fogOptions = fogGenerationOptions;
            beamOptions = beamGenerationOptions;
            laserOptions = laserGenerationOptions;
        }

        private static void ClearNotesofType(List<ShowLight> showlights, ShowLightType type)
            => showlights.RemoveAll(sl => sl.GetShowLightType() == type);

        public List<ShowLight> Generate(IEnumerable<ShowLight> initialShowlights)
        {
            var showlights = new List<ShowLight>(initialShowlights);
            SoloSectionTime = null;

            if (fogOptions.ShouldGenerate)
                GenerateShowlights(showlights, ShowLightType.Fog);

            if (beamOptions.ShouldGenerate)
                GenerateShowlights(showlights, ShowLightType.Beam);

            if (laserOptions.ShouldGenerate)
            {
                ClearNotesofType(showlights, ShowLightType.Laser);

                GenerateLaserLights(showlights);
            }

            // Workaround for last fog color glitching
            if (!showlights.Exists(sl => sl.Time >= SongLength && sl.IsFog()))
                AddExtraFogNote(showlights);

            showlights.Sort((a, b) => a.Time.CompareTo(b.Time));

            return showlights;
        }

        public List<ShowLight> Generate() =>
            Generate(Enumerable.Empty<ShowLight>());

        private void GenerateShowlights(List<ShowLight> showlights, ShowLightType type)
        {
            ClearNotesofType(showlights, type);

            var arrData = GetArrangementData(arrangementFilenames[type]);

            if (type == ShowLightType.Fog)
                showlights.AddRange(GenerateFogNotes(arrData));
            else
                showlights.AddRange(GenerateBeamNotes(arrData, showlights));

            ValidateFirstNoteOfType(showlights, type);
        }

        private ArrangementData GetArrangementData(string filename)
        {
            var fileInfo = new FileInfo(filename);
            var timeModified = fileInfo.LastWriteTime;

            if (!ArrangementCache.TryGetArrangementData(filename, out ArrangementData arrData, timeModified))
            {
                var Song = InstrumentalArrangement.Load(filename);

                arrData = new ArrangementData
                {
                    Sections = Song.Sections,
                    Ebeats = Song.Ebeats,
                    FirstBeatTime = Song.StartBeat,
                    SongLength = Song.MetaData.SongLength,
                    TimeModified = timeModified
                };

                // Try to find a solo section in the latter half of the song. If not found, use the first one
                var soloSections = Song.Sections.Where(s => s.Name == "solo");
                var soloSection = soloSections.FirstOrDefault(s => s.Time >= arrData.SongLength / 2) ?? soloSections.FirstOrDefault();
                arrData.SoloSectionTime = soloSection?.Time;

                // Get notes and chords
                var (notes, chords) = GetNotesAndChordsFromSong(Song);

                // Get MIDI notes
                arrData.MidiNotes = GetMidiNotes(Song, notes, chords, out int minMidiNote);
                arrData.LowOctaveMinMidiNote = minMidiNote;

                ArrangementCache.AddArrangementData(filename, arrData);
            }

            FirstBeatTime = arrData.FirstBeatTime;
            SongLength = arrData.SongLength;
            if (arrData.SoloSectionTime.HasValue)
                SoloSectionTime = arrData.SoloSectionTime.Value;

            return arrData;
        }

        private void GenerateLaserLights(List<ShowLight> showlights)
        {
            if (laserOptions.DisableLaserLights)
            {
                // Add "laser lights on" at the very end of the song
                showlights.Add(new ShowLight(SongLength - 100, ShowLight.LasersOn));
                showlights.Add(new ShowLight(SongLength, ShowLight.LasersOff));

                return;
            }

            if (SoloSectionTime.HasValue)
            {
                showlights.Add(new ShowLight(SoloSectionTime.Value, ShowLight.LasersOn));
            }
            else
            {
                // No solo sections, set lasers on at 60% into the song
                int time = (int)Math.Round(SongLength * 0.6);
                showlights.Add(new ShowLight(time, ShowLight.LasersOn));
            }

            showlights.Add(new ShowLight(SongLength - 5000, ShowLight.LasersOff));
        }

        // Adds an extra fog note at the end of the audio to prevent the last fog color from glitching.
        private void AddExtraFogNote(List<ShowLight> showlights)
        {
            showlights.Add(new ShowLight(SongLength, ShowLight.FogMax));
        }

        // Ensures that at least one note of the type is present and moves the first note to the start of the beatmap.
        private void ValidateFirstNoteOfType(List<ShowLight> showlights, ShowLightType showlightType)
        {
            int firstNoteIndex = showlights.FindIndex(sl => sl.GetShowLightType() == showlightType);
            if (firstNoteIndex == -1)
            {
                // Add new random note if not found
                showlights.Insert(0,
                    new ShowLight(FirstBeatTime,
                        showlightType == ShowLightType.Fog ? FogGenerationFunctions.GetRandomFogNote() : BeamGenerationFunctions.GetRandomBeamNote())
                );
            }
            else if (showlights[firstNoteIndex].Time != FirstBeatTime)
            {
                // Move first note to start of the beatmap
                showlights[firstNoteIndex] = new ShowLight(FirstBeatTime, showlights[firstNoteIndex].Note);
            }
        }

        private static (IList<Note> notes, IList<Chord> chords) GetNotesAndChordsFromSong(InstrumentalArrangement song)
        {
            // Check if the song has DD levels
            if (song.Levels.Count != 1)
            {
                int transcriptionNoteCount = song.TranscriptionTrack?.Notes?.Count ?? 0;
                int transcriptionChordCount = song.TranscriptionTrack?.Chords?.Count ?? 0;

                if (transcriptionNoteCount == 0 && transcriptionChordCount == 0)
                {
                    // Manual DD, no transcription track available
                    return GenerateTranscriptionTrack(song);
                }
                else
                {
                    // Use transcription track
                    return (song.TranscriptionTrack.Notes,
                            song.TranscriptionTrack.Chords);
                }
            }
            else // No DD, just grab all notes and chords
            {
                return (song.Levels[0].Notes, song.Levels[0].Chords);
            }
        }

        private static (IList<Note> notes, IList<Chord> chords) GenerateTranscriptionTrack(InstrumentalArrangement song)
        {
            var phrases = song.Phrases;
            var notes = new List<Note>();
            var chords = new List<Chord>();

            // Ignore the last phrase iteration (END)
            for (int i = 0; i < song.PhraseIterations.Count - 1; i++)
            {
                var phraseIteration = song.PhraseIterations[i];
                int phraseId = phraseIteration.PhraseId;
                int maxDifficulty = phrases[phraseId].MaxDifficulty;
                if (maxDifficulty == 0)
                    continue;

                int phraseStartTime = phraseIteration.Time;
                int phraseEndTime = song.PhraseIterations[i + 1].Time;
                var highestLevelForPhrase = song.Levels[maxDifficulty];

                var notesInPhraseIteration = highestLevelForPhrase.Notes
                    .Where(n => n.Time >= phraseStartTime && n.Time < phraseEndTime);

                var chordsInPhraseIteration = highestLevelForPhrase.Chords
                    .Where(c => c.Time >= phraseStartTime && c.Time < phraseEndTime);

                notes.AddRange(notesInPhraseIteration);
                chords.AddRange(chordsInPhraseIteration);
            }

            return (notes, chords);
        }

        private static List<MidiNote> GetMidiNotes(
            InstrumentalArrangement song,
            IEnumerable<Note> notes,
            IEnumerable<Chord> chords,
            out int minMidiNote)
        {
            var midiNotes = new List<MidiNote>();

            var handShapes = song.ChordTemplates;
            bool isBass = song.MetaData.ArrangementProperties.PathBass;
            sbyte capo = song.MetaData.Capo;
            short[] tuning = song.MetaData.Tuning.Strings;

            minMidiNote = MidiNote.StandardMIDINotes[0] + tuning[0] - (isBass ? 12 : 0);

            foreach (Note note in notes)
            {
                midiNotes.Add(MidiNote.FromNote(note, tuning, capo, isBass));
            }

            foreach (Chord chord in chords)
            {
                midiNotes.Add(MidiNote.FromChord(chord, handShapes, tuning, capo, isBass));
            }

            midiNotes.Sort((x, y) => x.Time.CompareTo(y.Time));

            return midiNotes;
        }

        private IEnumerable<ShowLight> GenerateFogNotes(ArrangementData arrangement)
        {
            var fogFunctions = new FogGenerationFunctions(arrangement.MidiNotes, fogOptions.RandomizeColors);

            switch (fogOptions.GenerationMethod)
            {
                case SingleColor:
                    return Enumerable.Repeat(new ShowLight(arrangement.FirstBeatTime, fogOptions.SelectedSingleFogColor), 1);

                case ChangeEveryNthBar:
                    return fogFunctions.FromBarNumbers(arrangement.Ebeats, fogOptions.ChangeFogColorEveryNthBar);

                case MinTimeBetweenChanges:
                    return fogFunctions.FromMinTime(fogOptions.MinTimeBetweenNotes);

                case FromSectionNames:
                    return fogFunctions.FromSections(arrangement.Sections);

                case FromLowestOctaveNotes:
                    int min = arrangement.LowOctaveMinMidiNote;
                    int max = arrangement.LowOctaveMaxMidiNote;

                    return fogFunctions.ConditionalGenerate(mn => mn.Note >= min && mn.Note <= max);

                case FromChords:
                    return fogFunctions.ConditionalGenerate(mn => mn.WasChord);

                default:
                    Debug.Print("ERROR: Unknown fog generation method.");
                    return Enumerable.Empty<ShowLight>();
            }
        }

        private IEnumerable<ShowLight> GenerateBeamNotes(ArrangementData arrangement, List<ShowLight> currentShowlights)
        {
            var beamFunctions = new BeamGenerationFunctions(arrangement.MidiNotes, beamOptions.RandomizeColors);

            switch (beamOptions.GenerationMethod)
            {
                case BeamGenerationMethod.MinTimeBetweenChanges:
                    return beamFunctions.FromMinTime(
                        currentShowlights,
                        arrangement.Sections,
                        beamOptions.MinTimeBetweenNotes,
                        beamOptions.UseCompatibleColors);

                case BeamGenerationMethod.FollowFogNotes:
                    return BeamGenerationFunctions.FromFogNotes(currentShowlights);

                default:
                    Debug.Print("ERROR: Unknown beam generation method.");
                    return Enumerable.Empty<ShowLight>();
            }
        }
    }
}
