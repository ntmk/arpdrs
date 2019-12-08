using UnityEngine;

/*
 * This class is a controller for handling UI elements in the scene.
 * ALL state changes of menus and activities should be handled through this class.
 */
public class MenuController : MonoBehaviour {
    // Menu State Machine
    private enum CurrentMenuState {  NetworkLoading, MainMenu, HelpMenu, Error, None };
    private CurrentMenuState currentMenu;
    private CurrentMenuState previousMenu = CurrentMenuState.None;
    private bool fatalError;

    //****
    // The general UI Scenes
    //****
    // The Main Scene UI
    [SerializeField]
    private GameObject mainSceneUI;
    // The Network Loading Scene UI
    [SerializeField]
    private GameObject networkLoadingSceneUI;

    //****
    // The specific UI panels
    //****
    // The Network Loading Scene UI
    [SerializeField]
    private GameObject networkStatusMenu;

    // The Main Scene UI
    // The Main Menu
    [SerializeField]
    private GameObject mainMenu;
    // The Help Menu
    [SerializeField]
    private GameObject helpMenu;
    [SerializeField]
    private GameObject errorMenu;
    [SerializeField]
    private GameObject UI;
    [SerializeField]
    private GameObject helpMenuToggleButton;
    [SerializeField]
    private GameObject closeMenuButton;
    [SerializeField]
    private GameObject errorMessageBody;
    [SerializeField]
    private GameObject openMainMenuButton;

    void Start() {
        // start with network open, main closed
        UI.SetActive(true);
        networkLoadingSceneUI.SetActive(true);
        mainSceneUI.SetActive(false);

        // setup the networkSceneUI
        networkStatusMenu.SetActive(true);

        // setup the mainSceneUI
        // close all menus except network menu
        helpMenu.SetActive(false);
        errorMenu.SetActive(false);
        mainMenu.SetActive(false);

        // disable all buttons
        helpMenuToggleButton.SetActive(false);
        closeMenuButton.SetActive(false);
        openMainMenuButton.SetActive(false);

        // networking starts open
        currentMenu = CurrentMenuState.NetworkLoading;
    }

    // Displays the help menu
    public void ShowHelpMenu() {
        // Display Help Menu
        // Save old menu
        SaveMenuState();
        currentMenu = CurrentMenuState.HelpMenu;
        helpMenuToggleButton.SetActive(false);
        DisplayMenu(helpMenu);
    }

    // Displays the main menu
    public void ShowMainMenu() {
        // Inactive unless no menu open
        if (currentMenu != CurrentMenuState.None) {
            return;
        }

        // Display Main Menu
        currentMenu = CurrentMenuState.MainMenu;
        DisplayMenu(mainMenu);
    }

    public void ShowErrorMessage(string message, bool isFatal = true) {
        message = HelperFunctions.ResolveTextSize(message);
        SaveMenuState();

        //mainSceneUI.SetActive(false);
        networkLoadingSceneUI.SetActive(false);

        currentMenu = CurrentMenuState.Error;
        helpMenuToggleButton.SetActive(false);

        errorMessageBody.GetComponent<TextMesh>().text = message;

        if (isFatal) {
            fatalError = isFatal;
            errorMessageBody.GetComponent<TextMesh>().text += HelperFunctions.ResolveTextSize("\n\nThis error is fatal!  This means the application cannot continue running.\n\nThe app will close when you close this message.");
        }

        DisplayMenu(errorMenu);
        //Time.timeScale = 0;
    }

    // Saves the current menu state for restore use
    private void SaveMenuState() {
        previousMenu = currentMenu;
        CloseMenu();
    }

    // Display selected Menu
    private void DisplayMenu(GameObject menu) {
        menu.SetActive(true);
        UpdateMenuPosition();

        closeMenuButton.SetActive(true);
        openMainMenuButton.SetActive(false);
    }

    private void UpdateMenuPosition() {
        Vector3 cameraPostion = Camera.main.transform.forward;
        cameraPostion.z += 1.5f;

        // Re-center the UI GameObject
        UI.transform.position = cameraPostion;
    }

    

    // Hides the current menu
    public void CloseMenu(bool CloseButton = false) {
        switch (currentMenu) {
            case CurrentMenuState.NetworkLoading:
                networkLoadingSceneUI.SetActive(false);
                mainSceneUI.SetActive(true);
                helpMenuToggleButton.SetActive(true);
                openMainMenuButton.SetActive(true);
                currentMenu = CurrentMenuState.None;

                if (previousMenu == CurrentMenuState.NetworkLoading) {
                    ShowMainMenu();
                }

                break;
            case CurrentMenuState.HelpMenu:
                currentMenu = CurrentMenuState.None;
                helpMenu.SetActive(false);
                helpMenuToggleButton.SetActive(true);
                if(previousMenu == CurrentMenuState.MainMenu) {
                    previousMenu = CurrentMenuState.None;
                    ShowMainMenu();
                } else {
                    previousMenu = CurrentMenuState.None;
                    closeMenuButton.SetActive(false);
                    openMainMenuButton.SetActive(true);
                }
                break;
            case CurrentMenuState.MainMenu:
                mainMenu.SetActive(false);
                closeMenuButton.SetActive(false);
                openMainMenuButton.SetActive(true);
                currentMenu = CurrentMenuState.None;
                break;
            case CurrentMenuState.Error:
                errorMenu.SetActive(false);
                closeMenuButton.SetActive(false);
                helpMenuToggleButton.SetActive(true);

                if (fatalError && CloseButton) {
                    Debug.Log("Closing Application");
                    Application.Quit();
                    
                } else {
                    // Open Main Menu after error
                    Time.timeScale = 1;
                    mainSceneUI.SetActive(true);
                    ShowMainMenu();
                }

                break;
            case CurrentMenuState.None:
                //Do nothing
                break;
            default:
                break;
        }
    }
}
