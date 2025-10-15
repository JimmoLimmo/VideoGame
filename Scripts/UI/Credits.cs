using Godot;
using System;

public partial class Credits : Node2D {
	[Export] public float startTime = 0.5f;
	[Export] public float slideTime = 7f;

	private int slideCount = 0;
	private bool exit = false;

	private Timer slideTimer;
	private Label scrum;
	private Label pgm;
	private Label art;
	private Label music;
	private Label sfx;
	private Label ty;

	private Label[] slides;
	
	public override void _Ready() {
		slideTimer = GetNode<Timer>("SlideTimer");
	
		scrum = GetNode<Label>("CanvasLayer/Control/Scrum");
		pgm = GetNode<Label>("CanvasLayer/Control/Programming");
		art = GetNode<Label>("CanvasLayer/Control/Art");
		music = GetNode<Label>("CanvasLayer/Control/Music");
		sfx = GetNode<Label>("CanvasLayer/Control/SoundEffects");
		ty = GetNode<Label>("CanvasLayer/Control/ThankYou");
		
		slides = new Label[] {pgm, art, music, sfx, ty};
		
		slideTimer.OneShot = true;
		slideTimer.Timeout += OnCycleTimeout;
		slideTimer.Start(startTime);
	}
	
	private async void OnCycleTimeout() {
		if(exit) GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");
		
		Label prev = null;
		if(slideCount > 0) prev = slides[slideCount-1];
		Label curr = null;
		if(slideCount < slides.Length) curr = slides[slideCount];
		
		if(prev != null) {
			var fadeOut = GetTree().CreateTween();
			fadeOut.TweenProperty(prev, "modulate:a", 0.0f, slideTime/4);
			await ToSignal(fadeOut, Tween.SignalName.Finished);
		}
		
		if(curr != null) {
			var fadeIn = GetTree().CreateTween();
			fadeIn.TweenProperty(curr, "modulate:a", 1.0f, slideTime/4);
			await ToSignal(fadeIn, Tween.SignalName.Finished);
		} else {
			exit = true;
		}
		
		slideTimer.Start(slideTime/2);
		slideCount ++;
	}
}
