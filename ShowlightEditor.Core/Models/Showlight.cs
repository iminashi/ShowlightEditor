using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ShowlightEditor.Core.Models
{
    [Serializable, XmlRoot("showlight", Namespace = "")]
    public sealed class Showlight : ReactiveObject, IComparable<Showlight>, IEquatable<Showlight>, IXmlSerializable, ISerializable
    {
        #region Constants

        public const int FogMin = 24;
        public const int FogMax = 35;
        public const int BeamOff = 42;
        public const int BeamMin = 48;
        public const int BeamMax = 59;
        public const int LasersOff = 66;
        public const int LasersOn = 67;

        #endregion

        public static ShowlightType GetShowlightType(int note)
        {
            if (note >= FogMin && note <= FogMax)
                return ShowlightType.Fog;

            if (note == BeamOff || (note >= BeamMin && note <= BeamMax))
                return ShowlightType.Beam;

            if (note == LasersOn || note == LasersOff)
                return ShowlightType.Laser;

            return ShowlightType.Undefined;
        }

        private static int _id;
        private int _note;

        public int Id { get; }

        [Reactive]
        public float Time { get; set; }

        public int Note
        {
            get => _note;
            set
            {
                ShowlightType = GetShowlightType(value);
                this.RaiseAndSetIfChanged(ref _note, value);
            }
        }

        public ShowlightType ShowlightType { get; private set; }

        public Showlight()
        {
            Id = _id++;
        }

        public Showlight(int note, float time)
        {
            Id = _id++;
            Note = note;
            Time = time;
        }

        public Showlight(Showlight other)
        {
            Id = _id++;
            Note = other.Note;
            Time = other.Time;
        }

        #region IComparable<Showlight>, IEquatable<Showlight>, overrides, operators

        public int CompareTo(Showlight other)
        {
            if (other is null)
                return 1;

            return Time.CompareTo(other.Time);
        }

        public bool Equals(Showlight other)
        {
            if (other is null)
                return false;

            return Note == other.Note && Time == other.Time; //TODO: Floating point comparison
        }

        public override bool Equals(object obj)
            => obj is Showlight other && Equals(other);

        public override int GetHashCode()
            => Id;

        public override string ToString()
            => $"ID: {Id}, Time: {Time.ToString("F3", NumberFormatInfo.InvariantInfo)}, Note: {Note} ({ShowlightType})";

        public static bool operator ==(Showlight left, Showlight right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(Showlight left, Showlight right)
        {
            return !(left == right);
        }

        public static bool operator <(Showlight left, Showlight right)
            => left.CompareTo(right) < 0;

        public static bool operator <=(Showlight left, Showlight right)
            => left.CompareTo(right) <= 0;

        public static bool operator >(Showlight left, Showlight right)
            => left.CompareTo(right) > 0;

        public static bool operator >=(Showlight left, Showlight right)
            => left.CompareTo(right) >= 0;

        #endregion

        #region IXmlSerializable Implementation

        public XmlSchema GetSchema() => null;

        public void ReadXml(XmlReader reader)
        {
            Time = float.Parse(reader.GetAttribute("time"), NumberFormatInfo.InvariantInfo);
            Note = int.Parse(reader.GetAttribute("note"), NumberFormatInfo.InvariantInfo);

            reader.Read();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("time", Time.ToString("F3", NumberFormatInfo.InvariantInfo));
            writer.WriteAttributeString("note", Note.ToString(NumberFormatInfo.InvariantInfo));
        }

        #endregion

        #region ISerializable Implementation

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Note), Note);
            info.AddValue(nameof(Time), Time);
        }

        private Showlight(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            Id = (int)serializationInfo.GetValue(nameof(Id), typeof(int));
            Note = (int)serializationInfo.GetValue(nameof(Note), typeof(int));
            Time = (float)serializationInfo.GetValue(nameof(Time), typeof(float));
        }

        #endregion
    }
}
