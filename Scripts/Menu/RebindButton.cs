using Godot;
using System.Globalization;
using System.Linq;

public partial class RebindButton : Button {
	[Export] public string ActionName { get; set; } = "";

	private bool _waitingForInput = false;
	private TextInfo _textInfo = CultureInfo.CurrentCulture.TextInfo;

	public override void _Ready() {
		UpdateButtonLabel();
		Pressed += OnPressed;

		// Listen for reset signal from InputManager
		var im = GetNodeOrNull<InputManager>("/root/InputManager");
		if (im != null)
			im.BindingsUpdated += UpdateButtonLabel;
	}

	public override void _ExitTree() {
		// Disconnect when removed (prevents disposed-object errors)
		var im = GetNodeOrNull<InputManager>("/root/InputManager");
		if (im != null)
			im.BindingsUpdated -= UpdateButtonLabel;
	}

	private void OnPressed() {
		if (_waitingForInput) return;

		string prettyName = _textInfo.ToTitleCase(ActionName.Replace("_", " "));
		GD.Print($"[RebindButton] Waiting for new input for '{prettyName}'");

		Text = $"Press any key or button for {prettyName}...";
		_waitingForInput = true;
	}

	public override void _Input(InputEvent @event) {
// Safety check: don't process if not in tree
		if (!IsInsideTree() || IsQueuedForDeletion())
			return;
			
if (!_waitingForInput) return;

		// --- Keyboard ---
		if (@event is InputEventKey keyEvent && keyEvent.Pressed) {
			if (keyEvent.Keycode == Key.Escape) {
				GD.Print("[RebindButton] Rebinding canceled.");
				UpdateButtonLabel();
				_waitingForInput = false;
				return;
			}
			ApplyNewBinding(keyEvent, OS.GetKeycodeString(keyEvent.Keycode));
		}

		// --- Gamepad button ---
		else if (@event is InputEventJoypadButton joyButton && joyButton.Pressed) {
			ApplyNewBinding(joyButton, $"Button {joyButton.ButtonIndex}");
		}

		// --- Joystick axis / trigger ---
		else if (@event is InputEventJoypadMotion motion && Mathf.Abs(motion.AxisValue) > 0.5f) {
			string dir = motion.AxisValue > 0 ? "+" : "–";
			ApplyNewBinding(motion, $"Axis {motion.Axis}{dir}");
		}
	}

	private void ApplyNewBinding(InputEvent evt, string label) {
		InputMap.ActionEraseEvents(ActionName);
		InputMap.ActionAddEvent(ActionName, evt);

		string prettyName = _textInfo.ToTitleCase(ActionName.Replace("_", " "));
		Text = $"{prettyName}: {label}";
		_waitingForInput = false;

		// Save through InputManager autoload
		var im = GetNodeOrNull<InputManager>("/root/InputManager");
		im?.SaveBindings();

		GD.Print($"[RebindButton] '{prettyName}' rebound to {label} and saved.");
	}

	public void UpdateButtonLabel() {
		string prettyName = _textInfo.ToTitleCase(ActionName.Replace("_", " "));
		var events = InputMap.ActionGetEvents(ActionName);

		if (events.Count == 0) {
			Text = $"{prettyName}: None";
			return;
		}

		// Use first binding for display
		var e = events.First();
		string bindingText = e switch {
			InputEventKey key => OS.GetKeycodeString(key.Keycode),
			InputEventJoypadButton jb => $"Button {jb.ButtonIndex}",
			InputEventJoypadMotion jm => $"Axis {jm.Axis}",
			InputEventMouseButton mb => $"Mouse {mb.ButtonIndex}",
			_ => "None"
		};

		Text = $"{prettyName}: {bindingText}";
	}
}
