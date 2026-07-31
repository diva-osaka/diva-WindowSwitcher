namespace WindowSwitcher;

public static class WindowGrouping
{
    /// <summary>
    /// 同じワークスペースに属するウィンドウを1つのエントリにまとめる。
    ///
    /// VS Code はタブをドラッグして外に出すと、ソースコードや Markdown プレビューが
    /// 独立したウィンドウになる。それらはワークスペース名が同じであるため、
    /// ワークスペース名をキーにした辞書をそのまま作ると重複キーで例外になる。
    ///
    /// 戻り値は最初に現れた順を保つ。毎秒の再描画でボタンの並びが入れ替わらないようにするため。
    /// また、ワークスペース名が一意になることを保証するので、呼び出し側は安全に辞書化できる。
    /// </summary>
    public static List<WindowEntry> GroupByWorkspace(IEnumerable<WindowEntry> windows)
    {
        var order = new List<string>();
        var groups = new Dictionary<string, List<WindowEntry>>();

        foreach (var window in windows)
        {
            if (!groups.TryGetValue(window.WorkspaceName, out var group))
            {
                group = [];
                groups[window.WorkspaceName] = group;
                order.Add(window.WorkspaceName);
            }
            group.Add(window);
        }

        var result = new List<WindowEntry>(order.Count);
        foreach (var name in order)
        {
            var group = groups[name];
            var head = group[0];

            // 1つしかなければ作り直す必要はない
            if (group.Count == 1)
            {
                result.Add(head);
                continue;
            }

            result.Add(new WindowEntry
            {
                Handles = group.SelectMany(w => w.Handles).ToList(),
                FullTitle = head.FullTitle,
                WorkspaceName = head.WorkspaceName,
                DisplayName = head.DisplayName,
                IsRunning = head.IsRunning,
                IsPinned = head.IsPinned,
                Index = head.Index,
                Category = head.Category,
            });
        }

        return result;
    }
}
