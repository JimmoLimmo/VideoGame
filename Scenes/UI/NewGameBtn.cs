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
		var tree = GetTree();

		// Release UI focus cleanly via viewport
		GetViewport().GuiReleaseFocus();
		tree.Root.GuiDisableInput = true;
		Disabled = true;

		var fader = tree.Root.GetNodeOrNull<ScreenFader>("/root/ScreenFader");
		if (fader != null)
			await fader.FadeOut(0.5f);

		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		tree.ChangeSceneToPacked(sceneToSwitchTo);

		for (int i = 0; i < 3; i++)
			await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

		GlobalRoomChange.ForceUpdate();

		if (fader != null)
			await fader.FadeIn(0.5f, true);

		await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		tree.Root.GuiDisableInput = false;
		Disabled = false;

		CallDeferred(nameof(QueueFree));
	}


}
