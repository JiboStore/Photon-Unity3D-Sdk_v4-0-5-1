using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_ANDROID
public class PushNotificationsAndroid : MonoBehaviour {

	// Use this for initialization
	void Start () {
		InitPushwoosh();
		
		Debug.Log(this.gameObject.name);
		//Debug.Log("push token: " + getPushToken());
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	private static AndroidJavaObject pushwoosh = null;
	
	void InitPushwoosh() {
		if(pushwoosh != null)
			return;
		
		using(var pluginClass = new AndroidJavaClass("com.arellomobile.android.push.PushwooshProxy"))
		pushwoosh = pluginClass.CallStatic<AndroidJavaObject>("instance");
		
		pushwoosh.Call("setListenerName", this.gameObject.name);
	}
 
	public void setIntTag(string tagName, int tagValue)
	{
		pushwoosh.Call("setIntTag", tagName, tagValue);
	}

	public void unregisterDevice()
	{
		pushwoosh.Call("unregisterFromPushNotifications");
	}

	public void setStringTag(string tagName, string tagValue)
	{
		pushwoosh.Call("setStringTag", tagName, tagValue);
        Debug.Log("setting string tag: " + tagName + "=" + tagValue);
	}

	public void setListTag(string tagName, List<object> tagValues)
	{
		AndroidJavaObject tags = new AndroidJavaObject ("com.arellomobile.android.push.TagValues");

		foreach( var tagValue in tagValues )
		{
			tags.Call ("addValue", tagValue);
		}

		pushwoosh.Call ("setListTag", tagName, tags);
	}
	
	public void sendLocation(double lat, double lon)
	{
		pushwoosh.Call("sendLocation", lat, lon);
	}

	public void startTrackingGeoPushes()
	{
		pushwoosh.Call("startTrackingGeoPushes");
	}

	public void stopTrackingGeoPushes()
	{
		pushwoosh.Call("stopTrackingGeoPushes");
	}

	public void clearLocalNotifications()
	{
		pushwoosh.Call("clearLocalNotifications");
	}

	public void scheduleLocalNotification(string message, int seconds)
	{
		pushwoosh.Call("scheduleLocalNotification", message, seconds);
	}

	public void scheduleLocalNotification(string message, int seconds, string userdata)
	{
		pushwoosh.Call("scheduleLocalNotification", message, seconds, userdata);
	}
	
	public string getPushToken()
	{
		return pushwoosh.Call<string>("getPushToken");
	}

	void onRegisteredForPushNotifications(string token)
	{
		//do handling here
		Debug.Log("onRegisteredForPushNotifications: " + token);
	}

	void onFailedToRegisteredForPushNotifications(string error)
	{
		//do handling here
		Debug.Log(error);
	}

	void onPushNotificationsReceived(string payload)
	{
		//do handling here
        Debug.Log("onPushNotificationsReceived: " +payload);
	}
	void OnApplicationPause(bool paused)
	{
		if(paused)
		{
			pushwoosh.Call("onPause");
		}
		else
		{
			pushwoosh.Call("onResume");
		}
	}
}
#endif