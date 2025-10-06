using Godot;
using System;
using System.Threading.Tasks;

public partial class ScreenFader : Node {
	[Export] public ColorRect FadeRect;
	
	public override void _Ready() {
		FadeRect.Modulate = new Color(0, 0, 0, 0);
		FadeRect.Visible = false;
	}
	
	public async Task FadeOut(float duration = 1.0f) {
		FadeRect.Visible = true;
		var tween = CreateTween();
		tween.TweenProperty(FadeRect, "modulate:a", 1f, duration)
			.SetTrans(Tween.TransitionType.Linear)
			.SetEase(Tween.EaseType.InOut);
			
		await ToSignal(tween, Tween.SignalName.Finished);
	}
	
	public async Task FadeIn(float duration = 1.0f) {
		var tween = CreateTween();
		tween.TweenProperty(FadeRect, "modulate:a", 0f, duration)
			.SetTrans(Tween.TransitionType.Linear)
			.SetEase(Tween.EaseType.InOut);
			
		await ToSignal(tween, Tween.SignalName.Finished);
			
		FadeRect.Visible = false;
	}
}
