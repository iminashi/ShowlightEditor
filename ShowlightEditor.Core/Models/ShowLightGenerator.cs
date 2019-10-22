using Rocksmith2014Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using static ShowlightEditor.Core.Models.FogGenerationMethod;

namespace ShowlightEditor.Core.Models
{
    public sealed class ShowlightGenerator
    {
        public readonly Dictionary<ShowlightType, string> ArrangementFilenames = new Dictionary<ShowlightType, string>();

        private float? SoloSectionTime;
        private float FirstBeatTime;
        private float SongLength;

        private readonly FogGenerationOptions fogOptions;
        private readonly BeamGenerationOptions beamOptions;
        private readonly LaserGenerationOptions laserOptions;

        private static readonly int[] StandardMIDINotes = { 40, 45, 50, 55, 59, 64 };

        public ShowlightGenerator(
            string fileForFog,
            string fileForBeams,
            FogGenerationOptions fogGenerationOptions,
            BeamGenerationOptions beamGenerationOptions,
            LaserGenerationOptions laserGenerationOptions)
        {
            ArrangementFilenames[ShowlightType.Fog] = fileForFog;
            ArrangementFilenames[ShowlightType.Beam] = fileForBeams;

            fogOptions = fogGenerationOptions;
            beamOptions = beamGenerationOptions;
            laserOptions = laserGenerationOptions;
        }

        private static int GetMIDINote(int[] tuning, int @string, int fret, int capo, bool bass = false)
        {
            int note = StandardMIDINotes[@string] + tuning[@string] + fret - (bass ? 12 : 0);

            if (capo > 0 && fret == 0)
                note += capo;

            return note;
        }

