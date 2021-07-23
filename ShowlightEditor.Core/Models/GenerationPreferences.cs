using ShowLightGenerator;

using XmlUtils;

namespace ShowlightEditor.Core.Models
{
    public sealed class GenerationPreferences
    {
        public bool DisableLasers { get; set; }

        public FogGenerationMethod FogMethod { get; set; }
        public int FogChangeBars { get; set; } = 16;
        public double FogMinTime { get; set; } = 5.0;
        public bool FogRandomize { get; set; }

        public BeamGenerationMethod BeamMethod { get; set; }
        public double BeamMinTime { get; set; } = 0.9;
        public bool BeamCompatibleColors { get; set; }
        public bool BeamRandomize { get; set; }

        public static GenerationPreferences Load(string filename)
        {
            var pref = new GenerationPreferences();
            ReflectionConfig.LoadFromXml(filename, pref);
            return pref;
        }

        public void Save(string filename)
        {
            ReflectionConfig.SaveToXml(filename, this);
        }
    }
}
