# Moonbreak Console

Telescope-style developer console for Godot 4 C# (.NET 8). Floating modal with fuzzy search. Open-source addon.

## Requirements

- Godot 4.x
- .NET 8 (C# only — no GDScript support)

## Installation

Add as a git submodule inside your project:

```
git submodule add https://github.com/PizzaCutter/moonbreak-console addons/moonbreak_console
```

Then in Godot: **Project → Project Settings → Plugins → Moonbreak Console → Enable**.

## Usage

Press **backtick** to open. Type to fuzzy-search commands. Enter to execute. Escape to close.

Define a custom toggle action named `dev_console_toggle` in Godot's Input Map to override the backtick default.

## Registering Commands

Decorate any `static` method that returns `string` with `[ConsoleCommand]`:

```csharp
using Moonbreak;

public static class Cheats
{
    [ConsoleCommand(Category = "AI", Description = "Kill all enemies instantly")]
    private static string KillAllEnemies()
    {
        GameManager.Instance.KillAllEnemies();
        return "All enemies killed";
    }
}
```

No wiring needed. The registry scans all loaded assemblies at startup via reflection.

For runtime-dynamic commands, use the escape hatch:

```csharp
CommandRegistry.Register("mycommand", "Category", "Description", () => {
    return "output";
});
```

## Architecture

### 3 layers

**Pure logic (no Godot dependency)**

| File | Role |
|---|---|
| `ConsoleCommandAttribute.cs` | Attribute that decorates static methods. Stores `Name`, `Category`, `Description`. |
| `FuzzySearch.cs` | Single static `Score(query, candidate)` method. Substring hits score 1000+, fuzzy non-contiguous hits score lower, no match returns -1. |
| `CommandRegistry.cs` | Scans assemblies at startup, wraps tagged methods as `CommandEntry` delegates. `Query(input)` returns fuzzy-ranked results. |

**Godot runtime**

| File | Role |
|---|---|
| `DevConsole.cs` | Autoload singleton (`CanvasLayer`, layer 128). Handles backtick input, owns `ConsoleUI`, calls `ExecuteRaw()` which queries registry and dispatches. |
| `ConsoleUI.cs` | UI built in pure C# (no `.tscn`). `LineEdit` → results list → log pane. `TextChanged` refreshes results, `TextSubmitted` executes. |
| `BuiltinCommands.cs` | `help`, `clear`, `quit`, `timescale` — registered via the same `[ConsoleCommand]` attribute as user commands. |

**Godot editor**

| File | Role |
|---|---|
| `plugin.cfg` | Addon metadata. Points to `plugin.gd`. |
| `plugin.gd` | 7-line EditorPlugin. `_enable_plugin` registers the `DevConsole` autoload. `_disable_plugin` removes it. Only GDScript in the project. |

### Data flow

```
LineEdit.TextSubmitted
  → DevConsole.ExecuteRaw(input)
  → CommandRegistry.Query(input)    ← fuzzy scores all commands
  → cmd.Invoke()                    ← calls your static method
  → ConsoleUI.AppendLog(output)     ← shows in log pane
  → GD.Print(output)                ← mirrors to Godot output
```

## Built-in Commands

| Command | Behavior |
|---|---|
| `help` | Lists all registered commands with descriptions |
| `clear` | Clears the log pane |
| `quit` | Quits the application |
| `timescale` | Sets `Engine.TimeScale` (shows usage for now) |

## v0.1 Scope

Out of scope for v0.1: preview pane, per-argument autocomplete, GDScript support, command history persistence, collapsible log pane, in-game rebinding UI.

## Future Work

- **Editor-time console** — currently runtime-only. Editor support requires `@tool` annotation, hooking into `EditorInterface` viewport input, and a separate UI layer inside the editor viewport. `CanvasLayer` and `_Ready`-based scanning don't run in editor context.
- **Command arguments** — commands are currently no-arg `Func<string>`. Argument support (e.g. `timescale 0.5`) needs input parsing and a second delegate signature.
- **Per-argument autocomplete / type hints**
- **Command history** — recall previous commands with arrow keys, persisted across sessions.
- **Collapsible log pane**
- **In-game keybinding UI**
- **Preview pane**
