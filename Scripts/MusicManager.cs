using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public enum BgmTrack { None, Title, Overworld, Boss, Ambiance }

public partial class MusicManager : Node {
    // ---- Singleton instance ----
    public static MusicManager Instance { get; private set; }

    private readonly Dictionary<BgmTrack, string> _paths = new()
    {
        { BgmTrack.Title,     "res://Audio/Music/The Voice Someone Calls - Persona 3 Reload OST [Extended] [Rl1qXhmKYfM].mp3" },
        { BgmTrack.Overworld, "res://Audio/Music/Persona 3 OST - Tartarus Block 1 Extended.mp3" },
        { BgmTrack.Boss,   "res://Audio/Music/S61-216 Hollow Knight.wav" },
        { BgmTrack.Ambiance, "res://Audio/SFX/mines_machinery_atmos.wav"}
    };

    private AudioStreamPlayer _a, _b, _active, _inactive;
    private BgmTrack _current = BgmTrack.None;
    private BgmTrack _preBoss = BgmTrack.None;
    private bool _isFading = false;

    [Export] public double DefaultCrossfade = 0.8;
    [Export] public float StartGainDb = -12f;
    [Export] public float FadeOutDb = -24f;

    // --- Autoload setup ---
    public override void _EnterTree() {
        Instance = this;
    }

    public override void _Ready() {
        _a = GetNode<AudioStreamPlayer>("A");
        _b = GetNode<AudioStreamPlayer>("B");

        _a.Autoplay = false;
        _b.Autoplay = false;
        _a.VolumeDb = 0f;
        _b.VolumeDb = 0f;

        _active = _a;
        _inactive = _b;

        ProcessMode = Node.ProcessModeEnum.Always;
    }

    // --- Main playback control ---
    public async void Play(BgmTrack track, double crossfadeSeconds = -1.0) {
        if (_isFading) return;
        if (track == BgmTrack.None) { Stop(); return; }
        if (track == _current && _active.Playing) return;
        if (crossfadeSeconds < 0) crossfadeSeconds = DefaultCrossfade;

        if (!_paths.TryGetValue(track, out var path)) {
            GD.PushWarning($"MusicManager: no path mapped for {track}.");
            return;
        }

        var stream = ResourceLoader.Load<AudioStream>(path);
        if (stream == null) {
            GD.PushWarning($"MusicManager: failed to load {path}");
            return;
        }

        GD.Print($"[MusicManager] Switching to {track} ({path})");

        _isFading = true;

        _inactive.Stream = stream;
        _inactive.VolumeDb = StartGainDb;
        _inactive.Play();

        if (_active.Playing && crossfadeSeconds > 0) {
            await FadePair(_active, FadeOutDb, _inactive, 0f, crossfadeSeconds);
            _active.Stop();
            _active.VolumeDb = 0f;
        }
        else {
            _active.Stop();
            _inactive.VolumeDb = 0f;
        }

        (_active, _inactive) = (_inactive, _active);
        _current = track;
        _isFading = false;
    }

    public async void Stop(double fadeSeconds = 0.4) {
        if (!_active.Playing) return;
        await Fade(_active, -30f, fadeSeconds);
        _active.Stop();
        _active.VolumeDb = 0f;
        _current = BgmTrack.None;
    }

    // --- Boss track helpers ---
    public void StartBoss(double crossfadeSeconds = -1.0) {
        if (!_paths.ContainsKey(BgmTrack.Boss)) {
            GD.PushWarning("MusicManager: Boss track not mapped. Add it to _paths.");
            return;
        }

        if (_current != BgmTrack.Boss)
            _preBoss = _current;

        Play(BgmTrack.Boss, crossfadeSeconds);
    }

    public void EndBoss(double crossfadeSeconds = -1.0) {
        if (_preBoss != BgmTrack.None)
            Play(_preBoss, crossfadeSeconds);
    }

    // --- Status helpers ---
    public bool IsPlaying(BgmTrack track) {
        return _current == track && _active != null && _active.Playing;
    }

    public BgmTrack CurrentTrack => _current;

    // --- Tween helpers ---
    private Task Fade(AudioStreamPlayer p, float toDb, double seconds) {
        var tcs = new TaskCompletionSource();
        var tw = GetTree().CreateTween();
        tw.TweenProperty(p, "volume_db", toDb, seconds);
        tw.Finished += () => tcs.SetResult();
        return tcs.Task;
    }

    private async Task FadePair(AudioStreamPlayer from, float fromDb, AudioStreamPlayer to, float toDb, double seconds) {
        var t1 = GetTree().CreateTween(); t1.TweenProperty(from, "volume_db", fromDb, seconds);
        var t2 = GetTree().CreateTween(); t2.TweenProperty(to, "volume_db", toDb, seconds);
        await ToSignal(t2, Tween.SignalName.Finished);
    }
}
