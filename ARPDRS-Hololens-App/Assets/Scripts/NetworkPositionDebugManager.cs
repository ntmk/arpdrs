using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPositionDebugManager : MonoBehaviour {
    private SortedList<string, NetworkPositionDebugData> clients = new SortedList<string, NetworkPositionDebugData>();
    private TextMesh debugText;
    private string headerText = "Object\t\t\t\t\t\t\t\t\t\tLocalPosition\t\tLocalRotation\t\t\t\tGlobalPosition\t\tGlobalRotation\t\tScale";

	// Use this for initialization
	void Start () {
        debugText = this.GetComponentInChildren<TextMesh>();
    }

    // Update is called once per frame
    void Update () {

        string newText = headerText;

		foreach(KeyValuePair<string, NetworkPositionDebugData> client in clients) {
            newText += "\n" + client.Value.id + "\t\t\t" 
                + client.Value.transform.localPosition + "\t\t" 
                + client.Value.transform.localRotation + "\t\t"
                + client.Value.transform.position + "\t\t"
                + client.Value.transform.rotation + "\t\t"
                + client.Value.transform.localScale;
        }

        debugText.text = newText;
	}

    public void Register(NetworkPositionDebugData client) {
        if(client == null) {
            Debug.LogError("You cannot register a null client with NetworkPositionDebugManager");
            return;
        }

        if(clients.ContainsKey(client.id) == true) {
            Debug.LogError(string.Format("Tried to register duplicate client object {0} to NetworkPositionDebugManager", client.id));
            return;
        }

        clients.Add(client.id, client);
    }

    public void UpdateClient(NetworkPositionDebugData client) {
        if (client == null) {
            Debug.LogError("You cannot update a null client with NetworkPositionDebugManager");
            return;
        }

        if (clients[client.id] == null) {
            Debug.LogError(string.Format("Tried to update missing client object {0} to NetworkPositionDebugManager", client.id));
            return;
        }
        clients[client.id] = client;
    }

    public void DeRegister(NetworkPositionDebugData client) {
        if (client == null) {
            Debug.LogError("You cannot de-register a null client with NetworkPositionDebugManager");
            return;
        }

        if (clients[client.id] == null) {
            Debug.LogError(string.Format("Tried to remove a non existent client object {0} in NetworkPositionDebugManager", client.id));
            return;
        }

        clients.Remove(client.id);
    }
}
