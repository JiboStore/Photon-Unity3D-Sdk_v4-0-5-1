using UnityEngine;
using System.Collections;

[RequireComponent(typeof(InputToEvent))]
public class PointedAtGameObjectInfo : MonoBehaviour 
{
    void OnGUI()
    {
        if (InputToEvent.goPointedAt != null)
        {
            GUI.Label(new Rect(Input.mousePosition.x + 5, Screen.height - Input.mousePosition.y - 15, 300, 30), string.Format("GameObject {0}", InputToEvent.goPointedAt));
        }
    }
}