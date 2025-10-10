using Godot;
using System;

public partial class ConfirmationPopup : Popup {
	private Label _label;
	private Button _yes;
	private Button _no;
	private Button _ok;
	private Action _onConfirm;

	public override void _Ready() {
		_label = GetNode<Label>("Panel/VBoxContainer/Label");
		_yes = GetNode<Button>("Panel/VBoxContainer/HBoxContainer/Yes");
		_no = GetNode<Button>("Panel/VBoxContainer/HBoxContainer/No");
		_ok = GetNode<Button>("Panel/VBoxContainer/HBoxContainer/OK");

		_yes.Pressed += () => { Hide(); _onConfirm?.Invoke(); };
		_no.Pressed += Hide;
		_ok.Pressed += Hide;

		AboutToPopup += () => GetTree().Paused = true;
		VisibilityChanged += () => GetTree().Paused = false;
	}

	// Standard yes/no popup
	public void ShowPopup(string message, Action onConfirm = null) {
		_label.Text = message;
		_onConfirm = onConfirm;

		_yes.Visible = true;
		_no.Visible = true;
		_ok.Visible = false;

		PopupCentered();
		_yes.GrabFocus();
	}

	// Info-only popup (just "OK")
	public void ShowInfo(string message) {
		_label.Text = message;
		_onConfirm = null;

		_yes.Visible = false;
		_no.Visible = false;
		_ok.Visible = true;

		PopupCentered();
		_ok.GrabFocus();
	}
}
