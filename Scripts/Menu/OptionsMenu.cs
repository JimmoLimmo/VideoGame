using Godot;
using System.Threading.Tasks;

public partial class OptionsMenu : Control {
    private SettingsManager _settings;

    private CheckBox _fullscreenCheck;
    private OptionButton _resolutionDropdown;
    private HSlider _masterSlider;
    private HSlider _musicSlider;
    private HSlider _sfxSlider;
    private Button _applyButton;
    private Button _inputButton;
    private Button _backButton;

    public override void _Ready() {
        _settings = GetNodeOrNull<SettingsManager>("/root/SettingsManager");
        if (_settings == null) {
            GD.PushWarning("[OptionsMenu] SettingsManager not found in autoload.");
            return;
        }

        _fullscreenCheck = GetNode<CheckBox>("CenterContainer/VBoxContainer/FullscreenCheck");
        _resolutionDropdown = GetNode<OptionButton>("CenterContainer/VBoxContainer/ResolutionDropdown");
        _masterSlider = GetNode<HSlider>("CenterContainer/VBoxContainer/MasterVolumeSlider");
        _musicSlider = GetNode<HSlider>("CenterContainer/VBoxContainer/MusicVolumeSlider");
        _sfxSlider = GetNode<HSlider>("CenterContainer/VBoxContainer/SFXVolumeSlider");
        _applyButton = GetNode<Button>("CenterContainer/VBoxContainer/ApplyButton");
        _inputButton = GetNode<Button>("CenterContainer/VBoxContainer/InputButton");
        _backButton = GetNode<Button>("CenterContainer/VBoxContainer/BackButton");

        _resolutionDropdown.AddItem("1280 × 720");
        _resolutionDropdown.AddItem("1920 × 1080");
        _resolutionDropdown.AddItem("2560 × 1440");
        _resolutionDropdown.Select(_settings.ResolutionIndex);

        _fullscreenCheck.ButtonPressed = _settings.Fullscreen;
        _masterSlider.Value = _settings.MasterVolume * 100f;
        _musicSlider.Value = _settings.MusicVolume * 100f;
        _sfxSlider.Value = _settings.SfxVolume * 100f;

        _applyButton.Pressed += OnApplyPressed;
        _inputButton.Pressed += OnInputPressed;
    }

    private void OnApplyPressed() {
        if (_settings == null) return;

        _settings.Fullscreen = _fullscreenCheck.ButtonPressed;
        _settings.MasterVolume = (float)_masterSlider.Value / 100f;
        _settings.MusicVolume = (float)_musicSlider.Value / 100f;
        _settings.SfxVolume = (float)_sfxSlider.Value / 100f;
        _settings.ResolutionIndex = _resolutionDropdown.GetSelectedId();

        _settings.ApplySettings();
        _settings.SaveSettings();

        GD.Print("[OptionsMenu] Settings applied and saved.");
    }

    private async void OnInputPressed() {
        var tree = GetTree();

        // Load InputMenu scene
        var inputScene = GD.Load<PackedScene>("res://Scenes/UI/InputMenu.tscn");
        if (inputScene == null) {
            GD.PushError("[OptionsMenu] Failed to load InputMenu scene!");
            return;
        }

        var inputMenu = inputScene.Instantiate<Control>();
        tree.Root.AddChild(inputMenu);

        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        // Remove this OptionsMenu so only InputMenu shows
        QueueFree();

        GD.Print("[OptionsMenu] Switched to InputMenu.");
    }
}
