using Godot;
using System;

// Clean PauseController: single class, minimal logic to toggle PauseMenu
public partial class PauseController : Node
{
	[Export] public string PauseAction = "ui_cancel";
	private bool _isPaused = false;

	public override void _Input(InputEvent @event)
	{
		// Safety check: don't process if not in tree
		if (!IsInsideTree() || IsQueuedForDeletion())
			return;
			
		if (Input.IsActionJustPressed(PauseAction))
			TogglePause();
	}

	public override void _Ready()
	{
		GD.Print("PauseController: Ready");
	}

	public void TogglePause()
	{
		var root = GetTree().Root;
		var pauseNode = root.GetNodeOrNull<Control>("PauseMenu");

		if (_isPaused)
		{
			if (pauseNode is PauseMenu pm) pm.HideMenu();
			_isPaused = false;
			return;
		}

		if (pauseNode == null)
		{
			var path = "res://Scenes/UI/pause_menu.tscn";
			var packed = GD.Load<PackedScene>(path);
			if (packed == null)
			{
				GD.PrintErr("PauseController: could not load pause_menu.tscn");
				return;
			}

			var inst = packed.Instantiate() as Control;
			if (inst == null) return;
			inst.Name = "PauseMenu";

			var canvas = new CanvasLayer { Name = "PauseCanvasLayer", Layer = 100 };
			root.AddChild(canvas);
			canvas.AddChild(inst);

			void SetPauseModeRecursive(Node n)
			{
				try { n.Set("pause_mode", 2); } catch { }
				if (n is Control c)
				{
					try { c.MouseFilter = Control.MouseFilterEnum.Stop; } catch { }
					try { c.Set("focus_mode", 2); } catch { }
				}
				foreach (var child in n.GetChildren())
					if (child is Node cn) SetPauseModeRecursive(cn);
			}

			SetPauseModeRecursive(inst);
			pauseNode = inst;
		}

		if (pauseNode is PauseMenu pm2)
		{
			pm2.ShowMenu();
			_isPaused = true;
		}
	}
}