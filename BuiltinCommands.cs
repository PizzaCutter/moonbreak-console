using Godot;

namespace Moonbreak
{
    internal static class BuiltinCommands
    {
        [ConsoleCommand(Name = "help", Category = "Console", Description = "Lists all registered commands")]
        private static string Help()
        {
            var lines = new System.Text.StringBuilder();
            foreach (var cmd in CommandRegistry.All)
            {
                lines.AppendLine($"{cmd.Category}.{cmd.Name} — {cmd.Description}");
            }
            return lines.ToString().TrimEnd();
        }

        [ConsoleCommand(Name = "clear", Category = "Console", Description = "Clears the log pane")]
        private static string Clear()
        {
            DevConsole.Instance?.ClearLog();
            return "";
        }

        [ConsoleCommand(Name = "quit", Category = "Console", Description = "Quits the application")]
        private static string Quit()
        {
            DevConsole.Instance?.GetTree().Quit();
            return "Quitting...";
        }

        [ConsoleCommand(Name = "timescale", Category = "Engine", Description = "Sets Engine.TimeScale")]
        private static void Timescale(float value)
        {
            Engine.TimeScale = value;
        }
    }
}
