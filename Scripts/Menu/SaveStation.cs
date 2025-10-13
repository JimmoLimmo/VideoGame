using Godot;
using System.Threading.Tasks;

public partial class SaveStation : CharacterBody2D
{
	[Export] public bool AutoSaveOnEnter = false;
	[Export] public string InteractAction = "interact"; // map V to this action in InputMap
	[Export] public string SaveAction = "save"; // map X to this action in InputMap
	[Export] public int SavedFrame = 1;
	[Export] public int IdleFrame = 2;

	private bool _playerInRange = false;
	private Player _player;
	private Sprite2D _sprite;
	private Label _prompt;
	private bool _promptOpen = false;
	private AcceptDialog _dialog;
	private float _saveCooldown = 0f;

	public override void _Ready()
	{
		// Don't run runtime-only initialization while the scene is open in the editor.
		// Creating modal dialogs or popups in the editor can conflict with the editor's own
		// dialogs and cause "exclusive child window" errors. Skip when in editor.
		if (Engine.IsEditorHint())
			return;
		_sprite = GetNodeOrNull<Sprite2D>("Sprite2D");

		// Try to find an Area2D detector; if none exists, create one at runtime copying CollisionShape2D
		var detector = GetNodeOrNull<Area2D>("Detector");
		if (detector == null)
		{
			detector = new Area2D();
			detector.Name = "Detector";
			AddChild(detector);
			// Ensure detector detects anything by default (avoid layer/mask mismatches)
			try {
				detector.CollisionLayer = uint.MaxValue;
				detector.CollisionMask = uint.MaxValue;
				
			} catch {
				// some Godot versions may not allow direct setting; ignore if unavailable
			}

			var existingShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			var cs = new CollisionShape2D();
			if (existingShape != null && existingShape.Shape != null)
			{
				// Duplicate the shape resource so we don't share it
				cs.Shape = existingShape.Shape.Duplicate() as Shape2D;
				cs.Position = existingShape.Position;
			}
			detector.AddChild(cs);
		}

		detector.Monitoring = true;
		detector.BodyEntered += OnBodyEntered;
		detector.BodyExited += OnBodyExited;
		// Also listen for area overlap events (Player node may be an Area2D)
		try {
			detector.AreaEntered += OnAreaEntered;
			detector.AreaExited += OnAreaExited;
		} catch {
			// Some Godot versions may not have AreaEntered events on Area2D delegate types; ignore if unavailable
		}

		// Ensure sprite starts at idle frame
		if (_sprite != null)
			_sprite.Frame = IdleFrame;

		// Prompt: use existing Text2D child named "Prompt" or create one
		_prompt = GetNodeOrNull<Label>("Prompt");
		if (_prompt == null)
		{
			_prompt = new Label();
			_prompt.Name = "Prompt";
			// As a Control node, set a simple custom position via a CanvasLayer if needed.
			// Here we use a small offset so it appears above the station.
			_prompt.SetPosition(new Vector2(0, -28));
			_prompt.Visible = false;
			AddChild(_prompt);
		}

		// Create or find a modal dialog for save confirmation
		_dialog = GetNodeOrNull<AcceptDialog>("SaveDialog");
		if (_dialog == null)
		{
			_dialog = new AcceptDialog();
			_dialog.Name = "SaveDialog";
			_dialog.DialogText = "Press X to save";
			// Keep default OK behavior; we'll manually hide the dialog when saving
			AddChild(_dialog);
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Player p)
		{
			
			_playerInRange = true;
			_player = p;

			// Show initial prompt to interact
			if (_prompt != null)
			{
				_prompt.Text = "Press V to interact";
				_prompt.Visible = true;
			}

			if (AutoSaveOnEnter)
				SaveNow();
		}
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is Player p && p == _player)
		{
			
			_playerInRange = false;
			_player = null;
			_promptOpen = false;
			if (_prompt != null)
			{
				_prompt.Visible = false;
				_prompt.Text = string.Empty;
			}
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		
		// If the area is the player's area node, or its parent is Player, treat as enter
		var maybeParent = area.GetParent();
		if (maybeParent is Player pp)
		{
			OnBodyEntered(pp);
			return;
		}
	}

