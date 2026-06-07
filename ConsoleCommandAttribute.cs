using System;

namespace Moonbreak
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ConsoleCommandAttribute : Attribute
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "General";
        public string Description { get; set; } = "";
    }
}
