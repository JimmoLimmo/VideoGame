using Godot;
using System;

public partial class PauseMenu : Control
{
	private Button _resumeBtn;
	private Button _saveBtn;
	private Button _exitBtn;
	private Button _quitBtn;
	private Label _statusLabel;

	public override void _Ready()
	{
		// Note: do not set PauseMode here to avoid API mismatches; we control pause via GetTree().Paused

		_resumeBtn = GetNodeOrNull<Button>("Panel/VBox/ResumeBtn");
		_saveBtn = GetNodeOrNull<Button>("Panel/VBox/SaveBtn");
		_exitBtn = GetNodeOrNull<Button>("Panel/VBox/ExitBtn");
		_quitBtn = GetNodeOrNull<Button>("Panel/VBox/QuitBtn");
		_statusLabel = GetNodeOrNull<Label>("Panel/VBox/Status");

		if (_resumeBtn != null) _resumeBtn.Pressed += OnResumePressed;
		if (_saveBtn != null) _saveBtn.Pressed += OnSavePressed;
		if (_exitBtn != null) _exitBtn.Pressed += OnExitPressed;
		if (_quitBtn != null) _quitBtn.Pressed += OnQuitPressed;

		Visible = false;

		// Ensure this pause menu and its controls still process input while the tree is paused.
		try
		{
			// Set pause_mode on this Control
			try { this.Set("pause_mode", 2); } catch { }
			// Make sure it captures mouse
			try { this.MouseFilter = Control.MouseFilterEnum.Stop; } catch { }
			// Recursively set on children
			void EnsureChildControls(Node n)
			{
				if (n is Control c)
				{
					try { c.Set("pause_mode", 2); } catch { }
					try { c.MouseFilter = Control.MouseFilterEnum.Stop; } catch { }
					try { c.Set("focus_mode", 2); } catch { }
				}
				foreach (var child in n.GetChildren())
				{
					if (child is Node cn)
						EnsureChildControls(cn);
				}
			}
			EnsureChildControls(this);
		}
		catch (Exception) { }

		GD.Print("PauseMenu: Ready");
	}

	public override void _GuiInput(InputEvent @event)
	{
		// Log GUI events to see whether clicks reach this Control when paused
		// No verbose GUI input logging in production
		base._GuiInput(@event);
	}


	private string GetPrimaryKeyForAction(string action)
	{
		// Fast fallback mapping for common actions (project uses these defaults)
		if (action == "save") return "X";
		if (action == "interact") return "V";
		return action.ToUpper();
	}

	public void ShowMenu()
	{
		Visible = true;
		// Reduce global time scale to 0 to 'pause' gameplay while allowing UI to process.
		try { Engine.TimeScale = 0f; } catch { }
	}

	public void HideMenu()
	{
		Visible = false;
		try { Engine.TimeScale = 1f; } catch { }
	}

	private void OnResumePressed()
	{
		
		HideMenu();
	}

	private void OnSavePressed()
	{
		// Find the player via group "player" and save
		var nodes = GetTree().GetNodesInGroup("player");
		if (nodes.Count > 0 && nodes[0] is Player player)
		{
			SaveManager.Save(player.ToSaveData());
			if (_statusLabel != null)
			{
				_statusLabel.Text = "Saved.";
			}
		}
		else
		{
			if (_statusLabel != null)
				_statusLabel.Text = "No player to save.";
		}
	}

	private void OnExitPressed()
	{
		// Stop any currently playing audio before scene transition
		AudioManager.StopAllAudio();

		// Restore time immediately so the engine isn't stuck paused during the scene change
		try { Engine.TimeScale = 1f; } catch { }

		// Hide and remove the pause UI so it won't persist into the main menu
		try { HideMenu(); } catch { }
		try
		{
			var root = GetTree().Root;
			var canvas = root.GetNodeOrNull<CanvasLayer>("PauseCanvasLayer");
			if (canvas != null)
			{
				canvas.QueueFree();
				GD.Print("PauseMenu: removed PauseCanvasLayer");
			}
		}
		catch (Exception) { }

		// Defer the scene change to avoid modifying the scene tree while we're processing UI callbacks.
			var mainMenuPath = "res://Scenes/UI/main_menu.tscn";

	// Defer the scene change by calling our wrapper method on this node so CallDeferred invokes the method in this script.
	CallDeferred(nameof(DeferredChangeScene), mainMenuPath);

	// Safety: if ChangeSceneToFile doesn't take effect, queue a deferred fallback on this node as well.
	CallDeferred(nameof(DeferredMainMenuFallback), mainMenuPath);
	}

	private void DeferredChangeScene(string path)
	{
		try
		{
			// Diagnostic: list root children before attempting scene change
			try { } catch { }

			var err = GetTree().ChangeSceneToFile(path);
			if (err != Error.Ok)
				GD.PrintErr($"PauseMenu: ChangeSceneToFile failed: {err}");

			// Always try to clean up any lingering pause UI that might persist in the root (canvas or menu nodes)
			try
			{
				Engine.TimeScale = 1f;
				var root = GetTree().Root;
				var canvas = root.GetNodeOrNull<CanvasLayer>("PauseCanvasLayer");
				if (canvas != null)
				{
					canvas.QueueFree();
				}
				var pauseNode = root.GetNodeOrNull<Node>("PauseMenu");
				if (pauseNode != null)
				{
					pauseNode.QueueFree();
				}
			}
			catch (Exception) { }

			// Diagnostic: list root children after deferred change and cleanup
			try { } catch { }
		}
		catch (Exception ex)
		{
			GD.PrintErr("PauseMenu: DeferredChangeScene exception: " + ex.Message);
		}
	}

	private void DeferredMainMenuFallback(string mainMenuPath)
	{
		// If current scene already has the main menu root, nothing to do.
		var cur = GetTree().CurrentScene;
		// Compare by scene root name as a lightweight heuristic to detect if main menu is already active
		if (cur != null && cur.Name == "MainMenu")
			return;

		// Instantiate and replace current scene as a fallback
		var packed = GD.Load<PackedScene>(mainMenuPath);
		if (packed == null)
		{
			GD.PrintErr($"PauseMenu: fallback could not load main menu at {mainMenuPath}");
			return;
		}

		var newScene = packed.Instantiate() as Node;
		if (newScene == null)
		{
			GD.PrintErr("PauseMenu: fallback failed to instantiate main menu");
			return;
		}

		// Free current scene root to avoid stacking
		try { if (GetTree().CurrentScene != null) GetTree().CurrentScene.QueueFree(); } catch { }
		GetTree().Root.AddChild(newScene);
		try { GetTree().CurrentScene = newScene; } catch { }
		

		// Aggressive cleanup: remove lingering Node2D or CanvasLayer children that may be leftover
		try
		{
			var root = GetTree().Root;
			for (int i = root.GetChildCount() - 1; i >= 0; i--)
			{
				var child = root.GetChild(i);
				
				// Keep the newly added scene and all autoloads
				if (child == newScene) continue;
				if (child is PauseController) continue;
				
				// Keep all autoloads by name - don't remove them!
				string childName = child.Name.ToString();
				if (childName == "GlobalRoomChange" || childName == "MusicManager" || 
				    childName == "HUD" || childName == "SettingsManager" || 
				    childName == "AudioManager" || childName == "SaveManager") continue;
				
				// Remove only specific unwanted nodes, not all CanvasLayers
				if (child.Name == "PauseMenu" || child is Node2D)
				{
					try { child.QueueFree(); } catch { }
				}
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr("PauseMenu: error during aggressive cleanup: " + ex.Message);
		}
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}