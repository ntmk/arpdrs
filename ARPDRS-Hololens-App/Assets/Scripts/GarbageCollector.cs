using UnityEngine;

/*
 * This Class is used for destroying prototypes in the scene.
 */
public class GarbageCollector : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    // Called on destruction
    void OnDestroy() {
        // If last protoype in scene, show main menu
        if (GameObject.Find("Prototypes").transform.childCount == 1) {
            GameObject.Find("MenuController").GetComponent<MenuController>().ShowMainMenu();
        }
        Destroy(this.transform.parent.gameObject);
    }
}
