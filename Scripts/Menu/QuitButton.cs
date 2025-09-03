using Godot;
using System;

public partial class QuitButton : Button
{
    public override void _Ready()
    {
        Pressed += onQuitButtonPressed;
    }

    private void onQuitButtonPressed()
    {
        GetTree().Quit();
    }
}
