using Rocksmith2014.XML;

using System;
using System.Collections.Generic;

namespace ShowLightGenerator
{
    internal sealed class ArrangementData
    {
        private int lowOctaveMinMidiNote;

        public List<MidiNote> MidiNotes { get; set; }

        public List<Section> Sections { get; set; }
        public List<Ebeat> Ebeats { get; set; }

        public DateTime TimeModified { get; set; }

        public int LowOctaveMinMidiNote
        {
            get => lowOctaveMinMidiNote;
            set
            {
                lowOctaveMinMidiNote = value;
                LowOctaveMaxMidiNote = value + 11;
            }
        }

        public int LowOctaveMaxMidiNote { get; private set; }

        public int? SoloSectionTime { get; set; }
        public int FirstBeatTime { get; set; }
        public int SongLength { get; set; }
    }
}
