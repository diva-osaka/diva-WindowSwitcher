namespace WindowSwitcher;

public enum EntryCategory { VsCode, Terminal }

public class WindowEntry
{
    /// <summary>
    /// このエントリに属するウィンドウハンドル。
    /// VS Code はタブを外に出すと同じワークスペースで複数のウィンドウを持つため、
    /// 1エントリが複数のハンドルを持ちうる。
    /// </summary>
    public List<nint> Handles { get; init; } = [];

    /// <summary>
    /// 代表となるハンドル。おおむね Z オーダーが最も手前のウィンドウ。
    /// 起動していないエントリ（ピン留めのみ）では 0 になる。
    /// </summary>
    public nint PrimaryHandle => Handles.Count > 0 ? Handles[0] : 0;

    public string FullTitle { get; init; } = "";
    public string WorkspaceName { get; init; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsRunning { get; set; } = true;
    public bool IsPinned { get; set; }
    public int Index { get; set; } = -1;
    public EntryCategory Category { get; init; } = EntryCategory.VsCode;
}
