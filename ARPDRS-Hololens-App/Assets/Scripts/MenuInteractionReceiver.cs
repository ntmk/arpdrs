using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.Receivers;
using UnityEngine;

/*
 * This class handles interactions on menu buttons and what to do
 */
public class MenuInteractionReceiver : InteractionReceiver {
    [SerializeField]
    private MenuController menuController;
    [SerializeField]
    GameObject mainMenuCollection;
    // Use this for initialization
    void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		
	}

    protected override void InputDown(GameObject obj, InputEventData eventData) {
        base.InputDown(obj, eventData);
        if (obj.GetComponent<OnNewModelSelected>() != null) {
            OnNewModelSelected currentModel = obj.GetComponent<OnNewModelSelected>();
            currentModel.OnClicked();
        } else if (obj.GetComponent<RegisteredButton>() != null) {
            switch(obj.name){
                case "CloseMenuButton":
                    menuController.CloseMenu(true);
                    break;
                case "HelpMenuToggleButton":
                    menuController.ShowHelpMenu();
                    break;
                case "OpenMainMenuButton":
                    menuController.ShowMainMenu();
                    break;
                default:
                    break;
            }
        } else { 
            //Input down on a button that does not have a prefab associated with it
        }
    }

    public void Initialize() {
        // Register all Menu Buttons in Main Menu Collection with Receiver
        foreach (Transform child in mainMenuCollection.transform) {
            Registerinteractable(child.gameObject);
        }
    }
}
