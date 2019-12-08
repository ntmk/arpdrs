using UnityEngine;
using HoloToolkit.Sharing.Spawning;

/*
 * This class is used for spawning one of our prototypes (Assuming it has a syncPrefab class).
 * We pass the number identifier (represents which sync clas the object is registered with)
 */
public class SyncObjectSpawner : MonoBehaviour {
    [SerializeField]
    private PrefabSpawnManager spawnManager = null;

    [SerializeField]
    [Tooltip("Optional transform target, for when you want to spawn the object on a specific parent.  If this value is not set, then the spawned objects will be spawned on this game object.")]
    private Transform spawnParentTransform;

    // Use this for initialization
    private void Awake() {
        if (spawnManager == null) {
            //You need to reference the spawn manager on SyncObjectSpawner
        }

        // If we don't have a spawn parent transform, then spawn the object on this transform.
        if (spawnParentTransform == null) {
            spawnParentTransform = transform;
        }
    }

    //Pss the button identifier that tells us which sync class to use
    public void SpawnBasicSyncObject(int prefabNumber) {

        Vector3 position = new Vector3(0, 0, 0);    // spawn location
        Quaternion rotation = Quaternion.identity;  // zeroed rotation

        switch (prefabNumber) {
            case 1:
                spawnManager.Spawn(new SyncPrefab01(), position, rotation, spawnParentTransform.gameObject, name, false);
                break;
            case 2:
                spawnManager.Spawn(new SyncPrefab02(), position, rotation, spawnParentTransform.gameObject, name, false);
                break;
            case 3:
                spawnManager.Spawn(new SyncPrefab03(), position, rotation, spawnParentTransform.gameObject, name, false);
                break;
            case 4:
                spawnManager.Spawn(new SyncPrefab04(), position, rotation, spawnParentTransform.gameObject, name, false);
                break;
            case 5:
                spawnManager.Spawn(new SyncPrefab05(), position, rotation, spawnParentTransform.gameObject, name, false);
                break;
            case 6:
                spawnManager.Spawn(new SyncPrefab06(), position, rotation, spawnParentTransform.gameObject, name, false);
                break;
            case 7:
                spawnManager.Spawn(new SyncPrefab07(), position, rotation, spawnParentTransform.gameObject, name, false);
                break;
            case 8:
                spawnManager.Spawn(new SyncPrefab08(), position, rotation, spawnParentTransform.gameObject, name, false);
                break;
            case 9:
                spawnManager.Spawn(new SyncPrefab09(), position, rotation, spawnParentTransform.gameObject, name, false);
                break;
            case 10:
                spawnManager.Spawn(new SyncPrefab10(), position, rotation, spawnParentTransform.gameObject, name, false);
                break;
            default:
                //Cannot instantiate prefab: Unexpected prefab name
                break;
        }
    }
}
