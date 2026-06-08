#nullable enable
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

        // Palette
        private static readonly Color BgDark     = new Color("#0b0c0d");
        private static readonly Color BgInput    = new Color("#13151a");
        private static readonly Color BgRow      = new Color("#0f1012");
        private static readonly Color BgSelected = new Color("#162a1e");
        private static readonly Color Accent     = new Color("#3ddc84");
        private static readonly Color TextMain   = new Color("#d0d0d0");
        private static readonly Color TextDim    = new Color("#4a4a4a");
        private static readonly Color TextDesc   = new Color("#7a7a7a");
        private static readonly Color Border     = new Color("#222426");
        private static readonly Color MatchColor = new Color("#3ddc84");

        private static StyleBoxFlat Flat(Color bg, int padH = 0, int padV = 0,
            int borderLeft = 0, Color? borderCol = null)
        {
            StyleBoxFlat s = new StyleBoxFlat
            {
                BgColor = bg,
                BorderWidthLeft = borderLeft,
                BorderColor = borderCol ?? Colors.Transparent,
                ContentMarginLeft = padH + borderLeft,
                ContentMarginRight = padH,
                ContentMarginTop = padV,
                ContentMarginBottom = padV,
            };
            return s;
        }

        private static HSeparator MakeSep()
        {
            HSeparator sep = new HSeparator();
            sep.AddThemeColorOverride("color", Border);
            return sep;
        }

        public override void _Ready()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            ColorRect overlay = new ColorRect
            {
                Color = new Color("#000000cc"),
                AnchorRight = 1,
                AnchorBottom = 1,
            };
            AddChild(overlay);

            CenterContainer center = new CenterContainer();
            center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            AddChild(center);

            // Main panel
            PanelContainer panel = new PanelContainer();
            panel.CustomMinimumSize = new Vector2(700, 440);
            StyleBoxFlat panelStyle = new StyleBoxFlat
            {
                BgColor = BgDark,
                BorderWidthLeft = 1, BorderWidthRight = 1,
                BorderWidthTop = 1,  BorderWidthBottom = 1,
                BorderColor = Border,
            };
            panel.AddThemeStyleboxOverride("panel", panelStyle);
            center.AddChild(panel);

            VBoxContainer vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 0);
            panel.AddChild(vbox);

            // Input row
            PanelContainer inputPanel = new PanelContainer();
            inputPanel.AddThemeStyleboxOverride("panel", Flat(BgInput, 10, 6));
            vbox.AddChild(inputPanel);

            HBoxContainer inputRow = new HBoxContainer();
            inputRow.AddThemeConstantOverride("separation", 6);
            inputPanel.AddChild(inputRow);

            Label prompt = new Label
            {
                Text = ">",
                VerticalAlignment = VerticalAlignment.Center,
            };
            prompt.AddThemeColorOverride("font_color", Accent);
            inputRow.AddChild(prompt);

            StyleBoxFlat inputStyle = Flat(Colors.Transparent, 4, 2);
            _inputBar = new LineEdit
            {
                PlaceholderText = "type command...",
                CustomMinimumSize = new Vector2(0, 24),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            _inputBar.AddThemeStyleboxOverride("normal", inputStyle);
            _inputBar.AddThemeStyleboxOverride("focus", inputStyle);
            _inputBar.AddThemeColorOverride("font_color", TextMain);
            _inputBar.AddThemeColorOverride("font_placeholder_color", TextDim);
            _inputBar.AddThemeColorOverride("caret_color", Accent);
            _inputBar.AddThemeColorOverride("selection_color", BgSelected);
            _inputBar.TextChanged += OnInputChanged;
            _inputBar.TextSubmitted += OnInputSubmitted;
            inputRow.AddChild(_inputBar);

            _countLabel = new Label
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                CustomMinimumSize = new Vector2(64, 0),
            };
            _countLabel.AddThemeColorOverride("font_color", TextDim);
            inputRow.AddChild(_countLabel);

            vbox.AddChild(MakeSep());

            // Results list
            ScrollContainer resultsScroll = new ScrollContainer
            {
                CustomMinimumSize = new Vector2(0, 360),
            };
            vbox.AddChild(resultsScroll);

            _resultsList = new VBoxContainer();
            _resultsList.AddThemeConstantOverride("separation", 0);
            _resultsList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
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
                row.AddThemeStyleboxOverride("panel", Flat(BgRow, 12, 4));
                row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
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
                    _rows[i].AddThemeStyleboxOverride("panel", Flat(BgSelected, 12, 4, 2, Accent));
                }
                else
                {
                    _rows[i].AddThemeStyleboxOverride("panel", Flat(BgRow, 12, 4));
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
                seg.AddThemeColorOverride("font_color", isMatch ? MatchColor : TextMain);
                hbox.AddChild(seg);
                i = j;
            }

            string paramHint = BuildParamHint(cmd.Parameters);
            if (!string.IsNullOrEmpty(paramHint))
            {
                Label hint = new Label
                {
                    Text = $" {paramHint}",
                    AutowrapMode = TextServer.AutowrapMode.Off,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                hint.AddThemeColorOverride("font_color", TextDim);
                hbox.AddChild(hint);
            }

            Label desc = new Label
            {
                Text = $"  {cmd.Description}",
                AutowrapMode = TextServer.AutowrapMode.Off,
                VerticalAlignment = VerticalAlignment.Center,
            };
            desc.AddThemeColorOverride("font_color", TextDesc);
            hbox.AddChild(desc);

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

        public string GetInput() => _inputBar.Text;

        public void SetInput(string text)
        {
            _inputBar.Text = text;
            _inputBar.CaretColumn = text.Length;
            int spaceIdx = text.IndexOf(' ');
            _currentQuery = spaceIdx >= 0 ? text[..spaceIdx] : text;
            _selectedIndex = 0;
            RefreshResults(_currentQuery);
        }

        public void FocusInput()
        {
            _inputBar.GrabFocus();
        }
    }
}
