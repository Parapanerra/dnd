// The toggle numbers are serialized in scene names and old character saves.
// Keep these ranges stable until the persisted toggle IDs are migrated.
internal readonly struct RestToggleRange
{
    internal readonly int First;
    internal readonly int Last;

    internal RestToggleRange(int first, int last)
    {
        First = first;
        Last = last;
    }
}

internal static class RestToggleGroups
{
    internal const string LegacyBloodCursePanelName = "Panel (5)";
    internal static RestToggleRange Exhaustion => new RestToggleRange(0, 5);
    internal static RestToggleRange BloodCurseShortRest => new RestToggleRange(0, 7);
    internal static RestToggleRange BloodCurseLongRestExtra => new RestToggleRange(8, 11);
}