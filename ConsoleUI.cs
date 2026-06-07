using Godot;
using System.Collections.Generic;

namespace Moonbreak
{
    public partial class ConsoleUI : Control
    {
        private LineEdit _inputBar;
        private VBoxContainer _resultsList;
        private VBoxContainer _logList;
        private ScrollContainer _logScroll;

        public override void _Ready()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            // Darken full screen
            var overlay = new ColorRect
            {
                Color = new Color("#00000088"),
                AnchorRight = 1,
                AnchorBottom = 1,
            };
            AddChild(overlay);

            // Modal panel — centered, 640×480
            var panel = new PanelContainer();
            panel.SetAnchorsPreset(Control.LayoutPreset.Center);
            panel.CustomMinimumSize = new Vector2(640, 480);
            AddChild(panel);

            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride("separation", 4);
            panel.AddChild(vbox);

            // Input bar
            _inputBar = new LineEdit
            {
                PlaceholderText = "Type a command...",
                CustomMinimumSize = new Vector2(0, 32),
            };
            _inputBar.TextChanged += OnInputChanged;
            _inputBar.TextSubmitted += OnInputSubmitted;
            vbox.AddChild(_inputBar);

            // Results list
            var resultsScroll = new ScrollContainer
            {
                CustomMinimumSize = new Vector2(0, 200),
            };
            vbox.AddChild(resultsScroll);

            _resultsList = new VBoxContainer();
            _resultsList.AddThemeConstantOverride("separation", 2);
            resultsScroll.AddChild(_resultsList);

            // Separator
            vbox.AddChild(new HSeparator());

            // Log pane
            _logScroll = new ScrollContainer
            {
                CustomMinimumSize = new Vector2(0, 180),
            };
            vbox.AddChild(_logScroll);

            _logList = new VBoxContainer();
            _logList.AddThemeConstantOverride("separation", 2);
            _logScroll.AddChild(_logList);

            SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

            // Show all commands initially
            RefreshResults("");
        }

        private void OnInputChanged(string text)
        {
            RefreshResults(text);
        }

        private void OnInputSubmitted(string text)
        {
            DevConsole.Instance.ExecuteRaw(text);
            _inputBar.Clear();
            RefreshResults("");
        }

        private void RefreshResults(string query)
        {
            foreach (Node child in _resultsList.GetChildren())
            {
                child.QueueFree();
            }

            var results = CommandRegistry.Query(query);
            foreach (var cmd in results)
            {
                var label = new Label
                {
                    Text = $"{cmd.Category}.{cmd.Name}  —  {cmd.Description}",
                    AutowrapMode = TextServer.AutowrapMode.Off,
                };
                _resultsList.AddChild(label);
            }
        }

        public void AppendLog(string message)
        {
            var label = new Label
            {
                Text = message,
                AutowrapMode = TextServer.AutowrapMode.Word,
            };
            _logList.AddChild(label);

            // Scroll to bottom next frame
            CallDeferred(MethodName.ScrollLogToBottom);
        }

        private void ScrollLogToBottom()
        {
            _logScroll.ScrollVertical = (int)_logScroll.GetVScrollBar().MaxValue;
        }

        public void ClearLog()
        {
            foreach (Node child in _logList.GetChildren())
            {
                child.QueueFree();
            }
        }

        public void FocusInput()
        {
            _inputBar.GrabFocus();
        }
    }
}
