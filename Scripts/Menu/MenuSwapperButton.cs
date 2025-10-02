using Godot;
using System;

public partial class MenuSwapperButton : Button
{
	// You can assign either a direct Node reference or a NodePath in the inspector.
	[Export] public Node switchToMenu;
	[Export] public NodePath switchToMenuPath;

	public override void _Ready()
	{
		Pressed += onMenuSwapperButtonPressed;
	}

	private void onMenuSwapperButtonPressed()
	{
		var parent = GetParent();
		if (parent == null)
		{
			GD.PrintErr("MenuSwapperButton: parent is null");
			return;
		}

		var grand = parent.GetParent();
		if (!(grand is MenuTab menuTab))
		{
			GD.PrintErr("MenuSwapperButton: could not find MenuTab as parent->parent");
			return;
		}

		// Resolve the target node: prefer direct Node reference, fall back to NodePath
		Node target = null;
		if (switchToMenu != null)
			target = switchToMenu;
		else if (switchToMenuPath != null)
			target = GetNodeOrNull<Node>(switchToMenuPath);

		if (target == null)
		{
			GD.PrintErr("MenuSwapperButton: switchToMenu not set or could not be resolved.");
			return;
		}

		menuTab.onMenuSwapButtonPressed(target.GetIndex());
	}

}
