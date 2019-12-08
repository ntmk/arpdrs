using HoloToolkit.Sharing;
using UnityEngine;

/*
 * This class is used for checking if the sharing service is
 * still connected. If it loses connection we throw an error
 */
public class ConnectionCheck : MonoBehaviour {

    [SerializeField]
    GameObject menuController;

    bool HaveConnected = false;
    // Use this for initialization
    void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        if (!HaveConnected) {
            HaveConnected = SharingStage.Instance.IsConnected;
        }

        if (HaveConnected) {
            if (!SharingStage.Instance.IsConnected) {
                menuController.GetComponent<MenuController>().ShowErrorMessage("\nLost Connection to Server.\n", true);
            }
        }
	}
}
