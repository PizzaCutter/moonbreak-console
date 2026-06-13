#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Moonbreak
{
    public partial class DevConsole : CanvasLayer
    {
        public static DevConsole Instance { get; private set; } = null!;

        private ConsoleUI _ui = null!;

        private readonly List<string> _history = new();
        private int _historyIndex = -1;
        private string _savedInput = "";

        private const int MaxHistory = 100;
        private const string HistoryPath = "user://console_history.json";

        public override void _Ready()
        {
            Instance = this;
            Layer = 128;
            ProcessMode = ProcessModeEnum.Always;

            CommandRegistry.ScanAssemblies();
            LoadHistory();

            _ui = new ConsoleUI { FilterContext = CommandContext.Game };
            AddChild(_ui);
            _ui.Hide();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is not InputEventKey key || !key.Pressed || key.Echo) { return; }

            bool toggle = InputMap.HasAction("dev_console_toggle")
                ? Input.IsActionJustPressed("dev_console_toggle")
                : key.Keycode == Key.Quoteleft;

            if (toggle)
            {
                if (_ui.Visible) { Close(); } else { Open(); }
                GetViewport().SetInputAsHandled();
                return;
            }

            if (!_ui.Visible) { return; }

            switch (key.Keycode)
            {
                case Key.Escape:
                    Close();
                    GetViewport().SetInputAsHandled();
                    return;
                case Key.Up when key.CtrlPressed:
                    NavigateHistory(-1);
                    GetViewport().SetInputAsHandled();
                    return;
                case Key.Down when key.CtrlPressed:
                    NavigateHistory(1);
                    GetViewport().SetInputAsHandled();
                    return;
                case Key.Up:
                    _ui.MoveSelection(-1);
                    GetViewport().SetInputAsHandled();
                    return;
                case Key.Down:
                    _ui.MoveSelection(1);
                    GetViewport().SetInputAsHandled();
                    return;
                case Key.Tab:
                    _ui.CommitSelection();
                    GetViewport().SetInputAsHandled();
                    return;
                case Key.Enter:
                case Key.KpEnter:
                    _ui.TryExecuteOrCommit();
                    GetViewport().SetInputAsHandled();
                    return;
                case Key.C when key.CtrlPressed:
                    _ui.ClearInput();
                    GetViewport().SetInputAsHandled();
                    return;
            }

            bool isTextKey = key.Unicode != 0
                || key.Keycode == Key.Backspace
                || key.Keycode == Key.Delete
                || key.Keycode == Key.Left
                || key.Keycode == Key.Right
                || key.Keycode == Key.Home
                || key.Keycode == Key.End;

            if (!isTextKey) { GetViewport().SetInputAsHandled(); }
        }

        private void NavigateHistory(int delta)
        {
            if (_history.Count == 0) { return; }

            if (delta < 0) // Ctrl+Up — older
            {
                if (_historyIndex == -1)
                {
                    _savedInput = _ui.GetInput();
                    _historyIndex = _history.Count - 1;
                }
                else
                {
                    _historyIndex = Mathf.Max(0, _historyIndex - 1);
                }
                _ui.SetInput(_history[_historyIndex]);
            }
            else // Ctrl+Down — newer
            {
                if (_historyIndex == -1) { return; }
                _historyIndex++;
                if (_historyIndex >= _history.Count)
                {
                    _historyIndex = -1;
                    _ui.SetInput(_savedInput);
                }
                else
                {
                    _ui.SetInput(_history[_historyIndex]);
                }
            }
        }

        public void Open()
        {
            _historyIndex = -1;
            _savedInput = "";
            GetTree().Paused = true;
            _ui.Show();
            _ui.FocusInput();
        }

        public void Close()
        {
            _ui.Hide();
            GetTree().Paused = false;
        }

        private void SaveHistory()
        {
            using FileAccess file = FileAccess.Open(HistoryPath, FileAccess.ModeFlags.Write);
            if (file == null) { return; }
            file.StoreString(Json.Stringify(_history.ToArray()));
        }

        private void LoadHistory()
        {
            if (!FileAccess.FileExists(HistoryPath)) { return; }
            using FileAccess file = FileAccess.Open(HistoryPath, FileAccess.ModeFlags.Read);
            if (file == null) { return; }
            Godot.Collections.Array parsed = Json.ParseString(file.GetAsText()).AsGodotArray();
            foreach (Godot.Variant entry in parsed)
            {
                _history.Add(entry.AsString());
            }
        }

        public void ExecuteRaw(string input)
        {
            string trimmed = input.Trim();
            if (string.IsNullOrEmpty(trimmed)) { return; }

            if (_history.Count == 0 || _history[^1] != trimmed)
            {
                _history.Add(trimmed);
                if (_history.Count > MaxHistory) { _history.RemoveAt(0); }
                SaveHistory();
            }

            string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string commandQuery = parts[0];
            string[] args = parts.Skip(1).ToArray();

            List<CommandEntry> results = CommandRegistry.QueryForContext(commandQuery, CommandContext.Game);
            if (results.Count == 0)
            {
                GD.Print($"[Console] Unknown command: {commandQuery}");
                return;
            }

            CommandEntry cmd = results.Find(c => c.Name.ToLowerInvariant() == commandQuery.ToLowerInvariant())
                      ?? results[0];

            if (cmd.InstanceMethod != null)
            {
                List<Node> matches = CommandRegistry.FindNodesOfType(GetTree().Root, cmd.TargetType!);
                if (matches.Count == 0)
                {
                    GD.Print($"[Console] {cmd.Name}: no instances found.");
                    return;
                }
                foreach (Node node in matches)
                {
                    try
                    {
                        object?[] converted = CommandRegistry.ConvertArgs(args, cmd.InstanceMethod.GetParameters());
                        object? result = cmd.InstanceMethod.Invoke(node, converted);
                        string? output = result?.ToString();
                        GD.Print(string.IsNullOrEmpty(output) ? $"[{node.Name}] ok" : $"[{node.Name}] {output}");
                    }
                    catch (Exception e)
                    {
                        string msg = e.InnerException?.Message ?? e.Message;
                        GD.PrintErr($"[{node.Name}] Error: {msg}");
                    }
                }
            }
            else
            {
                try
                {
                    string? output = cmd.Invoke(args);
                    if (!string.IsNullOrEmpty(output))
                    {
                        GD.Print($"[Console] {output}");
                    }
                }
                catch (Exception e)
                {
                    string msg = e.InnerException?.Message ?? e.Message;
                    GD.PrintErr($"[Console] Error: {msg}");
                }
            }
        }

    }
}
