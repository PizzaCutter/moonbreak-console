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

            if (key.Keycode == Key.Escape)
            {
                Close();
                GetViewport().SetInputAsHandled();
            }
        }

        public void Open()
        {
            _ui.Show();
            _ui.FocusInput();
        }

        public void Close()
        {
            _ui.Hide();
        }

        public void ClearLog()
        {
            _ui.ClearLog();
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
                _ui.AppendLog($"Unknown command: {commandQuery}");
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
                    _ui.AppendLog(output);
                    GD.Print($"[Console] {output}");
                }
            }
            catch (Exception e)
            {
                string msg = e.InnerException?.Message ?? e.Message;
                _ui.AppendLog($"Error: {msg}");
                GD.PrintErr($"[Console] {msg}");
            }
        }
    }
}
