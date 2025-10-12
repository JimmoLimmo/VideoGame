using Godot;
using System;

public partial class Shockwave : Area2D {
    [Export] public float Speed = 550f;
    [Export] public float Lifetime = 2.5f;
    [Export] public int Damage = 1;
    [Export] public float FadeTime = 0.4f;

    private float _life = 0f;
    private int _dir = 1;
    private Sprite2D _sprite;

    public override void _Ready() {
        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        Monitoring = true;
        BodyEntered += OnBodyEntered;
    }

    public void Setup(int dir) {
        _dir = Mathf.Sign(dir);
        if (_sprite != null)
            _sprite.FlipH = _dir < 0;
    }

    public override void _PhysicsProcess(double delta) {
        _life += (float)delta;
        GlobalPosition += new Vector2(Speed * _dir * (float)delta, 0);

        if (_life > Lifetime - FadeTime) {
            float fade = 1f - ((_life - (Lifetime - FadeTime)) / FadeTime);
            Modulate = new Color(1, 1, 1, fade);
        }

        if (_life >= Lifetime)
            QueueFree();
    }

    private void OnBodyEntered(Node body) {
        if (body is Player p)
            p.ApplyHit(Damage, GlobalPosition);
    }
}
