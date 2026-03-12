using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace WindowSwitcher;

public partial class MainWindow : Window
{
    private static readonly string[] CircledNumbers =
        ["①", "②", "③", "④", "⑤", "⑥", "⑦", "⑧", "⑨"];

    private readonly DispatcherTimer _timer;
    private PinSettings _pinSettings;
    private GlobalHotkey? _hotkey;
    private List<WindowEntry> _vsCodeEntries = [];
    private List<WindowEntry> _terminalEntries = [];
    private List<string> _unpinnedVsCodeOrder = [];
    private List<nint> _terminalOrder = [];

    public MainWindow()
    {
        InitializeComponent();

        _pinSettings = PinSettings.Load();
        RestoreWindowBounds();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => RefreshList();
        _timer.Start();

        Loaded += OnLoaded;
        RefreshList();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        _hotkey = new GlobalHotkey(handle, OnVsCodeHotkey, OnTerminalHotkey);
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        _hotkey?.Dispose();
        SaveWindowBounds();
        base.OnClosed(e);
    }

    private void RestoreWindowBounds()
    {
        if (_pinSettings.WindowLeft.HasValue) Left = _pinSettings.WindowLeft.Value;
        if (_pinSettings.WindowTop.HasValue) Top = _pinSettings.WindowTop.Value;
        if (_pinSettings.WindowWidth.HasValue) Width = _pinSettings.WindowWidth.Value;
        if (_pinSettings.WindowHeight.HasValue) Height = _pinSettings.WindowHeight.Value;
    }

    private void SaveWindowBounds()
    {
        if (WindowState == WindowState.Normal)
        {
            _pinSettings.WindowLeft = Left;
            _pinSettings.WindowTop = Top;
            _pinSettings.WindowWidth = Width;
            _pinSettings.WindowHeight = Height;
        }
        _pinSettings.Save();
    }

    private void OnVsCodeHotkey(int index)
    {
        if (index < _vsCodeEntries.Count && _vsCodeEntries[index].IsRunning)
            WindowActivator.Activate(_vsCodeEntries[index].Handle);
    }

    private void OnTerminalHotkey(int index)
    {
        if (index < _terminalEntries.Count && _terminalEntries[index].IsRunning)
            WindowActivator.Activate(_terminalEntries[index].Handle);
    }

    private void RefreshList()
    {
        var entries = new List<WindowEntry>();

        // --- VS Code ---
        var running = WindowEnumerator.GetVsCodeWindows();
        var runningByWorkspace = running.ToDictionary(w => w.WorkspaceName, w => w);

        // Pinned first
        foreach (var pinName in _pinSettings.PinnedNames)
        {
            if (runningByWorkspace.TryGetValue(pinName, out var w))
            {
                w.IsPinned = true;
                entries.Add(w);
                runningByWorkspace.Remove(pinName);
            }
            else
            {
                entries.Add(new WindowEntry
                {
                    WorkspaceName = pinName,
                    DisplayName = WindowEnumerator.ShortenName(pinName),
                    IsPinned = true,
                    IsRunning = false,
                });
            }
        }

        // Non-pinned VS Code (stable order)
        _unpinnedVsCodeOrder.RemoveAll(name => !runningByWorkspace.ContainsKey(name));
        foreach (var name in runningByWorkspace.Keys)
        {
            if (!_unpinnedVsCodeOrder.Contains(name))
                _unpinnedVsCodeOrder.Add(name);
        }
        foreach (var name in _unpinnedVsCodeOrder)
        {
            if (runningByWorkspace.TryGetValue(name, out var w))
                entries.Add(w);
        }

        // Assign indices for VS Code entries
        for (int i = 0; i < entries.Count; i++)
            entries[i].Index = i;

        // --- Terminals (handle-based to support duplicate titles) ---
        var terminals = WindowEnumerator.GetTerminalWindows();
        var runningHandles = new HashSet<nint>(terminals.Select(t => t.Handle));

        var terminalEntries = new List<WindowEntry>();

        // Pinned terminals first (by title — all matching windows are pinned)
        var pinnedSet = new HashSet<string>(_pinSettings.PinnedTerminalNames);
        var usedHandles = new HashSet<nint>();

        foreach (var pinName in _pinSettings.PinnedTerminalNames)
        {
            var matched = terminals.Where(t => t.FullTitle == pinName && !usedHandles.Contains(t.Handle)).ToList();
            if (matched.Count > 0)
            {
                foreach (var t in matched)
                {
                    t.IsPinned = true;
                    terminalEntries.Add(t);
                    usedHandles.Add(t.Handle);
                }
            }
            else
            {
                terminalEntries.Add(new WindowEntry
                {
                    WorkspaceName = pinName,
                    DisplayName = pinName,
                    IsPinned = true,
                    IsRunning = false,
                    Category = EntryCategory.Terminal,
                });
            }
        }

        // Non-pinned terminals (stable order by handle)
        var unpinnedTerminals = terminals.Where(t => !usedHandles.Contains(t.Handle)).ToList();
        var unpinnedHandles = new HashSet<nint>(unpinnedTerminals.Select(t => t.Handle));

        _terminalOrder.RemoveAll(h => !unpinnedHandles.Contains(h));
        foreach (var t in unpinnedTerminals)
        {
            if (!_terminalOrder.Contains(t.Handle))
                _terminalOrder.Add(t.Handle);
        }
        var unpinnedByHandle = unpinnedTerminals.ToDictionary(t => t.Handle, t => t);
        foreach (var handle in _terminalOrder)
        {
            if (unpinnedByHandle.TryGetValue(handle, out var t))
                terminalEntries.Add(t);
        }

        // Assign indices for terminal entries (①から)
        for (int i = 0; i < terminalEntries.Count; i++)
            terminalEntries[i].Index = i;

        _vsCodeEntries = entries;
        _terminalEntries = terminalEntries;
        RebuildUI(entries, terminalEntries);
    }

