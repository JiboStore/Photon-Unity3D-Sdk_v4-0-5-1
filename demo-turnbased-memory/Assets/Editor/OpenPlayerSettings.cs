using UnityEngine;
using System.Collections;
using UnityEditor;

public class OpenPlayerSettings : MonoBehaviour {

    [MenuItem("Window/Open Player Prefs &b")]
    public static void Open()
    {
        EditorApplication.ExecuteMenuItem("Edit/Project Settings/Player");
    }

}
