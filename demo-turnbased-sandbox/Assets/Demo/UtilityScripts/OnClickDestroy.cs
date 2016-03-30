using UnityEngine;
using System.Collections;

public class OnClickDestroy : MonoBehaviour
{
    private void OnClick()
    {
        GameObject.Destroy(this.gameObject);
    }
}
