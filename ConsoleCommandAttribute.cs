using System;

namespace Moonbreak
{
    public enum CommandContext { Game, Editor, Both }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ConsoleCommandAttribute : Attribute
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "General";
        public string Description { get; set; } = "";
        public CommandContext Context { get; set; } = CommandContext.Game;
    }
}
