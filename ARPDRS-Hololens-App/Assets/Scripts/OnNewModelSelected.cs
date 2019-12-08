using UnityEngine;

/* 
 * Attached to the menu button prefab to deal with on click action
 */
public class OnNewModelSelected : MonoBehaviour {

    SyncObjectSpawner syncObjectSpawner;

    public int buttonIdentfier;

    void Start() {
        syncObjectSpawner = GameObject.Find("Prototypes").GetComponent<SyncObjectSpawner>();
    }
    public void OnClicked() {
        syncObjectSpawner.SpawnBasicSyncObject(buttonIdentfier);
    }
}
