using UnityEngine;
using Debug = UnityEngine.Debug;


public class MemoryNotification : MonoBehaviour
{
    public const string TagForUserId = "UID2";
    public const string TagForAppId = "PhotonAppId";


    public void RegisterPushTags()
    {
        // to register for PushNotifications, this demo uses the AppId and the userID as (PushWoosh-)Tags.

        // the playerName can be accessed by the NamePickerGui
        string userId = NamepickerGui.NickName;     // this demo does not use a proper userID yet. we assume NickName, PlayerName and UserID will all be the same

        // to get the appId we have to find the MemoryGui and access the AppId field of it.
        string appId = null;
        MemoryGui mg = GetComponent<MemoryGui>();
        if (mg != null)
        {
            appId = mg.AppId;
        }

        this.SetNameTagForPushNotifications(userId, appId);
    }

    /// <summary>
    /// Use this to set this user's name as tag for push notifications. Call when the name changed.
    /// </summary>
    /// <param name="userName">Unique user ID. In best case verified with Custom Authentication.</param>
    public void SetNameTagForPushNotifications(string userName, string appId)
    {
        #if UNITY_EDITOR || !UNITY_ANDROID
        Debug.LogWarning("Not able to set a push-message-tag because the system doesn't support it. Skipping that.");
        #else
        // without this, the client won't register for push notifications
        // we modified the Assets/Plugins/Android/AndroidManifest.xml with our pushwoosh app values

        PushNotificationsAndroid pn = this.gameObject.AddComponent<PushNotificationsAndroid>(); 
        if (pn != null)
        {
            pn.setStringTag(TagForUserId, userName);
            if (appId != null) pn.setStringTag(TagForAppId, appId);
        }
        else
        {
            Debug.LogError("PushNotificationsAndroid component is missing?!");
        }
#endif
    }
}
