
This is the readme for the Photon Client SDKs.
(C) Exit Games GmbH 2016



LoadBalancing Documentation
----------------------------------------------------------------------------------------------------
The API reference is included in this package as CHM file.
Find the current manuals, tutorials, API reference and more online:
http://doc.photonengine.com



Running the Demos
----------------------------------------------------------------------------------------------------
Our demos are built for the Photon Cloud for convenience (no initial server setup needed).
The service is free for development and signing up is instant and without obligation.

Every game title on the Cloud gets it's own AppId string which must be copied into the clients.
The demos use a property "AppId" in the source files. Set it's value before you build them.

Sign In:             https://www.photonengine.com/en/Account/SignIn
AppId in Dashboard:  https://www.photonengine.com/dashboard

Each application type has it's own AppId: Realtime, Turnbased and Chat.
You will find specific sections of the Dashboard per application type.


Alternatively you can host a "Photon Cloud" yourself and use any AppId.
Download the server SDK here:
https://www.photonengine.com/OnPremise/Download

How to start the server:
http://doc.photonengine.com/en/onpremise/current/getting-started/photon-server-in-5min




Chat Documentation
----------------------------------------------------------------------------------------------------
    http://doc.photonengine.com/en/chat
    http://forum.photonengine.com


Implementing Photon Chat
----------------------------------------------------------------------------------------------------
    Photon Chat is separated from other Photon Applications, so it needs it's own AppId.
    Our demos usually don't have an AppId set.
    In code, find "<your appid>" to copy yours in. In Unity, we usually use a component
    to set the AppId via the Inspector. Look for the "Scripts" GameObject in the scenes.

    Register your Chat Application in the Dashboard:
    https://www.photonengine.com/en/Chat/Dashboard

    The class ChatClient and the interface IChatClientListener wrap up most important parts
    of the Chat API. Please refer to their documentation on how to use this API.
    More documentation will follow.

    If you use Unity, copy the source from the ChatApi folder into your project.
    It should run in most cases (unless your Photon assembly is very old).




Unity Notes
----------------------------------------------------------------------------------------------------
If you don't use Unity, skip this chapter.


The SDK contains a "PhotonAssets" folder. Copy the content of it into your project's Asset folder.
If you use Unity 5 and plan a Windows Universal export, also copy the "PhotonAssets-U5" folder to 
your project. On top. You need to setup the DLLs in Unity 5 Inspector.


Currently supported export platforms are:
    Standalone (Windows, OSx and Linux)
    Web (Windows and MacOS)
    WebGL
    iOS (Unity 4 needs iOS Pro Unity license)
    Android (Unity 4 needs Android Pro Unity license)
    Windows Store and Phone
    PS3, PS4 and XBox (certified developers should get in contact with us on demand)


All Unity projects must use ExitGames.Client.Photon.Hashtable!
This provides compatibility with Win 8 RT and Win 8 Phone exports.
Add this to your code (at the beginning), to resolve the "ambiguous Hashtable" declaration:
using Hashtable = ExitGames.Client.Photon.Hashtable;


Web players do a policy-file request to port TCP 843 before they connect to a remote server.
The Photon Cloud and Server SDK will handle these requests.
If you host the server, open the additional "policy request" port: TCP 843. If you configure
your server applications, run "Policy Application" for webplayers.


How to add Photon to your Unity project:
1) The Unity SDK contains a "PhotonAssets" folder. 
   Copy the content PhotonAssets it into your project's Asset folder.
2) Make sure to have the following line of code in your scripts to make it run in background:
   Application.runInBackground = true; //without this Photon will loose connection if not focussed
3) Add "using Hashtable = ExitGames.Client.Photon.Hashtable;" to your scripts. Without quotation.
4) iOS build settings (Edit->Project Settings->Player)
   "iPhone Stripping Level" to "Strip Bytecode" and use
   "DotNet 2.0 subset".
   If your project runs fine in IDE but fails on device, check the "DotNet 2.0 Subset" option!
5) Change the server address in the client. A default of "localhost:5055" won't work on device.
6) Implement OnApplicationQuit() and disconnect the client when the app stops.



Windows Phone Notes
----------------------------------------------------------------------------------------------------
To run demos on a smartphone or simulator, you need to setup your server's network address or
use the Photon Cloud.

We assume you run a Photon Server on the same development machine, so by default, our demos have a
address set to "127.0.0.1:5055" and use UDP.
Demos for LoadBalancing or the Cloud will be set to: "app.exitgamescloud.com:5055" and use UDP.

Search the code and replace those values to run the demos on your own servers or in a simulator.
The Peer.Connect() method is usually used to set the server's address.

A smartphone has it's own IP address, so 127.0.0.1 will contact the phone instead of the machine
that also runs Photon.



Xamarin Notes
----------------------------------------------------------------------------------------------------
Edit MainActivity.cs with your own AppId (approx. line 33). Read "Running Loadbalancing Demos".
Alternatively change the ServerAddress to a Photon Server you are hosting. Don't forget the ":port".

Our library dlls are fully Xamarin.iOS, Xamarin.Android and Xamarin.mac compatible,
but the projects to build them are not. So you can't reference the LoadBalancingApi project
directly.
Instead, we reference the dlls to minimize the number of projects to maintain and potential for
desaster.

The demo particle links directly to some of the source files of demo-particle-logic for similar
reasons.

Let us know if our workflow doesn't work for you.




Playstation Mobile Notes
----------------------------------------------------------------------------------------------------
The demo by default connects to the EU Photon Cloud servers. You only need to register to run your
own application (game) on it. This gives you the AppId which must be inserted into:
demo-particle-psm\AppMain.cs