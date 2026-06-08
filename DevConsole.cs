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

        public override void _Ready()
        {
            Instance = this;
            Layer = 128;
            ProcessMode = ProcessModeEnum.Always;

            CommandRegistry.ScanAssemblies();

            _ui = new ConsoleUI();
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

            // Console-specific keys
            switch (key.Keycode)
            {
                case Key.Escape:
                    Close();
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

            // Let printable characters and text editing keys reach the LineEdit.
            // Block everything else (game actions, movement keys, etc).
            bool isTextKey = key.Unicode != 0
                || key.Keycode == Key.Backspace
                || key.Keycode == Key.Delete
                || key.Keycode == Key.Left
                || key.Keycode == Key.Right
                || key.Keycode == Key.Home
                || key.Keycode == Key.End;

            if (!isTextKey) { GetViewport().SetInputAsHandled(); }
        }

        public void Open()
        {
            GetTree().Paused = true;
            _ui.Show();
            _ui.FocusInput();
        }

        public void Close()
        {
            _ui.Hide();
            GetTree().Paused = false;
        }

        public void ExecuteRaw(string input)
        {
            string trimmed = input.Trim();
            if (string.IsNullOrEmpty(trimmed)) { return; }

            string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string commandQuery = parts[0];
            string[] args = parts.Skip(1).ToArray();

            List<CommandEntry> results = CommandRegistry.Query(commandQuery);
            if (results.Count == 0)
            {
                GD.Print($"[Console] Unknown command: {commandQuery}");
                return;
            }

            // Exact name match first, otherwise top fuzzy result
            CommandEntry cmd = results.Find(c => c.Name.ToLowerInvariant() == commandQuery.ToLowerInvariant())
                      ?? results[0];

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
