@tool
extends EditorPlugin

const AUTOLOAD_NAME = "DevConsole"
const AUTOLOAD_PATH = "res://addons/moonbreak_console/DevConsole.cs"

var _panel = null

func _enable_plugin() -> void:
	add_autoload_singleton(AUTOLOAD_NAME, AUTOLOAD_PATH)

func _disable_plugin() -> void:
	remove_autoload_singleton(AUTOLOAD_NAME)

func _enter_tree() -> void:
	set_process_shortcut_input(true)
	_panel = load("res://addons/moonbreak_console/EditorConsolePanel.cs").new()
	_panel.visible = false
	EditorInterface.get_base_control().add_child(_panel)
	_panel.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)

func _exit_tree() -> void:
	if _panel != null:
		_panel.queue_free()
		_panel = null

func _shortcut_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_QUOTELEFT:
			if _panel.visible:
				_panel.hide()
			else:
				_panel.show()
			get_viewport().set_input_as_handled()
