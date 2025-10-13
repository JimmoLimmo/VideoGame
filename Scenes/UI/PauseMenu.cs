using Godot;
using System.Threading.Tasks;

public partial class PauseMenu : Control {
    [Export] public string PauseActionName = "pause"; // ESC / Start button, etc.

    private Button _continueBtn;
    private Button _quitBtn;
    private ColorRect _overlay;
    private CanvasLayer _pauseLayer;

    public override void _Ready() {
        // Always start hidden
        Visible = false;

        // Find nodes
        _pauseLayer = GetNode<CanvasLayer>("PauseLayer");
        _continueBtn = GetNode<Button>("PauseLayer/CenterContainer/VBoxContainer/ContinueBtn");
        _quitBtn = GetNode<Button>("PauseLayer/CenterContainer/VBoxContainer/QuitToTitleBtn");
        _overlay = GetNode<ColorRect>("PauseLayer/ColorRect");

        _continueBtn.Pressed += OnContinuePressed;
        _quitBtn.Pressed += OnQuitPressed;

        // Make sure this layer always draws above HUD
        _pauseLayer.Layer = 50;

        // Force-hide all children at runtime too
        HideAll();

        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Process(double delta) {
        var tree = GetTree();
        var scene = tree.CurrentScene;
        if (scene == null)
            return;

        // Completely disable pause logic on menu/title screens
        if (scene.IsInGroup("Menu") || scene.IsInGroup("Title")) {
            HideAll();
            if (tree.Paused)
                tree.Paused = false;
            return;
        }

        // Only toggle pause in gameplay scenes
        if (Input.IsActionJustPressed(PauseActionName))
            TogglePause();
    }

    private void HideAll() {
        Visible = false;
        if (_pauseLayer != null)
            _pauseLayer.Visible = false;
    }

    private async void TogglePause() {
        var tree = GetTree();
        bool isPausing = !tree.Paused;

        if (isPausing) {
            tree.Paused = true;
            Visible = true;
            _pauseLayer.Visible = true;
            _continueBtn.GrabFocus();

            // Fade in overlay and menu
            var tween = CreateTween();
            _overlay.Modulate = new Color(0, 0, 0, 0);
            Modulate = new Color(1, 1, 1, 0);
            tween.TweenProperty(_overlay, "modulate:a", 0.6f, 0.25);
            tween.TweenProperty(this, "modulate:a", 1.0f, 0.25);
        }
        else {
            // Fade out overlay and menu
            var tween = CreateTween();
            tween.TweenProperty(_overlay, "modulate:a", 0.0f, 0.15);
            tween.TweenProperty(this, "modulate:a", 0.0f, 0.15);
            await ToSignal(tween, Tween.SignalName.Finished);

            HideAll();
            tree.Paused = false;
        }
    }

    private void OnContinuePressed() => TogglePause();

    private async void OnQuitPressed() {
        var tree = GetTree();
        var fader = tree.Root.GetNodeOrNull<ScreenFader>("/root/ScreenFader");

        if (fader != null)
            await fader.FadeOut(0.4f);

        HideAll();
        tree.Paused = false;
        tree.ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");

        if (fader != null)
            await fader.FadeIn(0.4f);
    }

    public override void _Notification(int what) {
        // Auto-hide on scene change
        if (what == 1000)
            HideAll();
    }
}
