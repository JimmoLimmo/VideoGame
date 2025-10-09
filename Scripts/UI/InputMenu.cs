using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;

public partial class InputMenu : Control {
	private Button _backButton;
	private Button _resetButton;
	private InputManager _inputManager;

	public override void _Ready() {
		FocusMode = FocusModeEnum.All;
		MouseFilter = MouseFilterEnum.Stop;

		_inputManager = GetNode<InputManager>("/root/InputManager");
		_backButton = GetNode<Button>("CenterContainer/VBoxContainer/BackButton");
		_resetButton = GetNode<Button>("CenterContainer/VBoxContainer/ResetButton");

		_backButton.Pressed += OnBackPressed;
		_resetButton.Pressed += OnResetPressed;

		_inputManager.Connect(InputManager.SignalName.BindingsUpdated, new Callable(this, nameof(OnBindingsUpdated)));

		UpdateButtonLabels();
		CallDeferred(nameof(RegrabFocus));
	}

	private void RegrabFocus() {
		_backButton?.GrabFocus();
		GD.Print("[InputMenu] Focus set to Back");
	}

	private async void OnBindingsUpdated() {
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		UpdateButtonLabels();
		GD.Print("[InputMenu] BindingsUpdated → UI refreshed.");
	}

	private void OnResetPressed() {
		_inputManager.ResetToDefaults();
		GD.Print("[InputMenu] Reset pressed.");
	}

	private async void OnBackPressed() {
		var tree = GetTree();
		var scene = GD.Load<PackedScene>("res://Scenes/UI/OptionsMenu.tscn");
		if (scene == null) {
			GD.PushError("[InputMenu] Failed to load OptionsMenu.tscn");
			return;
		}

		if (tree.Root.HasNode("OptionsMenu")) return;

		QueueFree();
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		var optionsMenu = scene.Instantiate<Control>();
		optionsMenu.Name = "OptionsMenu";
		tree.Root.AddChild(optionsMenu);

		if (optionsMenu.HasMethod("RegrabFocus"))
			optionsMenu.CallDeferred("RegrabFocus");

		GD.Print("[InputMenu] Switched back to OptionsMenu.");
	}

	private void UpdateButtonLabels() {
		foreach (var node in GetTree().GetNodesInGroup("rebind_buttons")) {
			if (node is Button btn && btn.HasMeta("ActionName")) {
				string action = btn.GetMeta("ActionName").AsString();
				btn.Text = $"{FormatActionName(action)}: {GetCurrentBindingLabel(action)}";
			}
		}
	}

	private string FormatActionName(string action) => action.Replace("_", " ").ToUpper();

	private string GetCurrentBindingLabel(string action) {
		var events = InputMap.ActionGetEvents(action);
		if (events.Count == 0) return "None";

		var keyboard = new List<string>();
		var gamepad = new List<string>();

		foreach (var e in events) {
			switch (e) {
				case InputEventJoypadButton jb:
					gamepad.Add(InputLabelFormatter.PrettyGamepadButton((int)jb.ButtonIndex));
					break;
				case InputEventJoypadMotion jm:
					gamepad.Add(InputLabelFormatter.PrettyGamepadAxis((int)jm.Axis));
					break;
				case InputEventKey key:
					keyboard.Add(InputLabelFormatter.PrettyKey(key.Keycode));
					break;
			}
		}

		var parts = new List<string>();
		if (keyboard.Count > 0) parts.Add(string.Join(" / ", keyboard));
		if (gamepad.Count > 0) parts.Add(string.Join(" / ", gamepad));
		return string.Join("  |  ", parts);
	}
}
