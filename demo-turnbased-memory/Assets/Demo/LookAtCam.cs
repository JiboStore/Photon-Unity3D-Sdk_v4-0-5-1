using UnityEngine;
using System.Collections;

public class LookAtCam : MonoBehaviour
{
    public GameObject[] GameObjects;

    // Use this for initialization
    private void Update()
    {
        foreach (GameObject o in GameObjects)
        {
            o.transform.LookAt(2*o.transform.position - Camera.main.transform.position);
        }
    }
}