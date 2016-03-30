// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Exit Games GmbH">
//   Exit Games GmbH, 2015
// </copyright>
// <summary>
// Helper script to delete some GameObjects on start (in-Editor help).
// </summary>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;

/// <summary>This component will destroy the GameObject it is attached to (in Start()).</summary>
public class DeleteOnStart : MonoBehaviour {

    // Use this for initialization
    void Start () {
        Component.DestroyObject(this.gameObject);
    }
}
