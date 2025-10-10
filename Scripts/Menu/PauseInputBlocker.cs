using Godot;
using System;

public partial class PauseInputBlocker : Control
{
    public override void _Ready()
    {
        // Fill the whole viewport
        try { AnchorLeft = 0; AnchorTop = 0; AnchorRight = 1; AnchorBottom = 1; } catch {}
        try { Set("pause_mode", 2); } catch {}
        try { MouseFilter = Control.MouseFilterEnum.Stop; } catch {}
    }

    public override void _GuiInput(InputEvent @event)
    {
        // Forward GUI events to the PauseMenu instance under the same CanvasLayer
        try
        {
            var parent = GetParent();
            if (parent == null) return;
            var pauseMenu = parent.GetNodeOrNull("PauseMenu");
            if (pauseMenu != null)
            {
                // Use Call to invoke the PauseMenu's _GuiInput handler so it sees the event
                pauseMenu.Call("_GuiInput", @event);
            }
        }
        catch (Exception) { }
        base._GuiInput(@event);
    }
}