	private void OnAreaExited(Area2D area)
	{
		
		var maybeParent2 = area.GetParent();
		if (maybeParent2 is Player pp2)
		{
			OnBodyExited(pp2);
			return;
		}
	}

	public override void _Process(double delta)
	{
		if (!_playerInRange) return;

		// decrement cooldown
		if (_saveCooldown > 0f)
			_saveCooldown = Mathf.Max(0f, _saveCooldown - (float)delta);

		// Press V to interact/open the save prompt
		if (Input.IsActionJustPressed(InteractAction))
		{
			
			// Open a modal dialog to confirm saving
			if (_dialog != null)
			{
				_dialog.PopupCentered();
				_dialog.Visible = true;
			}
			else
			{
				_promptOpen = !_promptOpen;
				if (_prompt != null)
				{
					_prompt.Text = _promptOpen ? $"Press {SaveAction.ToUpper()} to save" : $"Press {InteractAction.ToUpper()} to interact";
					_prompt.Visible = true;
				}
			}
		}

		// If the dialog is visible and save action pressed, perform save
		if (_dialog != null && _dialog.Visible && Input.IsActionJustPressed(SaveAction))
		{
			
			if (_saveCooldown <= 0f)
			{
				SaveNow();
				_dialog.Hide();
				_saveCooldown = 0.7f; // prevent immediate repeats
			}
			else
			{
				
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Safety check: don't process if not in tree
		if (!IsInsideTree() || IsQueuedForDeletion())
			return;
			
		// Only care about input when player is in range
		if (!_playerInRange) return;

		// Log raw key events for debugging
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			
		}

		// Check actions directly from the event
		if (Input.IsActionJustPressed(InteractAction))
		{
			
			if (_dialog != null)
			{
				_dialog.PopupCentered();
				_dialog.Visible = true;
			}
			else
			{
				_promptOpen = !_promptOpen;
				if (_prompt != null)
				{
					_prompt.Text = _promptOpen ? $"Press {SaveAction.ToUpper()} to save" : $"Press {InteractAction.ToUpper()} to interact";
					_prompt.Visible = true;
				}
			}
		}

		if (_dialog != null && _dialog.Visible && Input.IsActionJustPressed(SaveAction))
		{
			
			SaveNow();
			_dialog.Hide();
		}
	}

	private void SaveNow()
	{
		if (_player == null) return;

		// Merge player's SaveData into the cached save so we preserve collected items
		var playerSave = _player.ToSaveData();
		var cached = SaveManager.GetCurrentSave();
		// copy top-level values
		cached.Hp = playerSave.Hp;
		cached.HasSword = playerSave.HasSword;
		cached.HasDash = playerSave.HasDash;
		cached.HasWalljump = playerSave.HasWalljump;
		cached.HasClawTeleport = playerSave.HasClawTeleport;
		cached.PlayerPosition = playerSave.PlayerPosition;
		// If playerSave provides CollectedItems (unlikely), merge them too
		if (playerSave.CollectedItems != null)
		{
			if (cached.CollectedItems == null) cached.CollectedItems = new System.Collections.Generic.List<string>();
			foreach (var id in playerSave.CollectedItems)
			{
				if (!cached.CollectedItems.Contains(id)) cached.CollectedItems.Add(id);
			}
		}

		// Persist the cached save to disk
		SaveManager.SaveNow();
		FlashSaved();
	}

	private async void FlashSaved()
	{
		if (_sprite == null) return;
		int old = _sprite.Frame;
		_sprite.Frame = SavedFrame;
		if (_prompt != null)
		{
			var oldText = _prompt.Text;
			_prompt.Text = "Saved!";
			await ToSignal(GetTree().CreateTimer(0.7f), "timeout");
			_sprite.Frame = IdleFrame;
			_prompt.Text = oldText;
		}
		else
		{
			await ToSignal(GetTree().CreateTimer(0.7f), "timeout");
			_sprite.Frame = IdleFrame;
		}
	}
}