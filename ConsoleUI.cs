using Godot;
using System.Collections.Generic;
using System.Reflection;

namespace Moonbreak
{
    public partial class ConsoleUI : Control
    {
        private LineEdit _inputBar = null!;
        private Label _countLabel = null!;
        private VBoxContainer _resultsList = null!;
        private List<CommandEntry> _currentResults = new();
        private List<PanelContainer> _rows = new();
        private int _selectedIndex = 0;
        private string _currentQuery = "";

        private static readonly Color HighlightColor = new Color("#2d5a8e");
        private static readonly Color MatchColor = new Color("#ffcc44");

        public override void _Ready()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            // Darken full screen
            ColorRect overlay = new ColorRect
            {
                Color = new Color("#00000088"),
                AnchorRight = 1,
                AnchorBottom = 1,
            };
            AddChild(overlay);

            // CenterContainer fills viewport and centers the modal
            CenterContainer center = new CenterContainer();
            center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            AddChild(center);

            PanelContainer panel = new PanelContainer();
            panel.CustomMinimumSize = new Vector2(640, 400);
            center.AddChild(panel);

            VBoxContainer vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 4);
            panel.AddChild(vbox);

            HBoxContainer inputRow = new HBoxContainer();
            inputRow.AddThemeConstantOverride("separation", 8);
            vbox.AddChild(inputRow);

            _inputBar = new LineEdit
            {
                PlaceholderText = "Type a command...",
                CustomMinimumSize = new Vector2(0, 32),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _inputBar.TextChanged += OnInputChanged;
            _inputBar.TextSubmitted += OnInputSubmitted;
            inputRow.AddChild(_inputBar);

            _countLabel = new Label
            {
                VerticalAlignment = VerticalAlignment.Center,
                CustomMinimumSize = new Vector2(64, 0),
            };
            inputRow.AddChild(_countLabel);

            ScrollContainer resultsScroll = new ScrollContainer
            {
                CustomMinimumSize = new Vector2(0, 300),
            };
            vbox.AddChild(resultsScroll);

            _resultsList = new VBoxContainer();
            _resultsList.AddThemeConstantOverride("separation", 0);
            resultsScroll.AddChild(_resultsList);

            SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

            RefreshResults("");
        }

        private void OnInputChanged(string text)
        {
            _selectedIndex = 0;
            int spaceIdx = text.IndexOf(' ');
            _currentQuery = spaceIdx >= 0 ? text[..spaceIdx] : text;
            RefreshResults(_currentQuery);
        }

        private void OnInputSubmitted(string text)
        {
            // Enter is intercepted in DevConsole._Input before reaching LineEdit.
            // This handler is a fallback only.
            TryExecuteOrCommit();
        }

        public void TryExecuteOrCommit()
        {
            if (_currentResults.Count == 0 || _selectedIndex >= _currentResults.Count)
            {
                string text = _inputBar.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    DevConsole.Instance.ExecuteRaw(text);
                    DevConsole.Instance.Close();
                }
                return;
            }

            CommandEntry selected = _currentResults[_selectedIndex];
            bool hasParams = _inputBar.Text.Contains(' ');
            if (selected.Parameters.Length == 0 || hasParams)
            {
                string raw = hasParams ? _inputBar.Text.Trim() : selected.Name;
                DevConsole.Instance.ExecuteRaw(raw);
                DevConsole.Instance.Close();
            }
            else
            {
                _inputBar.Text = selected.Name + " ";
                _inputBar.CaretColumn = _inputBar.Text.Length;
            }
        }

        private void RefreshResults(string query)
        {
            foreach (Node child in _resultsList.GetChildren())
            {
                child.QueueFree();
            }
            _rows.Clear();

            _currentResults = CommandRegistry.Query(query);
            _countLabel.Text = $"{_currentResults.Count}/{CommandRegistry.All.Count}";

            for (int i = 0; i < _currentResults.Count; i++)
            {
                CommandEntry cmd = _currentResults[i];

                PanelContainer row = new PanelContainer();
                _resultsList.AddChild(row);
                _rows.Add(row);

                string key = $"{cmd.Category}.{cmd.Name}";
                HashSet<int>? matchPos = FuzzySearch.GetMatchPositions(_currentQuery, key);
                row.AddChild(BuildRowLabel(cmd, key, matchPos));
            }

            UpdateRowStyles();
        }

        private void UpdateRowStyles()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (i == _selectedIndex)
                {
                    StyleBoxFlat style = new StyleBoxFlat { BgColor = HighlightColor };
                    _rows[i].AddThemeStyleboxOverride("panel", style);
                }
                else
                {
                    _rows[i].RemoveThemeStyleboxOverride("panel");
                }
            }
        }

        private HBoxContainer BuildRowLabel(CommandEntry cmd, string key, HashSet<int>? matchPos)
        {
            HBoxContainer hbox = new HBoxContainer();
            hbox.AddThemeConstantOverride("separation", 0);
            hbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            int i = 0;
            while (i < key.Length)
            {
                bool isMatch = matchPos != null && matchPos.Contains(i);
                int j = i + 1;
                while (j < key.Length && (matchPos != null && matchPos.Contains(j)) == isMatch) { j++; }

                Label seg = new Label
                {
                    Text = key[i..j],
                    AutowrapMode = TextServer.AutowrapMode.Off,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                if (isMatch) { seg.AddThemeColorOverride("font_color", MatchColor); }
                hbox.AddChild(seg);
                i = j;
            }

            string paramHint = BuildParamHint(cmd.Parameters);
            string suffix = string.IsNullOrEmpty(paramHint)
                ? $"  —  {cmd.Description}"
                : $" {paramHint}  —  {cmd.Description}";
            hbox.AddChild(new Label { Text = suffix, AutowrapMode = TextServer.AutowrapMode.Off });

            return hbox;
        }

        private string BuildParamHint(ParameterInfo[] parameters)
        {
            if (parameters == null || parameters.Length == 0) { return ""; }
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (ParameterInfo p in parameters)
            {
                sb.Append($"<{p.Name}:{p.ParameterType.Name}> ");
            }
            return sb.ToString().TrimEnd();
        }

        public void MoveSelection(int delta)
        {
            if (_currentResults.Count == 0) { return; }
            _selectedIndex = (_selectedIndex + delta + _currentResults.Count) % _currentResults.Count;
            UpdateRowStyles();
        }

        public void CommitSelection()
        {
            if (_currentResults.Count == 0 || _selectedIndex >= _currentResults.Count) { return; }
            CommandEntry cmd = _currentResults[_selectedIndex];
            _inputBar.Text = cmd.Name;
            _inputBar.CaretColumn = cmd.Name.Length;
            _inputBar.CallDeferred(Control.MethodName.GrabFocus);
        }

        public void ClearInput()
        {
            _inputBar.Text = "";
            _selectedIndex = 0;
            _currentQuery = "";
            RefreshResults("");
        }

        public void FocusInput()
        {
            _inputBar.GrabFocus();
        }
    }
}
