using Godot;
using System;

public partial class ConfirmationPopup : Popup {
	private Label _label;
	private Button _yesButton;
	private Button _noButton;
	private Button _okButton;

	private Action _onConfirm;

	public override void _Ready() {
		_label = GetNode<Label>("Panel/VBoxContainer/Label");
		_yesButton = GetNode<Button>("Panel/VBoxContainer/HBoxContainer/Yes");
		_noButton = GetNode<Button>("Panel/VBoxContainer/HBoxContainer/No");
		_okButton = GetNode<Button>("Panel/VBoxContainer/HBoxContainer/OK");

		_yesButton.Pressed += OnYes;
		_noButton.Pressed += OnNo;
		_okButton.Pressed += OnOk;

		Visible = false;
	}

	public void ShowPopup(string message, Action onConfirm = null, bool showYesNo = true) {
		_label.Text = message;
		_onConfirm = onConfirm;

		_yesButton.Visible = showYesNo;
		_noButton.Visible = showYesNo;
		_okButton.Visible = !showYesNo;

		PopupCentered();
		Show();

		// Focus a sensible default
		if (showYesNo) _yesButton.GrabFocus();
		else _okButton.GrabFocus();
	}

	private void OnYes() { _onConfirm?.Invoke(); Hide(); }
	private void OnNo() { Hide(); }
	private void OnOk() { Hide(); }
}