        private static int GetChordNote(int[] tuning, Chord chord, ChordTemplateCollection handshapes, int capo, bool bass = false)
        {
            ChordTemplate chordTemplate = handshapes[chord.ChordId];

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

        private static void ClearNotesofType(List<Showlight> showlights, ShowlightType type)
            => showlights.RemoveAll(sl => sl.ShowlightType == type);

        public async Task<List<Showlight>> Generate(IEnumerable<Showlight> initialShowlights)
        {
            var showlights = new List<Showlight>(initialShowlights);
            SoloSectionTime = null;

            if (fogOptions.ShouldGenerate)
                await GenerateShowlights(showlights, ShowlightType.Fog).ConfigureAwait(false);

            if (beamOptions.ShouldGenerate)
                await GenerateShowlights(showlights, ShowlightType.Beam).ConfigureAwait(false);

            if (laserOptions.ShouldGenerate)
            {
                ClearNotesofType(showlights, ShowlightType.Laser);

                GenerateLaserLights(showlights);
            }

            // Workaround for last fog color glitching
            if (!showlights.Exists(sl => sl.Time >= SongLength && sl.ShowlightType == ShowlightType.Fog))
                AddExtraFogNote(showlights);

            showlights.Sort();

            return showlights;
        }

        private async Task GenerateShowlights(List<Showlight> showlights, ShowlightType type)
        {
            ClearNotesofType(showlights, type);

            var arrData = await GetArrangementData(ArrangementFilenames[type]).ConfigureAwait(false);

            if (type == ShowlightType.Fog)
                showlights.AddRange(GenerateFogNotes(arrData));
            else
                showlights.AddRange(GenerateBeamNotes(arrData, showlights));

            ValidateFirstNoteOfType(showlights, type);
        }

        private async Task<ArrangementData> GetArrangementData(string filename)
        {
            var fileInfo = new FileInfo(filename);
            var timeModified = fileInfo.LastWriteTime;

            if (!ArrangementCache.TryGetArrangementData(filename, out ArrangementData arrData, timeModified))
            {
                var Song = await RS2014Song.LoadAsync(filename).ConfigureAwait(false);

                arrData = new ArrangementData
                {
                    Sections = Song.Sections,
                    Ebeats = Song.Ebeats,
                    FirstBeatTime = Song.StartBeat,
                    SongLength = Song.SongLength,
                    TimeModified = timeModified
                };

                // Try to find a solo section in the latter half of the song. If not found, use the first one
                var soloSections = Song.Sections.Where(sec => sec.Name == "solo");
                Section soloSection = soloSections.FirstOrDefault(sec => sec.Time >= arrData.SongLength / 2) ?? soloSections.FirstOrDefault();
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

        private void GenerateLaserLights(List<Showlight> showlights)
        {
            if (laserOptions.DisableLaserLights)
            {
                // Add "laser lights on" at the very end of the song
                showlights.Add(new Showlight(Showlight.LasersOn, SongLength - 0.1f));
                showlights.Add(new Showlight(Showlight.LasersOff, SongLength));

                return;
            }

            if (SoloSectionTime.HasValue)
            {
                showlights.Add(new Showlight(Showlight.LasersOn, SoloSectionTime.Value));
            }
            else
            {
                // No solo sections, set lasers on at 60% into the song
                showlights.Add(new Showlight(Showlight.LasersOn, (float)Math.Round(SongLength * 0.6, 3)));
            }

            showlights.Add(new Showlight(Showlight.LasersOff, SongLength - 5.0f));
        }

        // Adds an extra fog note at the end of the audio to prevent the last fog color from glitching.
        private void AddExtraFogNote(List<Showlight> showlights)
        {
            showlights.Add(new Showlight(Showlight.FogMax, SongLength + 0.1f));
        }

        // Ensures that at least one note of the type is present and moves the first note to the start of the beatmap.
        private void ValidateFirstNoteOfType(List<Showlight> showlights, ShowlightType showlightType)
        {
            int firstNoteIndex = showlights.FindIndex(sl => sl.ShowlightType == showlightType);
            if (firstNoteIndex == -1)
            {
                // Add new random note if not found
                showlights.Insert(0,
                    new Showlight(
                        showlightType == ShowlightType.Fog ? FogGenerationFunctions.GetRandomFogNote() : BeamGenerationFunctions.GetRandomBeamNote(),
                        FirstBeatTime)
                );
            }
            else if (showlights[firstNoteIndex].Time != FirstBeatTime)
            {
                // Move first note to start of the beatmap
                showlights[firstNoteIndex] = new Showlight(showlights[firstNoteIndex].Note, FirstBeatTime);
            }
        }

        private static (IList<Note> notes, IList<Chord> chords) GetNotesAndChordsFromSong(RS2014Song song)
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

        private static (IList<Note> notes, IList<Chord> chords) GenerateTranscriptionTrack(RS2014Song song)
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

                float phraseStartTime = phraseIteration.Time;
                float phraseEndTime = song.PhraseIterations[i + 1].Time;
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

        private static List<MidiNote> GetMidiNotes(RS2014Song song, IEnumerable<Note> notes, IEnumerable<Chord> chords, out int minMidiNote)
        {
            var MidiNotes = new List<MidiNote>();

            var handShapes = song.ChordTemplates;
            bool isBass = song.ArrangementProperties.PathBass == 1;
            int capo = song.Capo;
            int[] tuning = song.Tuning.Strings;

            minMidiNote = StandardMIDINotes[0] + tuning[0] - (isBass ? 12 : 0);

            foreach (Note note in notes)
            {
                MidiNotes.Add(new MidiNote(GetMIDINote(tuning, note.String, note.Fret, capo, isBass), note.Time));
            }

            foreach (Chord chord in chords)
            {
                MidiNotes.Add(new MidiNote(GetChordNote(tuning, chord, handShapes, capo, isBass), chord.Time, wasChord: true));
            }

            MidiNotes.Sort((x, y) => x.Time.CompareTo(y.Time));

            return MidiNotes;
        }

        private IEnumerable<Showlight> GenerateFogNotes(ArrangementData arrangement)
        {
            FogGenerationFunctions.Initialize(arrangement.MidiNotes, fogOptions.RandomizeColors);

            switch (fogOptions.GenerationMethod)
            {
                case SingleColor:
                    return Enumerable.Repeat(new Showlight(fogOptions.SelectedSingleFogColor, arrangement.FirstBeatTime), 1);

                case ChangeEveryNthBar:
                    return FogGenerationFunctions.FromBarNumbers(arrangement.Ebeats, fogOptions.ChangeFogColorEveryNthBar);

                case MinTimeBetweenChanges:
                    return FogGenerationFunctions.FromMinTime(fogOptions.MinTimeBetweenNotes);

                case FromSectionNames:
                    return FogGenerationFunctions.FromSections(arrangement.Sections);

                case FromLowestOctaveNotes:
                    int min = arrangement.LowOctaveMinMidiNote;
                    int max = arrangement.LowOctaveMaxMidiNote;

                    return FogGenerationFunctions.ConditionalGenerate(mn => mn.Note >= min && mn.Note <= max);

                case FromChords:
                    return FogGenerationFunctions.ConditionalGenerate(mn => mn.WasChord);

                default:
                    Debug.Print("ERROR: Unknown fog generation method.");
                    return Enumerable.Empty<Showlight>();
            }
        }

        private IEnumerable<Showlight> GenerateBeamNotes(ArrangementData arrangement, List<Showlight> currentShowlights)
        {
            BeamGenerationFunctions.Initialize(arrangement.MidiNotes, beamOptions.RandomizeColors);

            switch (beamOptions.GenerationMethod)
            {
                case BeamGenerationMethod.MinTimeBetweenChanges:
                    return BeamGenerationFunctions.FromMinTime(
                        currentShowlights,
                        arrangement.Sections,
                        beamOptions.MinTimeBetweenNotes,
                        beamOptions.UseCompatibleColors);

                case BeamGenerationMethod.FollowFogNotes:
                    return BeamGenerationFunctions.FromFogNotes(currentShowlights);

                default:
                    Debug.Print("ERROR: Unknown beam generation method.");
                    return Enumerable.Empty<Showlight>();
            }
        }
    }
}
