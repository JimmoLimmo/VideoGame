using Godot;
using System.Threading.Tasks;
using System.Collections.Generic;

public partial class InputMenu : Control {
	private Button _backButton;
	private Button _resetButton;
	private InputManager _inputManager;

	public override void _Ready() {
		_inputManager = GetNode<InputManager>("/root/InputManager");
		_backButton = GetNode<Button>("CenterContainer/VBoxContainer/BackButton");
		_resetButton = GetNode<Button>("CenterContainer/VBoxContainer/ResetButton");

		_backButton.Pressed += OnBackPressed;
		_resetButton.Pressed += OnResetPressed;

		UpdateButtonLabels();
	}

	private async void OnResetPressed() {
		_inputManager.ResetToDefaults();

		// wait one frame before updating UI
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		UpdateButtonLabels();
		GD.Print("[InputMenu] Reset pressed — defaults restored and saved.");
	}


	private async void OnBackPressed() {
		var tree = GetTree();
		var scene = GD.Load<PackedScene>("res://Scenes/UI/OptionsMenu.tscn");
		if (scene == null) {
			GD.PushError("[InputMenu] Failed to load OptionsMenu.tscn");
			return;
		}

		if (tree.Root.HasNode("OptionsMenu"))
			return;

		QueueFree();
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		var optionsMenu = scene.Instantiate<Control>();
		optionsMenu.Name = "OptionsMenu";
		tree.Root.AddChild(optionsMenu);

		GD.Print("[InputMenu] Switched back to OptionsMenu.");
	}

	// ------------------------------------------------------------
	// Label helpers
	// ------------------------------------------------------------
	private void UpdateButtonLabels() {
		foreach (var node in GetTree().GetNodesInGroup("rebind_buttons")) {
			if (node is Button btn && btn.HasMeta("ActionName")) {
				string action = btn.GetMeta("ActionName").AsString();
				btn.Text = $"{FormatActionName(action)}: {GetCurrentBindingLabel(action)}";
			}
		}
	}

	private string FormatActionName(string action) {
		return action.Replace("_", " ").ToUpper();
	}

	private string GetCurrentBindingLabel(string action) {
		var events = InputMap.ActionGetEvents(action);
		if (events.Count == 0) return "None";

		var keyboardLabels = new List<string>();
		var mouseLabels = new List<string>();
		var gamepadLabels = new List<string>();

		foreach (var e in events) {
			switch (e) {
				case InputEventKey key:
					keyboardLabels.Add(OS.GetKeycodeString(key.Keycode));
					break;
				case InputEventMouseButton mb:
					mouseLabels.Add($"Mouse {mb.ButtonIndex}");
					break;
				case InputEventJoypadButton jb:
					gamepadLabels.Add($"Btn {jb.ButtonIndex}");
					break;
				case InputEventJoypadMotion jm:
					gamepadLabels.Add($"Axis {jm.Axis}");
					break;
			}
		}

		var parts = new List<string>();
		if (keyboardLabels.Count > 0) parts.Add(string.Join(" / ", keyboardLabels));
		if (mouseLabels.Count > 0) parts.Add(string.Join(" / ", mouseLabels));
		if (gamepadLabels.Count > 0) parts.Add(string.Join(" / ", gamepadLabels));

		return string.Join("  |  ", parts);
	}
}
