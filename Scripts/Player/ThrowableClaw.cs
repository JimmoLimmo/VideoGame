using Godot;
using System.Collections.Generic;

public partial class ThrowableClaw : RigidBody2D
{
	[Export] public int Damage { get; set; } = 10;
	[Export] public float MaxRange { get; set; } = 600f;
	[Export] public float ThrowSpeed { get; set; } = 300f;
	
	private Vector2 _startPosition;
	private Player _player;
	private Vector2 _direction;
	private bool _isThrown = false;
	private bool _hasHit = false;
	private float _totalDistanceTraveled = 0f;
	private Vector2 _lastPosition;
	
	// Visual and audio feedback nodes
	private Sprite2D _sprite;
	private CollisionShape2D _collisionShape;
	private Area2D _hurtbox;
	private CollisionShape2D _hurtboxShape;
	private AudioStreamPlayer2D _audioPlayer;
	
	// Trail effect for visual feedback
	private Line2D _trail;
	private List<Vector2> _trailPoints = new List<Vector2>();
	private int _maxTrailPoints = 20;
	
	public override void _Ready()
	{
		// Get node references
		_sprite = GetNode<Sprite2D>("Sprite2D");
		_collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		_hurtbox = GetNode<Area2D>("Hurtbox");
		_hurtboxShape = GetNode<CollisionShape2D>("Hurtbox/CollisionShape2D");
		_audioPlayer = GetNode<AudioStreamPlayer2D>("AudioStreamPlayer2D");
		
		// Create trail effect
		_trail = new Line2D();
		_trail.Width = 3f;
		_trail.DefaultColor = Colors.Cyan;
		AddChild(_trail);
		
		// Set up physics
		GravityScale = 0f; // No gravity for thrown claw
		LinearDamp = 0f; // No air resistance
		
		// Connect collision signals
		BodyEntered += OnBodyEntered;
		
		// Store initial position for distance calculation
		_lastPosition = GlobalPosition;
		
		// Connect to damage detection area
		if (_hurtbox != null)
		{
			_hurtbox.BodyEntered += OnHurtboxBodyEntered;
		}
		
		GD.Print("Collision detection enabled for ThrowableClaw");
	}
	
	public void ThrowInDirection(Vector2 direction)
	{
		_direction = direction.Normalized();
		_startPosition = GlobalPosition;
		_isThrown = true;
		_hasHit = false;
		_totalDistanceTraveled = 0f;
		_lastPosition = GlobalPosition;
		
		// Set velocity
		LinearVelocity = _direction * ThrowSpeed;
		
		// Rotate sprite to match direction
		if (_sprite != null)
		{
			_sprite.Rotation = _direction.Angle();
		}
		
		// Play throw sound
		if (_audioPlayer != null)
		{
			_audioPlayer.Play();
		}
		
		GD.Print($"ThrowableClaw thrown in direction: {_direction}");
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (!_isThrown) return;
		
		// Calculate distance traveled
		float frameDistance = GlobalPosition.DistanceTo(_lastPosition);
		_totalDistanceTraveled += frameDistance;
		_lastPosition = GlobalPosition;
		
		// Add point to trail
		UpdateTrail();
		
		// Check if we've exceeded maximum range
		if (_totalDistanceTraveled >= MaxRange)
		{
			StopMoving();
		}
	}
	
	public override void _Input(InputEvent @event)
	{
		// Safety check: don't process input if we're not in the tree or being destroyed
		if (!IsInsideTree() || IsQueuedForDeletion())
			return;
			
		// Check if player presses the claw throw key again to teleport
		if (@event.IsActionPressed("claw_throw") && _player != null)
		{
			TeleportPlayerAndDestroy();
		}
	}
	
	private void UpdateTrail()
	{
		// Add current position to trail
		_trailPoints.Add(GlobalPosition);
		
		// Limit trail length
		if (_trailPoints.Count > _maxTrailPoints)
		{
			_trailPoints.RemoveAt(0);
		}
		
		// Update Line2D points
		if (_trail != null)
		{
			_trail.ClearPoints();
			foreach (Vector2 point in _trailPoints)
			{
				_trail.AddPoint(point - GlobalPosition); // Relative to claw position
			}
		}
	}
	
	private void StopMoving()
	{
		LinearVelocity = Vector2.Zero;
		GravityScale = 1f; // Allow gravity to take effect
		GD.Print("ThrowableClaw stopped due to max range");
	}
	
	public void TeleportPlayerAndDestroy()
	{
		if (_player == null) return;
		
		GD.Print("Teleporting player to claw position");
		
		// Calculate a safe teleport position (slightly offset from claw)
		Vector2 teleportPos = GlobalPosition;
		
		// Offset the player slightly to avoid getting stuck in walls
		Vector2 offset = Vector2.Zero;
		
		// Try to place player in a safe spot around the claw
		PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;
		
		// Check different offset positions
		Vector2[] offsets = {
			Vector2.Zero,
			new Vector2(-32, 0),  // Left
			new Vector2(32, 0),   // Right
			new Vector2(0, -32),  // Up
			new Vector2(0, 32),   // Down
			new Vector2(-32, -32), // Up-left
			new Vector2(32, -32),  // Up-right
		};
		
		foreach (Vector2 testOffset in offsets)
		{
			Vector2 testPos = GlobalPosition + testOffset;
			
			// Create a query to test if this position is safe
			PhysicsPointQueryParameters2D query = new PhysicsPointQueryParameters2D();
			query.Position = testPos;
			query.CollideWithAreas = false;
			query.CollideWithBodies = true;
			
			var results = spaceState.IntersectPoint(query);
			
			// If no collision found, this position is safe
			if (results.Count == 0)
			{
				teleportPos = testPos;
				break;
			}
		}
		
		// Teleport the player
		_player.GlobalPosition = teleportPos;
		
		// Destroy the claw
		QueueFree();
	}
	
	private void OnBodyEntered(Node body)
	{
		GD.Print($"ThrowableClaw collision with: {body.Name}");
		
		// Don't collide with the player or other thrown claws
		if (body == _player) return;
		if (body == this || body is ThrowableClaw)
			return;
		
		// Stop movement on collision
		StopMoving();
		_hasHit = true;
		
		// Deal damage if it's a damageable entity
		if (body.HasMethod("TakeDamage"))
		{
			body.Call("TakeDamage", Damage);
			GD.Print($"ThrowableClaw dealt {Damage} damage to {body.Name}");
		}
	}
	
	private void OnHurtboxBodyEntered(Node2D body)
	{
		// Handle damage detection through hurtbox
		if (body == _player) return;
		
		// Deal damage to enemies
		if (body.HasMethod("TakeDamage"))
		{
			body.Call("TakeDamage", Damage);
			GD.Print($"ThrowableClaw hurtbox dealt {Damage} damage to {body.Name}");
		}
	}
	
	public void SetPlayer(Player player)
	{
		_player = player;
	}
	
	// Called when the claw is about to be destroyed
	public override void _ExitTree()
	{
		// Clean up trail
		if (_trail != null && _trail.IsInsideTree())
		{
			_trail.QueueFree();
		}
		
		base._ExitTree();
	}
}