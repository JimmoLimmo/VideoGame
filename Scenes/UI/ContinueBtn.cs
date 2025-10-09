using Godot;
using System.Threading.Tasks;

public partial class ContinueBtn : Button {
	[Export] private NodePath confirmationPopupPath;
	private ConfirmationPopup _popup;

	public override void _Ready() {
		Pressed += OnPressed;
		_popup = GetNodeOrNull<ConfirmationPopup>(confirmationPopupPath);
	}

	private void OnPressed() {
		// if (!SaveSystem.HasSaveData()) {
		// 	_popup?.ShowPopup("No save data found.");
		// 	return;
		// }
		_popup?.ShowInfo("No Save Data Found.");
		return;
		// TODO: Load save data once SaveSystem is complete.
	}
}
