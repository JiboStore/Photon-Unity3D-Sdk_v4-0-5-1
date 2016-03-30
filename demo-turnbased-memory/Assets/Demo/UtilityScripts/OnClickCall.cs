using System;
using UnityEngine;
using System.Collections;

public class OnClickCall : MonoBehaviour
{
    public string CallMessage;
    public object Parameter;

    private void OnClick()
    {
        if (string.IsNullOrEmpty(CallMessage))
        {
            return;
        }

        if (Parameter != null) 
        {
            SendMessageUpwards(CallMessage, Parameter);
        }
        else
        {
            SendMessageUpwards(CallMessage);
        }
    }

    public void MsgLoad(string name)
    {
        Debug.Log("MsgLoad: " + name);
    }
}
