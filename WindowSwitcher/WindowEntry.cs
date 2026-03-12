namespace WindowSwitcher;

public enum EntryCategory { VsCode, Terminal }

public class WindowEntry
{
    public nint Handle { get; init; }
    public string FullTitle { get; init; } = "";
    public string WorkspaceName { get; init; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsRunning { get; set; } = true;
    public bool IsPinned { get; set; }
    public int Index { get; set; } = -1;
    public EntryCategory Category { get; init; } = EntryCategory.VsCode;
}
