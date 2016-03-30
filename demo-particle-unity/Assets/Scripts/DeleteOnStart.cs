using UnityEngine;

/// <summary>This component will destroy the GameObject it is attached to (in CallConnect()).</summary>
public class DeleteOnStart : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Component.DestroyObject(this.gameObject);
	}
}
