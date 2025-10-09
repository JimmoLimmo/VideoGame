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
		// if (SaveSystem.HasSaveData()) {
		// 	_popup?.ShowPopup(
		// 		"Start a new game?\nExisting save data will be lost.",
		// 		async () => await StartNewGame()
		// 	);
		// }
		// else {
		// 	await StartNewGame();
		// }
		_popup?.ShowPopup(
			"Start a New Game?\nExisting Save Data Will Be Lost.",
			async () => await StartNewGame()
		);
	}

	private async Task StartNewGame() {
		ReleaseFocus();
		Disabled = true;

		var tree = GetTree();
		tree.Root.GuiDisableInput = true; //  freeze UI input temporarily

		var fader = tree.Root.GetNodeOrNull<ScreenFader>("/root/ScreenFader");
		if (fader != null)
			await fader.FadeOut(0.5f);

		tree.ChangeSceneToPacked(sceneToSwitchTo);

		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		GlobalRoomChange.ForceUpdate();

		if (fader != null)
			await fader.FadeIn(0.5f, true);

		tree.Root.GuiDisableInput = false; // re-enable after fade
		Disabled = false;
	}

}
