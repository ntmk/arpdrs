using UnityEngine;

/*
 * This class is used to store default values of a game object we want
 * to access later in time. (Ex: Original Scale, rotation, position, ect.) 
 */
public class DefaultValues : MonoBehaviour {

    private Vector3 setScale;
    private Quaternion setRotation;
    private Vector3 modelPosition;

    // Set the original scale and rotation of the prototype
    private void Start() {
        setScale = this.gameObject.transform.localScale;
        setRotation = this.gameObject.transform.rotation;
    }

    private void Update() {
        modelPosition = this.gameObject.transform.position;
    }
    // returns the original scale for the prototype
    public Vector3 getScale() {
        return setScale;
    }

    // return the original rotation for the prototype
    public Quaternion getRotation() {
        return setRotation;
    }

    public Vector3 getPosition() {
        return modelPosition;
    }
}
