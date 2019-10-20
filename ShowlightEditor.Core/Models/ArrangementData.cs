using Rocksmith2014Xml;
using System;
using System.Collections.Generic;

namespace ShowlightEditor.Core.Models
{
    internal sealed class ArrangementData
    {
        private int lowOctaveMinMidiNote;
        private int lowOctaveMaxMidiNote;

        public List<MidiNote> MidiNotes { get; set; }

        public SectionCollection Sections { get; set; }
        public EbeatCollection Ebeats { get; set; }

        public DateTime TimeModified { get; set; }

        public int LowOctaveMinMidiNote
        {
            get => lowOctaveMinMidiNote;
            set
            {
                lowOctaveMinMidiNote = value;
                lowOctaveMaxMidiNote = value + 11;
            }
        }

        public int LowOctaveMaxMidiNote => lowOctaveMaxMidiNote;

        public float? SoloSectionTime;
        public float FirstBeatTime;
        public float SongLength;
    }
}
