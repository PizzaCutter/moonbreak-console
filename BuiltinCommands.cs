using Godot;

namespace Moonbreak
{
    internal static class BuiltinCommands
    {
        [ConsoleCommand(Name = "help", Category = "Console", Description = "Lists all registered commands")]
        private static string Help()
        {
            System.Text.StringBuilder lines = new System.Text.StringBuilder();
            foreach (CommandEntry cmd in CommandRegistry.All)
            {
                lines.AppendLine($"{cmd.Category}.{cmd.Name} — {cmd.Description}");
            }
            return lines.ToString().TrimEnd();
        }

        [ConsoleCommand(Name = "quit", Category = "Console", Description = "Quits the application")]
        private static void Quit()
        {
            DevConsole.Instance?.GetTree().Quit();
        }

        [ConsoleCommand(Name = "timescale", Category = "Engine", Description = "Sets Engine.TimeScale")]
        private static void Timescale(float value)
        {
            Engine.TimeScale = value;
        }
    }
}
