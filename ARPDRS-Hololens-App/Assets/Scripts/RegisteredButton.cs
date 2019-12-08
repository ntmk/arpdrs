using UnityEngine;

/*
 * This Class is used to register buttons with the reciever
 */
public class RegisteredButton : MonoBehaviour {

    [SerializeField]
    GameObject receiver;
	// Use this for initialization
	void Start () {
        // Register with receiver
        receiver.GetComponent<MenuInteractionReceiver>().Registerinteractable(this.gameObject);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
