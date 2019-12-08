using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPositionDebugClient : MonoBehaviour {
    [SerializeField]
    public NetworkPositionDebugManager manager;
    private string id;
    private static int MAX_NAME_LENGTH = 25;

    // Use this for initialization
    void Start () {
        string newId = this.GetInstanceID().ToString();
        int currentLength = name.Length + newId.Length + 1;
        if (currentLength >= MAX_NAME_LENGTH) {
            this.id = name.Substring(0, MAX_NAME_LENGTH - 1 - newId.Length) + "-" + newId;
        } else {
            this.id = name + "-" + newId;
            for (int i = currentLength; i < MAX_NAME_LENGTH; i++) {
                this.id += "_";
            }
        }
        manager.Register(new NetworkPositionDebugData(this.id, this.transform));
	}
	
	// Update is called once per frame
	void Update () {
        manager.UpdateClient(new NetworkPositionDebugData(this.id, this.transform));
	}

    private void OnDestroy() {
        manager.DeRegister(new NetworkPositionDebugData(this.id, this.transform));
    }
}
