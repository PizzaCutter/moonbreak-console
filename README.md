# Moonbreak Console

Developer console for Godot 4 C# (.NET 8). Floating modal overlay with fuzzy command search.

## Requirements

- Godot 4.x
- .NET 8 — C# only, no GDScript support (accepted tradeoff)

## Installation

Add as a git submodule:

```
git submodule add https://github.com/PizzaCutter/moonbreak-console addons/moonbreak_console
```

Enable in Godot: **Project → Project Settings → Plugins → Moonbreak Console → Enable**.

## Usage

Press **backtick** to open. Type to fuzzy-search commands. Enter to execute. Escape to close.

To override the backtick, define an action named `dev_console_toggle` in Godot's Input Map.

## Registering Commands

Decorate any `static` method with `[ConsoleCommand]`. Any return type works — `void` produces no log output, anything else is converted via `.ToString()`:

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

    [ConsoleCommand(Category = "Debug", Description = "Toggle god mode")]
    private static void ToggleGodMode() { ... }

    [ConsoleCommand(Category = "Debug", Description = "Get enemy count")]
    private static int GetEnemyCount() { return 5; }
}
```

No wiring needed. The registry scans all loaded assemblies at startup via reflection.

Methods can take parameters — parsed from the console input by type:

```csharp
[ConsoleCommand(Category = "Engine", Description = "Set timescale")]
private static void SetTimescale(float value) { Engine.TimeScale = value; }
```

For runtime-dynamic commands, use the manual escape hatch:

```csharp
CommandRegistry.Register("mycommand", "Category", "Description", args => "output");
```

## Fuzzy Search

Two match modes, sorted descending by score:

- **Substring match** — `kil` in `KillAllEnemies` → score 1000+
- **Fuzzy non-contiguous** — `kle` matches `KillAllEnemies` (k…l…e in order) → lower score

Results list updates on every keypress.

## Built-in Commands

| Command | Behavior |
|---|---|
| `help` | Lists all registered commands with descriptions |
| `clear` | Clears the log pane |
| `quit` | Quits the application |
| `timescale <float>` | Sets `Engine.TimeScale` |

## Architecture

### 3 layers

**Pure logic — no Godot dependency**

| File | Role |
|---|---|
| `ConsoleCommandAttribute.cs` | Attribute that decorates static methods. Stores `Name`, `Category`, `Description`. |
| `FuzzySearch.cs` | `Score(query, candidate)` — substring hits score 1000+, fuzzy hits score lower, no match returns -1. |
| `CommandRegistry.cs` | Scans assemblies at startup, wraps tagged methods as `CommandEntry` delegates. `Query(input)` returns fuzzy-ranked results. |

**Godot runtime**

| File | Role |
|---|---|
| `DevConsole.cs` | Autoload singleton (`CanvasLayer`, layer 128). Handles toggle input, owns `ConsoleUI`, dispatches `ExecuteRaw()`. |
| `ConsoleUI.cs` | UI built in pure C# — no `.tscn`. `LineEdit` → results list → log pane. |
| `BuiltinCommands.cs` | `help`, `clear`, `quit`, `timescale` — registered via `[ConsoleCommand]` like any user command. |

**Godot editor**

| File | Role |
|---|---|
| `plugin.cfg` | Addon metadata. Points to `plugin.gd`. |
| `plugin.gd` | EditorPlugin — `_enable_plugin` registers `DevConsole` autoload, `_disable_plugin` removes it. |

### Data flow

```
LineEdit.TextSubmitted
  → DevConsole.ExecuteRaw(input)
  → split → commandQuery + args[]
  → CommandRegistry.Query(commandQuery)   ← fuzzy scores all commands
  → ConvertArgs(args, parameters)         ← "0.5" → 0.5f etc.
  → cmd.Invoke(args)                      ← calls your static method
  → ConsoleUI.AppendLog(output)           ← shows in log pane
  → GD.Print(output)                      ← mirrors to Godot output
```

## Todo

- **Remove log pane** — output goes to `GD.Print` only.
- **Fix centering** — modal is fixed size, centered in viewport.
- **Row highlight** — selected result row gets background color highlight.
- **Tab to commit** — Tab key writes selected command name into input bar (replacing current text) so user can append arguments.
- **Up/Down navigation** — arrow keys move selection through results list, wraps around.
- **Input blocking** — while console open, `DevConsole._Input()` calls `SetInputAsHandled()` for all non-console keys so input doesn't bleed to game.
- **Ghost text** — deferred. Godot `LineEdit` doesn't support inline mixed-color text natively.

## Future Work

- **Editor-time console** — runtime-only. Editor support needs `@tool`, `EditorInterface` viewport input, and a separate UI layer. `CanvasLayer` and `_Ready`-based scanning don't run in editor context.
- **Command history** — recall previous commands with arrow keys, persisted across sessions.
- **Preview pane**
