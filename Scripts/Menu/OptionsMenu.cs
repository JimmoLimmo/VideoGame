using Godot;
using System;

public partial class OptionsMenu : Control {
    private SettingsManager _settings;

    private CheckBox _fullscreenCheck;
    private HSlider _volumeSlider;
    private OptionButton _resolutionDropdown;
    private Button _applyButton;
    private Button _backButton;

    public override void _Ready() {
        _settings = GetNodeOrNull<SettingsManager>("/root/SettingsManager");
        if (_settings == null)
            GD.PushWarning("[OptionsMenu] SettingsManager not found in autoload.");

        _fullscreenCheck = GetNode<CheckBox>("CenterContainer/VBoxContainer/FullscreenCheck");
        _volumeSlider = GetNode<HSlider>("CenterContainer/VBoxContainer/VolumeSlider");
        _resolutionDropdown = GetNode<OptionButton>("CenterContainer/VBoxContainer/ResolutionDropdown");
        _applyButton = GetNode<Button>("CenterContainer/VBoxContainer/ApplyButton");
        _backButton = GetNode<Button>("CenterContainer/VBoxContainer/BackButton");

        if (_settings != null) {
            _fullscreenCheck.ButtonPressed = _settings.Fullscreen;
            _volumeSlider.Value = _settings.MasterVolume * 100f;
            _resolutionDropdown.AddItem("1280 × 720");
            _resolutionDropdown.AddItem("1920 × 1080");
            _resolutionDropdown.AddItem("2560 × 1440");
            _resolutionDropdown.Select(_settings.ResolutionIndex);
        }

        _applyButton.Pressed += OnApplyPressed;
        _backButton.Pressed += OnBackPressed;
    }

    private void OnApplyPressed() {
        if (_settings == null) return;

        _settings.Fullscreen = _fullscreenCheck.ButtonPressed;
        _settings.MasterVolume = (float)_volumeSlider.Value / 100f;
        _settings.ResolutionIndex = _resolutionDropdown.GetSelectedId();

        _settings.ApplySettings();
        _settings.SaveSettings();

        GD.Print("[OptionsMenu] Settings applied and saved.");
    }

    private async void OnBackPressed() {
        var tree = GetTree();

        // --- Load MainMenu scene ---
        var mainMenuScene = GD.Load<PackedScene>("res://Scenes/UI/MainMenu.tscn");
        var mainMenu = mainMenuScene.Instantiate<Control>();
        tree.Root.AddChild(mainMenu);

        // Wait a frame so it appears
        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        // --- Remove this OptionsMenu ---
        QueueFree();
    }
}