    private void RebuildUI(List<WindowEntry> vsCodeEntries, List<WindowEntry> terminalEntries)
    {
        EntryPanel.Children.Clear();
        EntryPanel.RowDefinitions.Clear();

        var totalRows = vsCodeEntries.Count + (terminalEntries.Count > 0 ? 1 + terminalEntries.Count : 0);
        if (totalRows == 0) return;

        var row = 0;

        // VS Code entries
        foreach (var entry in vsCodeEntries)
        {
            EntryPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var btn = CreateEntryButton(entry, badgeColor: Color.FromRgb(0x00, 0x7A, 0xCC));
            Grid.SetRow(btn, row++);
            EntryPanel.Children.Add(btn);
        }

        // Separator + Terminal entries
        if (terminalEntries.Count > 0)
        {
            // Separator row
            EntryPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
            var sep = new Border
            {
                Height = 2,
                Background = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                Margin = new Thickness(6, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetRow(sep, row++);
            EntryPanel.Children.Add(sep);

            foreach (var entry in terminalEntries)
            {
                EntryPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                var btn = CreateEntryButton(entry, badgeColor: Color.FromRgb(0xCC, 0x44, 0x7A));
                Grid.SetRow(btn, row++);
                EntryPanel.Children.Add(btn);
            }
        }
    }

    private Button CreateEntryButton(WindowEntry entry, Color badgeColor)
    {
        var btn = new Button
        {
            Style = (Style)FindResource(entry.IsRunning ? "EntryButton" : "InactiveButton"),
            Tag = entry,
            VerticalContentAlignment = VerticalAlignment.Center,
        };

        var container = new Grid();

        // Title + pin indicator
        var textPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 18, 0, 0),
        };

        var header = new TextBlock
        {
            Text = entry.DisplayName,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        };

        if (!entry.IsRunning)
            header.Foreground = new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77));

        textPanel.Children.Add(header);

        if (entry.IsPinned)
        {
            var pinText = new TextBlock
            {
                Text = "pinned",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            };
            textPanel.Children.Add(pinText);
        }

        container.Children.Add(textPanel);

        // Number badge (top-left)
        if (entry.Index >= 0 && entry.Index < 9)
        {
            var badge = new Border
            {
                Background = entry.IsRunning
                    ? new SolidColorBrush(badgeColor)
                    : new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 0, 4, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            var badgeText = new TextBlock
            {
                Text = CircledNumbers[entry.Index],
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
            };
            badge.Child = badgeText;
            container.Children.Add(badge);
        }

        btn.Content = container;

        btn.Click += (_, _) =>
        {
            if (entry.IsRunning)
                WindowActivator.Activate(entry.Handle);
        };

        btn.ContextMenu = CreateContextMenu(entry);

        return btn;
    }

    private ContextMenu CreateContextMenu(WindowEntry entry)
    {
        var menu = new ContextMenu();
        var pinList = entry.Category == EntryCategory.Terminal
            ? _pinSettings.PinnedTerminalNames
            : _pinSettings.PinnedNames;

        if (entry.IsPinned)
        {
            var unpin = new MenuItem { Header = "Unpin" };
            unpin.Click += (_, _) =>
            {
                pinList.Remove(entry.WorkspaceName);
                _pinSettings.Save();
                RefreshList();
            };
            menu.Items.Add(unpin);
        }
        else
        {
            var pin = new MenuItem { Header = "Pin" };
            pin.Click += (_, _) =>
            {
                if (!pinList.Contains(entry.WorkspaceName))
                {
                    pinList.Add(entry.WorkspaceName);
                    _pinSettings.Save();
                }
                RefreshList();
            };
            menu.Items.Add(pin);
        }

        return menu;
    }
}
