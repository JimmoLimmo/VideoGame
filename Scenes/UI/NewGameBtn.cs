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

	private void OnPressed() {
		// Swap to OK/YesNo later when save system lands
		_popup?.ShowPopup(
			"Start a New Game?\nExisting Save Data Will Be Lost.",
			async () => await StartNewGame()
		);
	}

	private async Task StartNewGame() {
		ReleaseFocus();
		Disabled = true;

		var tree = GetTree();

		// Use global fader instance (autoload pattern)
		var fader = tree.Root.GetNodeOrNull<ScreenFader>("/root/ScreenFader");
		if (fader != null) await fader.FadeOut(0.5f);

		tree.ChangeSceneToPacked(sceneToSwitchTo);

		// Let the new scene settle
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		GlobalRoomChange.ForceUpdate();

		if (fader != null) await fader.FadeIn(0.5f, true);
		Disabled = false;
	}
}
