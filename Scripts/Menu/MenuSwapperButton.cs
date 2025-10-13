using Godot;
using System;

public partial class MenuSwapperButton : Button
{
	[Export] Node switchToMenu;

	public override void _Ready()
	{
		Pressed += onMenuSwapperButtonPressed;
	}

	private  void onMenuSwapperButtonPressed()
	{
		if(GetParent().GetParent() is MenuTab menuTab)
		{
			menuTab.onMenuSwapButtonPressed(switchToMenu.GetIndex());
		}
	}

}
