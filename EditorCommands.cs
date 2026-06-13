#nullable enable
#if TOOLS
using Godot;

namespace Moonbreak
{
    internal static class EditorCommands
    {
        [ConsoleCommand(Name = "ReloadScene", Category = "Editor", Context = CommandContext.Editor,
            Description = "Reload the currently open scene")]
        private static void ReloadScene()
        {
            Node? root = EditorInterface.Singleton.GetEditedSceneRoot();
            if (root == null) { GD.Print("[EditorConsole] No scene open."); return; }
            EditorInterface.Singleton.ReloadSceneFromPath(root.SceneFilePath);
        }

        [ConsoleCommand(Name = "SaveScene", Category = "Editor", Context = CommandContext.Editor,
            Description = "Save the currently open scene")]
        private static void SaveScene()
        {
            EditorInterface.Singleton.SaveScene();
        }

        [ConsoleCommand(Name = "PlayScene", Category = "Editor", Context = CommandContext.Editor,
            Description = "Play the current scene")]
        private static void PlayScene()
        {
            EditorInterface.Singleton.PlayCurrentScene();
        }

        [ConsoleCommand(Name = "StopScene", Category = "Editor", Context = CommandContext.Editor,
            Description = "Stop the running scene")]
        private static void StopScene()
        {
            EditorInterface.Singleton.StopPlayingScene();
        }

    }
}
#endif
