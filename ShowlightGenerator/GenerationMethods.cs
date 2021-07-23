namespace ShowLightGenerator
{
    public enum FogGenerationMethod
    {
        SingleColor,
        FromSectionNames,
        FromLowestOctaveNotes,
        FromChords,
        ChangeEveryNthBar,
        MinTimeBetweenChanges
    }

    public enum BeamGenerationMethod
    {
        FollowFogNotes,
        MinTimeBetweenChanges
    }
}
