#nullable enable
#if TOOLS
using System;
using System.Collections.Generic;
using Godot;

namespace Moonbreak
{
    [Tool]
    public partial class EditorConsolePanel : Control
    {
        private ConsoleUI _ui = null!;

        public override void _Ready()
        {
            CommandRegistry.ScanAssemblies();

            _ui = new ConsoleUI
            {
                FilterContext = CommandContext.Editor,
                ExecuteAction = ExecuteRaw,
                CloseAction = Hide,
            };
            AddChild(_ui);
            _ui.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

            SetProcessInput(true);
        }

        public override void _Notification(int what)
        {
            if (what == NotificationVisibilityChanged && Visible)
                _ui?.FocusInput();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is not InputEventKey key || !key.Pressed || key.Echo) { return; }

            switch (key.Keycode)
            {
                case Key.Escape:
                    Hide();
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
                case Key.C when key.CtrlPressed:
                    _ui.ClearInput();
                    GetViewport().SetInputAsHandled();
                    return;
            }
        }

        private void ExecuteRaw(string input)
        {
            string trimmed = input.Trim();
            if (string.IsNullOrEmpty(trimmed)) { return; }

            string[] parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string commandQuery = parts[0];
            string[] args = parts[1..];

            List<CommandEntry> results = CommandRegistry.QueryForContext(commandQuery, CommandContext.Editor);
            if (results.Count == 0)
            {
                GD.Print($"[EditorConsole] Unknown command: {commandQuery}");
                return;
            }

            CommandEntry cmd = results.Find(c => c.Name.ToLowerInvariant() == commandQuery.ToLowerInvariant())
                      ?? results[0];

            if (cmd.InstanceMethod != null)
            {
                Node? sceneRoot = EditorInterface.Singleton.GetEditedSceneRoot();
                if (sceneRoot == null)
                {
                    GD.Print("[EditorConsole] No scene open in editor.");
                    return;
                }
                List<Node> matches = CommandRegistry.FindNodesOfType(sceneRoot, cmd.TargetType!);
                if (matches.Count == 0)
                {
                    GD.Print($"[EditorConsole] {cmd.Name}: no instances found.");
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
                        GD.PrintErr($"[{node.Name}] Error: {e.InnerException?.Message ?? e.Message}");
                    }
                }
            }
            else
            {
                try
                {
                    string? output = cmd.Invoke(args);
                    if (!string.IsNullOrEmpty(output)) { GD.Print($"[EditorConsole] {output}"); }
                }
                catch (Exception e)
                {
                    GD.PrintErr($"[EditorConsole] Error: {e.InnerException?.Message ?? e.Message}");
                }
            }
        }

    }
}
#endif
