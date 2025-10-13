using Godot;
using System.Threading.Tasks;

public partial class NewGameBtn : Button {
	[Export] private PackedScene sceneToSwitchTo;
	[Export] private NodePath confirmationPopupPath;

	private ConfirmationPopup _popup;

	public override void _Ready() {
		Pressed += OnPressed;
		_popup = GetNodeOrNull<ConfirmationPopup>(confirmationPopupPath);
	}

	private async void OnPressed() {
		_popup?.ShowPopup(
			"Start a New Game?\nExisting Save Data Will Be Lost.",
			async () => await StartNewGame()
		);
	}

	private async Task StartNewGame() {
		ReleaseFocus();
		Disabled = true;

// IMPORTANT: Reset save data for new game
		SaveManager.ResetToNewGame(true);
		
		// Reset GlobalRoomChange state for new game  
		GlobalRoomChange.Activate = false;
		GlobalRoomChange.hasSword = false;
		GlobalRoomChange.hasDash = false;
		GlobalRoomChange.hasWalljump = false;
		GlobalRoomChange.hasClawTeleport = false;
		GlobalRoomChange.health = 5;
		GlobalRoomChange.mana = 0;

var tree = GetTree();
		var fader = tree.Root.GetNodeOrNull<ScreenFader>("/root/ScreenFader");
		if (fader != null)
			await fader.FadeOut(0.4f);

		tree.ChangeSceneToPacked(sceneToSwitchTo);

		// Give the new scene a moment to fully initialize (HUD, overlays, etc.)
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		GlobalRoomChange.ForceUpdate();

		if (fader != null)
			await fader.FadeIn(0.4f, true);

		Disabled = false;
	}



}
