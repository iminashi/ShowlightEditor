using ReactiveUI;

using Rocksmith2014.XML;

using ShowLightGenerator;

using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace ShowlightEditor.Core.ViewModels
{
    public sealed class ShowLightViewModel : ReactiveObject, IComparable<ShowLightViewModel>, IComparable, IEquatable<ShowLightViewModel>, ISerializable
    {
        public static ShowLightType GetShowlightType(int note)
        {
            if (note >= ShowLight.FogMin && note <= ShowLight.FogMax)
                return ShowLightType.Fog;

            if (note == ShowLight.BeamOff || (note >= ShowLight.BeamMin && note <= ShowLight.BeamMax))
                return ShowLightType.Beam;

            if (note == ShowLight.LasersOn || note == ShowLight.LasersOff)
                return ShowLightType.Laser;

            return ShowLightType.Undefined;
        }

        private static int _id;

        public int Id { get; }

        public ShowLight Model { get; }

        public int Time
        {
            get => Model.Time;
            set
            {
                Model.Time = value;
                this.RaisePropertyChanged(nameof(Time));
            }
        }

        public float TimeSeconds => Model.Time / 1000f;

        public byte Note
        {
            get => Model.Note;
            set
            {
                Model.Note = value;
                ShowlightType = Model.GetShowLightType();
                this.RaisePropertyChanged(nameof(Note));
            }
        }

        public ShowLightType ShowlightType { get; private set; }

        public ShowLightViewModel()
        {
            Id = _id++;
            Model = new ShowLight();
        }

        public ShowLightViewModel(ShowLight model)
        {
            Id = _id++;
            Model = model;
        }

        public ShowLightViewModel(byte note, int time)
        {
            Id = _id++;
            Model = new ShowLight(time, note);
        }

        public ShowLightViewModel(ShowLightViewModel other)
        {
            Id = _id++;
            Model = new ShowLight(other.Time, other.Note);
        }

        #region IComparable<Showlight>, IEquatable<Showlight>, overrides, operators

        public int CompareTo(ShowLightViewModel other)
        {
            if (other is null)
                return 1;

            return Time.CompareTo(other.Time);
        }

        public int CompareTo(object obj)
        {
            if (obj is null)
            {
                return 1;
            }

            if (obj is ShowLightViewModel x)
            {
                return CompareTo(x);
            }

            throw new ArgumentException("Incorrect type, expected ShowLightViewModel.", nameof(obj));
        }

        public bool Equals(ShowLightViewModel other)
        {
            if (other is null)
                return false;

            return Note == other.Note && Time == other.Time;
        }

        public override bool Equals(object obj)
            => obj is ShowLightViewModel other && Equals(other);

        public override int GetHashCode()
            => Id;

        public override string ToString()
            => $"ID: {Id}, Time: {Time.ToString("F3", NumberFormatInfo.InvariantInfo)}, Note: {Note} ({ShowlightType})";

        public static bool operator ==(ShowLightViewModel left, ShowLightViewModel right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(ShowLightViewModel left, ShowLightViewModel right)
        {
            return !(left == right);
        }

        public static bool operator <(ShowLightViewModel left, ShowLightViewModel right)
            => left.CompareTo(right) < 0;

        public static bool operator <=(ShowLightViewModel left, ShowLightViewModel right)
            => left.CompareTo(right) <= 0;

        public static bool operator >(ShowLightViewModel left, ShowLightViewModel right)
            => left.CompareTo(right) > 0;

        public static bool operator >=(ShowLightViewModel left, ShowLightViewModel right)
            => left.CompareTo(right) >= 0;

        #endregion IComparable<Showlight>, IEquatable<Showlight>, overrides, operators

        #region ISerializable Implementation

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Note), Note);
            info.AddValue(nameof(Time), Time);
        }

        private ShowLightViewModel(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            Id = (int)serializationInfo.GetValue(nameof(Id), typeof(int));
            Note = (byte)serializationInfo.GetValue(nameof(Note), typeof(byte));
            Time = (int)serializationInfo.GetValue(nameof(Time), typeof(int));
        }

        #endregion ISerializable Implementation
    }
}
