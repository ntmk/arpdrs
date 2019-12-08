using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class NetworkPositionDebugData {
    public string id;
    public Transform transform;

    public NetworkPositionDebugData(string id, Transform transform) {
        this.id = id;
        this.transform = transform;
    }
}
