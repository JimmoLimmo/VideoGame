using Godot;
using System.Threading.Tasks;

public partial class StartGameButton : Button {
    [Export] private PackedScene sceneToSwitchTo;
    private ScreenFader _fade;

    public override void _Ready() {
        Pressed += OnStartGameButtonPressed;
        _fade = GetTree().CurrentScene?.GetNodeOrNull<ScreenFader>("ScreenFade");
        if (_fade == null)
            GD.PushWarning("[StartGameButton] ScreenFade node not found. Fade will be skipped.");
    }

    private async void OnStartGameButtonPressed() {
        if (sceneToSwitchTo == null) return;

        var tree = GetTree();
        tree.Paused = false;

        // fade out first
        if (_fade != null)
            await _fade.FadeOut(0.25f);

        // switch scenes
        tree.ChangeSceneToPacked(sceneToSwitchTo);

        // fade back in once new scene has loaded
        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        var newFade = tree.CurrentScene?.GetNodeOrNull<ScreenFader>("ScreenFade");
        if (newFade != null)
            await newFade.FadeIn(0.55f);
    }
}
