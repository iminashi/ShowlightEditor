namespace ShowlightEditor.Core.Models
{
    public abstract class GenerationOptionsBase
    {
        public bool ShouldGenerate { get; set; }
        public float MinTimeBetweenNotes { get; set; }
        public bool RandomizeColors { get; set; }
    }

    public sealed class BeamGenerationOptions : GenerationOptionsBase
    {
        public bool UseCompatibleColors { get; set; }
        public BeamGenerationMethod GenerationMethod { get; set; }
    }

    public sealed class FogGenerationOptions : GenerationOptionsBase
    {
        public int ChangeFogColorEveryNthBar { get; set; }
        public int SelectedSingleFogColor { get; set; }
        public FogGenerationMethod GenerationMethod { get; set; }
    }

    public sealed class LaserGenerationOptions
    {
        public bool ShouldGenerate { get; set; }
        public bool DisableLaserLights { get; set; }
    }
}
