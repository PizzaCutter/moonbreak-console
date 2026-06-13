#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Godot;

namespace Moonbreak
{
    public class CommandEntry
    {
        public string Name = null!;
        public string Category = null!;
        public string Description = null!;
        public ParameterInfo[] Parameters = null!;
        public Func<string[], string?> Invoke = null!;
        public MethodInfo? InstanceMethod;
        public Type? TargetType;
        public CommandContext Context;
    }

    public static class CommandRegistry
    {
        private static readonly List<CommandEntry> _commands = new();

        public static IReadOnlyList<CommandEntry> All => _commands;

        public static IReadOnlyList<CommandEntry> AllForContext(CommandContext ctx)
            => _commands.Where(c => c.Context == ctx || c.Context == CommandContext.Both).ToList();

        public static List<CommandEntry> QueryForContext(string input, CommandContext ctx)
        {
            IReadOnlyList<CommandEntry> pool = AllForContext(ctx);
            if (string.IsNullOrEmpty(input)) { return pool.ToList(); }
            return pool
                .Select(cmd => (cmd, score: FuzzySearch.Score(input, $"{cmd.Category}.{cmd.Name}")))
                .Where(x => x.score >= 0)
                .OrderByDescending(x => x.score)
                .Select(x => x.cmd)
                .ToList();
        }

        public static void ScanAssemblies()
        {
            _commands.Clear();

            Stopwatch sw = Stopwatch.StartNew();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        ConsoleCommandAttribute? attr = method.GetCustomAttribute<ConsoleCommandAttribute>();
                        if (attr == null) { continue; }

                        string name = string.IsNullOrEmpty(attr.Name)
                            ? method.Name
                            : attr.Name;

                        ParameterInfo[] parameters = method.GetParameters();
                        bool isVoid = method.ReturnType == typeof(void);

                        Func<string[], string?> invoke = args =>
                        {
                            object?[] converted = ConvertArgs(args, parameters);
                            if (isVoid) { method.Invoke(null, converted); return null; }
                            return method.Invoke(null, converted)?.ToString();
                        };

                        _commands.Add(new CommandEntry
                        {
                            Name = name,
                            Category = attr.Category,
                            Description = attr.Description,
                            Parameters = parameters,
                            Invoke = invoke,
                            Context = attr.Context,
                        });
                    }

                    foreach (MethodInfo method in type.GetMethods(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        ConsoleCommandAttribute? attr = method.GetCustomAttribute<ConsoleCommandAttribute>();
                        if (attr == null) { continue; }

                        string name = string.IsNullOrEmpty(attr.Name) ? method.Name : attr.Name;
                        string category = string.IsNullOrEmpty(attr.Category) ? type.Name : attr.Category;

                        _commands.Add(new CommandEntry
                        {
                            Name = name,
                            Category = category,
                            Description = attr.Description,
                            Parameters = method.GetParameters(),
                            InstanceMethod = method,
                            TargetType = type,
                            Invoke = null!,
                            Context = attr.Context,
                        });
                    }
                }
            }

            sw.Stop();
            GD.Print($"[Moonbreak] Scanned {_commands.Count} commands in {sw.ElapsedMilliseconds}ms");
        }

        public static void Register(string name, string category, string description, Func<string[], string?> invoke)
        {
            _commands.Add(new CommandEntry
            {
                Name = name,
                Category = category,
                Description = description,
                Parameters = Array.Empty<ParameterInfo>(),
                Invoke = invoke,
            });
        }

        public static List<CommandEntry> Query(string input)
        {
            if (string.IsNullOrEmpty(input)) { return _commands.ToList(); }

            return _commands
                .Select(cmd => (cmd, score: FuzzySearch.Score(input, $"{cmd.Category}.{cmd.Name}")))
                .Where(x => x.score >= 0)
                .OrderByDescending(x => x.score)
                .Select(x => x.cmd)
                .ToList();
        }

        public static List<Node> FindNodesOfType(Node root, Type targetType)
        {
            var results = new List<Node>();
            if (targetType.IsInstanceOfType(root)) { results.Add(root); }
            foreach (Node child in root.GetChildren())
            {
                results.AddRange(FindNodesOfType(child, targetType));
            }
            return results;
        }

        internal static object?[] ConvertArgs(string[] args, ParameterInfo[] parameters)
        {
            object?[] result = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i < args.Length)
                {
                    result[i] = Convert.ChangeType(args[i], parameters[i].ParameterType);
                }
                else if (parameters[i].HasDefaultValue)
                {
                    result[i] = parameters[i].DefaultValue;
                }
                else
                {
                    throw new ArgumentException($"Missing argument: {parameters[i].Name} ({parameters[i].ParameterType.Name})");
                }
            }
            return result;
        }
    }
}
