using Godot;
using System;

public partial class MenuTab : PanelContainer
{
    private MainMenuManager mainMenu;

    public override void _Ready()
    {
        if (GetParent() is MainMenuManager)
        {
            mainMenu = GetParent() as MainMenuManager;
        }
    }

    public void onMenuSwapButtonPressed(int swapIndex)
    {
        mainMenu.swapMenu(swapIndex, GetIndex());
        Visible = false;
    }

    public void onMenuReturnButtonPressed()
    {
        mainMenu.swapMenuToPrevious();
        Visible = false;
    }

    public void loadSceneRequest(PackedScene loadScene)
    {
        mainMenu.onSwapScene(loadScene);
    }

}
