using Godot;
using System.Threading.Tasks;

public partial class PauseMenu : Control {
    [Export] public string PauseActionName = "pause"; // your custom pause bind

    private Button _continueBtn;
    private Button _optionsBtn;
    private Button _quitToTitleBtn;

    public override void _Ready() {
        _continueBtn = GetNode<Button>("CenterContainer/VBoxContainer/ContinueBtn");
        _quitToTitleBtn = GetNode<Button>("CenterContainer/VBoxContainer/QuitToTitleBtn");

        // Hide Options button
        var optionsBtn = GetNodeOrNull<Button>("CenterContainer/VBoxContainer/OptionsBtn");
        if (optionsBtn != null)
            optionsBtn.Visible = false;

        _continueBtn.Pressed += OnContinuePressed;
        _quitToTitleBtn.Pressed += OnQuitPressed;

        ProcessMode = ProcessModeEnum.Always;
        Visible = false;
        ZIndex = 100;
    }


    public override void _Process(double delta) {
        var scene = GetTree().CurrentScene;
        if (scene == null)
            return;

        // Skip pausing when we're in any menu scene
        if (scene.IsInGroup("Menu") || scene.IsInGroup("Title"))
            return;

        if (Input.IsActionJustPressed(PauseActionName))
            TogglePause();
    }



    private void TogglePause() {
        var tree = GetTree();
        bool isPaused = !tree.Paused;

        tree.Paused = isPaused;
        Visible = isPaused;

        if (isPaused) {
            tree.Paused = true;
            Visible = true;
            _continueBtn.GrabFocus();
        }

        else {
            // Unfreeze game
            Visible = false;
        }
    }

    private void OnContinuePressed() {
        TogglePause();
    }

    private async void OnOptionsPressed() {
        var tree = GetTree();
        tree.Paused = false;

        var optionsScene = GD.Load<PackedScene>("res://Scenes/UI/OptionsMenu.tscn");
        if (optionsScene == null) {
            GD.PushError("[PauseMenu] Failed to load OptionsMenu scene!");
            return;
        }

        var optionsMenu = optionsScene.Instantiate<Control>();
        GetTree().Root.AddChild(optionsMenu);

        // Hide pause menu
        Visible = false;

        await ToSignal(optionsMenu, "tree_exited");

        // Re-pause when returning
        tree.Paused = true;
        Visible = true;
        _continueBtn.GrabFocus();
    }


    private async void OnQuitPressed() {
        var tree = GetTree();
        var fader = tree.Root.GetNodeOrNull<ScreenFader>("/root/ScreenFader");

        if (fader != null)
            await fader.FadeOut(0.4f);

        tree.Paused = false;
        Visible = false;
        tree.ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");

        if (fader != null)
            await fader.FadeIn(0.4f);
    }

}
