using Godot;
using System.Threading.Tasks;

public partial class ScreenFader : Node {
	[Export] public ColorRect FadeRect;

	private Tween _tween;

	public override void _Ready() {
		if (FadeRect == null) {
			GD.PushError("[ScreenFader] FadeRect not assigned.");
			return;
		}

		// Start fully transparent black
		FadeRect.Color = new Color(0, 0, 0, 0);
		FadeRect.Visible = false;
	}

	public async Task FadeOut(float duration = 0.3f) {
		if (FadeRect == null) return;

		KillTween();

		// Ensure black before showing (prevents white flash)
		var c = FadeRect.Color;
		FadeRect.Color = new Color(0, 0, 0, c.A); // force black, keep current alpha

		FadeRect.Visible = true;

		_tween = CreateTween()
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);

		_tween.TweenProperty(FadeRect, "color:a", 1f, duration);
		await ToSignal(_tween, Tween.SignalName.Finished);
		_tween = null;
	}

	public async Task FadeIn(float duration = 0.3f, bool forceFromBlack = false) {
		if (FadeRect == null) return;

		KillTween();

		// Force black first (no white) and make sure it’s visible
		var c = FadeRect.Color;
		float startA = forceFromBlack ? 1f : c.A;
		FadeRect.Color = new Color(0, 0, 0, startA); // black with chosen alpha
		FadeRect.Visible = true;

		_tween = CreateTween()
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);

		_tween.TweenProperty(FadeRect, "color:a", 0f, duration);
		await ToSignal(_tween, Tween.SignalName.Finished);
		_tween = null;

		FadeRect.Visible = false; // stop blocking input/draw
	}

	private void KillTween() {
		if (_tween != null && IsInstanceValid(_tween))
			_tween.Kill();
		_tween = null;
	}
}
