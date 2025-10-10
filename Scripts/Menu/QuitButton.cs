using Godot;

public partial class QuitButton : Button {
<<<<<<< HEAD
    [Export] private NodePath confirmationPopupPath;
    private ConfirmationPopup _popup;

    public override void _Ready() {
        Pressed += OnPressed;
        _popup = GetNodeOrNull<ConfirmationPopup>(confirmationPopupPath);

        if (_popup == null)
            GD.PushWarning("[QuitButton] ConfirmationPopup not found — check the exported NodePath.");
    }

    private void OnPressed() {
        // Prevent duplicate popups or input leakage
        ReleaseFocus();
        Disabled = true;

        // Re-enable button next frame (after popup appears)
        CallDeferred(nameof(ReenableButton));

        // If popup missing, just quit
        if (_popup == null) {
            GetTree().Quit();
            return;
        }

        // Show confirmation dialog
        _popup.ShowPopup(
            "Are you sure you want to quit?",
            () => GetTree().Quit() // confirmed → quit application
        );
    }

    private void ReenableButton() {
        Disabled = false;
    }
=======
	[Export] private NodePath confirmationPopupPath;
	private ConfirmationPopup _popup;

	public override void _Ready() {
		Pressed += OnPressed;
		_popup = GetNodeOrNull<ConfirmationPopup>(confirmationPopupPath);

		if (_popup == null)
			GD.Print("[QuitButton] ConfirmationPopup not found — will quit directly without confirmation.");
	}

	private void OnPressed() {
		// Prevent duplicate popups or input leakage
		ReleaseFocus();
		Disabled = true;

		// Re-enable button next frame (after popup appears)
		CallDeferred(nameof(ReenableButton));

		// If popup missing, just quit
		if (_popup == null) {
			GetTree().Quit();
			return;
		}

		// Show confirmation dialog
		_popup.ShowPopup(
			"Are you sure you want to quit?",
			() => GetTree().Quit() // confirmed → quit application
		);
	}

	private void ReenableButton() {
		Disabled = false;
	}
>>>>>>> Aidan
}
