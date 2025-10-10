using Godot;

public partial class PauseInputBlocker : Control
{
	// Purpose: forward GUI input to the pause menu
	
	public override void _GuiInput(InputEvent @event)
	{
		// Safety check: don't process if not in tree
		if (!IsInsideTree() || IsQueuedForDeletion())
			return;
			
		// Find the pause menu and forward input to it
		var pauseMenu = GetNodeOrNull<PauseMenu>("../PauseMenu");
		if (pauseMenu != null)
		{
			pauseMenu._GuiInput(@event);
		}
	}
}