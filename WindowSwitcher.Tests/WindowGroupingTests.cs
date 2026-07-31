using WindowSwitcher;
using Xunit;

namespace WindowSwitcher.Tests;

public class WindowGroupingTests
{
    private static WindowEntry Window(nint handle, string workspace) => new()
    {
        Handles = [handle],
        FullTitle = $"file.cs - {workspace} - Visual Studio Code",
        WorkspaceName = workspace,
        DisplayName = workspace,
    };

    [Fact]
    public void ワークスペースが1つずつなら件数は変わらない()
    {
        var windows = new List<WindowEntry>
        {
            Window(1, "devsys-Alpha"),
            Window(2, "devsys-Bravo"),
        };

        var grouped = WindowGrouping.GroupByWorkspace(windows);

        Assert.Equal(2, grouped.Count);
        Assert.Equal([(nint)1], grouped[0].Handles);
        Assert.Equal([(nint)2], grouped[1].Handles);
    }

    [Fact]
    public void 同じワークスペースの複数ウィンドウは1エントリにまとめられる()
    {
        // VS Code でタブを外に出すと同一ワークスペースのウィンドウが増える。
        // 以前はここで ToDictionary が重複キー例外を投げてアプリが落ちていた。
        var windows = new List<WindowEntry>
        {
            Window(10, "devsys-Alpha"),
            Window(11, "devsys-Alpha"),
            Window(12, "devsys-Alpha"),
        };

        var grouped = WindowGrouping.GroupByWorkspace(windows);

        var entry = Assert.Single(grouped);
        Assert.Equal([(nint)10, (nint)11, (nint)12], entry.Handles);
    }

    [Fact]
    public void 重複があっても例外を投げない()
    {
        var windows = new List<WindowEntry>
        {
            Window(1, "same"),
            Window(2, "same"),
        };

        var ex = Record.Exception(() => { WindowGrouping.GroupByWorkspace(windows); });

        Assert.Null(ex);
    }

    [Fact]
    public void 代表ハンドルは最初に見つかったウィンドウ()
    {
        // EnumWindows はおおむね Z オーダー順に返すため、先頭が最前面のウィンドウ。
        var windows = new List<WindowEntry>
        {
            Window(100, "devsys-Alpha"),
            Window(200, "devsys-Alpha"),
        };

        var grouped = WindowGrouping.GroupByWorkspace(windows);

        Assert.Equal(100, grouped[0].PrimaryHandle);
    }

    [Fact]
    public void まとめたエントリは表示名などの属性を引き継ぐ()
    {
        var windows = new List<WindowEntry>
        {
            new()
            {
                Handles = [1],
                FullTitle = "a.cs - devsys-Alpha - Visual Studio Code",
                WorkspaceName = "devsys-Alpha",
                DisplayName = "Alpha",
            },
            new()
            {
                Handles = [2],
                FullTitle = "b.md - devsys-Alpha - Visual Studio Code",
                WorkspaceName = "devsys-Alpha",
                DisplayName = "Alpha",
            },
        };

        var entry = WindowGrouping.GroupByWorkspace(windows)[0];

        Assert.Equal("devsys-Alpha", entry.WorkspaceName);
        Assert.Equal("Alpha", entry.DisplayName);
        Assert.True(entry.IsRunning);
        Assert.Equal(EntryCategory.VsCode, entry.Category);
    }

    [Fact]
    public void 空のリストは空になる()
    {
        var grouped = WindowGrouping.GroupByWorkspace([]);

        Assert.Empty(grouped);
    }

    [Fact]
    public void 並び順は最初に現れた順を保つ()
    {
        // 並び順が変わると画面上のボタン位置が毎秒入れ替わってしまうため、
        // 最初に見つかった順を維持することが重要。
        var windows = new List<WindowEntry>
        {
            Window(1, "Charlie"),
            Window(2, "Alpha"),
            Window(3, "Charlie"),
            Window(4, "Bravo"),
        };

        var grouped = WindowGrouping.GroupByWorkspace(windows);

        Assert.Equal(["Charlie", "Alpha", "Bravo"], grouped.Select(e => e.WorkspaceName));
    }

    [Fact]
    public void まとめた結果はワークスペース名で一意になる()
    {
        // 呼び出し側がこの結果から辞書を作るため、一意性が保証されている必要がある。
        var windows = new List<WindowEntry>
        {
            Window(1, "Alpha"),
            Window(2, "Bravo"),
            Window(3, "Alpha"),
            Window(4, "Bravo"),
            Window(5, "Alpha"),
        };

        var grouped = WindowGrouping.GroupByWorkspace(windows);

        var ex = Record.Exception(() => { grouped.ToDictionary(e => e.WorkspaceName); });
        Assert.Null(ex);
        Assert.Equal(2, grouped.Count);
    }
}

public class WindowEntryTests
{
    [Fact]
    public void ハンドルが無いエントリの代表ハンドルはゼロ()
    {
        // ピン留めされているが起動していないエントリはハンドルを持たない。
        var entry = new WindowEntry { WorkspaceName = "not-running", IsRunning = false };

        Assert.Equal(0, entry.PrimaryHandle);
    }

    [Fact]
    public void 単一ウィンドウのエントリは代表ハンドルがそのハンドルになる()
    {
        var entry = new WindowEntry { Handles = [42] };

        Assert.Equal(42, entry.PrimaryHandle);
    }
}
