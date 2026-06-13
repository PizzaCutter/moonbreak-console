# Moonbreak Console

Developer console for Godot 4 C# (.NET 8). Floating modal overlay with fuzzy command search, available both in-game and in the editor.

![Moonbreak Console demo](example.gif)

## Installation

Add as a git submodule:

```
git submodule add https://github.com/PizzaCutter/moonbreak-console addons/moonbreak_console
```

Enable in Godot: **Project → Project Settings → Plugins → Moonbreak Console → Enable**.

## Usage

Press **backtick** (`` ` ``) to open in either context. Type to fuzzy-search commands. Escape to close.

To override the backtick in-game, define an action named `dev_console_toggle` in Godot's Input Map.

## Keyboard Shortcuts

| Key | Action |
|---|---|
| `` ` `` | Open / close console |
| `Escape` | Close console |
| `Enter` | Execute selected command (or autocomplete if params required) |
| `Tab` | Commit highlighted result into input bar |
| `Up` / `Down` | Move selection through results list |
| `Ctrl+Up` / `Ctrl+Down` | Cycle through command history (in-game only) |
| `Ctrl+C` | Clear the input bar |

## Command Execution

Typing and pressing Enter on a command with no parameters executes it immediately.

For commands with parameters, the first Enter autocompletes `Category.Name ` into the input bar — type your arguments, then Enter again to execute:

```
> Engine.timescale 0.5   ← Enter executes directly once params are present
```

## Command History

Executed commands are saved to `user://console_history.json` and restored across sessions (up to 100 entries). Consecutive duplicates are not stored. Use `Ctrl+Up` / `Ctrl+Down` to navigate. Navigating past the newest entry restores whatever you had typed before browsing.

History is currently in-game only. Editor console history is planned (see Planned Features).

## Fuzzy Search

Two match modes, sorted descending by score:

- **Substring match** — `kil` in `AI.KillAllEnemies` → score 1000+
- **Fuzzy non-contiguous** — `kle` matches `AI.KillAllEnemies` (k…l…e in order) → lower score

Results update on every keypress. Matched characters are highlighted in the results list. Search only applies to `Category.Name` — typing a space switches to parameter entry without affecting the results.

The counter in the top-right of the input bar shows `matched/total` commands.

## Registering Commands

### Static commands

Decorate any `static` method with `[ConsoleCommand]`. Any return type works — `void` produces no output, anything else is printed via `.ToString()`:

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

    [ConsoleCommand(Category = "Debug", Description = "Set timescale")]
    private static void SetTimescale(float value) { Engine.TimeScale = value; }
}
```

No wiring needed. The registry scans all loaded assemblies at startup via reflection.

### Instance commands

Decorate any instance method on a `Node` subclass with `[ConsoleCommand]`. At execute-time the console scans the live scene tree and calls the method on every matching node, printing one result line per instance:

```csharp
public partial class Character : CharacterBody3D
{
    [ConsoleCommand(Description = "Set Health")]
    public void SetHealth(int value)
    {
        Health = Mathf.Clamp(value, 0, MaxHealth);
    }
}
```

### Manual registration

For runtime-dynamic commands, use the escape hatch:

```csharp
CommandRegistry.Register("mycommand", "Category", "Description", args => "output");
```

## Command Context

Commands are tagged with a `Context` that controls which console surfaces they appear in:

| Context | In-game console | Editor console |
|---|---|---|
| `Game` (default) | ✓ | ✗ |
| `Editor` | ✗ | ✓ |
| `Both` | ✓ | ✓ |

```csharp
// Game-only (default — no need to write Context = CommandContext.Game)
[ConsoleCommand(Description = "Set Health")]
public void SetHealth(int value) { ... }

// Editor-only
[ConsoleCommand(Context = CommandContext.Editor, Description = "Save the current scene")]
private static void SaveScene() { EditorInterface.Singleton.SaveScene(); }

// Both
[ConsoleCommand(Context = CommandContext.Both, Description = "List all commands")]
private static string Help() { ... }
```

## Editor Console

The editor console uses the same UI and fuzzy search as the in-game console, overlaid directly on the editor viewport. Press backtick while in the editor (not running the game) to open it.

Editor commands have access to `EditorInterface.Singleton` for interacting with the editor:

```csharp
#if TOOLS
using Godot;
using Moonbreak;

internal static class MyEditorTools
{
    [ConsoleCommand(Name = "SelectNode", Category = "Editor",
        Context = CommandContext.Editor, Description = "Select a node by name")]
    private static void SelectNode(string name)
    {
        var root = EditorInterface.Singleton.GetEditedSceneRoot();
        // ... find and select node
    }
}
#endif
```

### Built-in editor commands

| Command | Description |
|---|---|
| `Editor.ReloadScene` | Reload the currently open scene |
| `Editor.SaveScene` | Save the currently open scene |
| `Editor.PlayScene` | Start playing the current scene |
| `Editor.StopScene` | Stop the running scene |

## Built-in Commands

| Command | Context | Description |
|---|---|---|
| `Console.help` | Both | Lists all registered commands with descriptions |
| `Console.quit` | Game | Quits the application |
| `Engine.timescale <float>` | Game | Sets `Engine.TimeScale` |

## Planned Features

### Editor console history
Command history (Ctrl+Up / Ctrl+Down) for the editor console, matching the in-game behaviour. Saved separately to `user://editor_console_history.json`.

### Enum argument autocomplete
After typing a command name and a space, the suggestion list switches to showing valid values for the next parameter. For `enum` parameters, this means listing all enum members. For `bool`, shows `true`/`false`. Driven by `ParameterInfo` reflection already available on each `CommandEntry`.

## About
This project was built with heavy AI assistance (Claude) as a learning exercise in AI-collaborative development. The goal was figuring out how to work effectively with AI as a programming tool, not just using it as an autocomplete.
