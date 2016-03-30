using System;
using System.Collections.Generic;
using ExitGames.Client.Photon.Chat;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This simple Chat UI uses a global chat (in lobby) and a room chat (in room).
/// </summary>
/// <remarks>
/// The ChatClient basically lets you create any number of channels. 
/// You just have to name them. Example: "gc" for Global Channel or for rooms: "rc"+RoomName.GetHashCode()
/// 
/// Names of users are set in Authenticate. That should be unique so users can actually get their messages.
/// 
/// 
/// Workflow: 
/// Create ChatClient, Connect to a server with your AppID, Authenticate the user (apply a unique name)
/// and subscribe to some channels. 
/// Subscribe a channel before you publish to that channel!
/// 
/// 
/// Note: 
/// Don't forget to call ChatClient.Service().
/// </remarks>
public class ChatNewGui : MonoBehaviour, IChatClientListener
{
    public string[] ChannelsToJoinOnConnect; // set in inspector. Demo channels to join automatically.
    public int HistoryLengthToFetch; // set in inspector. Up to a certain degree, previously sent messages can be fetched for context

    public string UserName { get; set; }

    private string selectedChannelName; // mainly used for GUI/input
    private bool doingPrivateChat;


    public ChatClient chatClient;


    public RectTransform ChatPanel;     // set in inspector (to enable/disable panel)
    public InputField InputFieldChat;   // set in inspector
    public Text CurrentChannelText;     // set in inspector
    public Toggle ChannelToggleToInstantiate; // set in inspector
    private readonly Dictionary<string, Toggle> channelToggles = new Dictionary<string, Toggle>();

    public bool ShowState = true;
    public Text StateText; // set in inspector

    private string userIdInput = "";
    private static string WelcomeText = "Welcome to chat. Type \\help to list commands.";
    private static string HelpText = "\n\\subscribe <list of channelnames> subscribes channels.\n\\unsubscribe <list of channelnames> leaves channels.\n\\msg <username> <message> send private message to user.\n\\clear clears the current chat tab. private chats get closed.\n\\help gets this help message.";


    public void Start()
    {
        DontDestroyOnLoad(gameObject);
        this.ChatPanel.gameObject.SetActive(true);

        Application.runInBackground = true; // this must run in background or it will drop connection if not focussed.

        if (string.IsNullOrEmpty(UserName))
        {
            UserName = "user" + Environment.TickCount%99; //made-up username
        }

        this.chatClient = new ChatClient(this);
        string chatAppId = ChatSettings.Instance.AppId;
        this.chatClient.Connect(chatAppId, "1.0", new AuthenticationValues(UserName));

        this.ChannelToggleToInstantiate.gameObject.SetActive(false);
        Debug.Log("Connecting as: " + UserName);
    }

    /// <summary>To avoid that the Editor becomes unresponsive, disconnect all Photon connections in OnApplicationQuit.</summary>
    public void OnApplicationQuit()
    {
        if (this.chatClient != null)
        {
            this.chatClient.Disconnect();
        }
    }

    public void Update()
    {
        if (this.chatClient != null)
        {
            this.chatClient.Service(); // make sure to call this regularly! it limits effort internally, so calling often is ok!
        }

        this.StateText.gameObject.SetActive(ShowState); // this could be handled more elegantly, but for the demo it's ok.
    }


    public void OnEnterSend()
    {
        if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
        {
            SendChatMessage(this.InputFieldChat.text);
            this.InputFieldChat.text = "";
        }
    }

    public void OnClickSend()
    {
        if (this.InputFieldChat != null)
        {
            SendChatMessage(this.InputFieldChat.text);
            this.InputFieldChat.text = "";
        }
    }


    private void SendChatMessage(string inputLine)
    {
        if (string.IsNullOrEmpty(inputLine))
        {
            return;
        }

        Debug.Log("chatClient " + (this.chatClient != null));

        if (inputLine[0].Equals('\\'))
        {
            string[] tokens = inputLine.Split(new char[] {' '}, 2);
            if (tokens[0].Equals("\\help"))
            {
                PostHelpToCurrentChannel();
            }
            if (tokens[0].Equals("\\state"))
            {
                int newState = int.Parse(tokens[1]);
                this.chatClient.SetOnlineStatus(newState, new string[] {"i am state " + newState}); // this is how you set your own state and (any) message
            }
            else if (tokens[0].Equals("\\subscribe") && !string.IsNullOrEmpty(tokens[1]))
            {
                this.chatClient.Subscribe(tokens[1].Split(new char[] {' ', ','}));
            }
            else if (tokens[0].Equals("\\unsubscribe") && !string.IsNullOrEmpty(tokens[1]))
            {
                this.chatClient.Unsubscribe(tokens[1].Split(new char[] {' ', ','}));
            }
            else if (tokens[0].Equals("\\clear"))
            {
                if (this.doingPrivateChat)
                {
                    this.chatClient.PrivateChannels.Remove(this.selectedChannelName);
                }
                else
                {
                    ChatChannel channel;
                    if (this.chatClient.TryGetChannel(this.selectedChannelName, this.doingPrivateChat, out channel))
                    {
                        channel.ClearMessages();
                    }
                }
            }
            else if (tokens[0].Equals("\\msg") && !string.IsNullOrEmpty(tokens[1]))
            {
                string[] subtokens = tokens[1].Split(new char[] {' ', ','}, 2);
                string targetUser = subtokens[0];
                string message = subtokens[1];
                this.chatClient.SendPrivateMessage(targetUser, message);
            }
        }
        else
        {
            if (this.doingPrivateChat)
            {
                this.chatClient.SendPrivateMessage(this.userIdInput, inputLine);
            }
            else
            {
                this.chatClient.PublishMessage(this.selectedChannelName, inputLine);
            }
        }
    }

    private void PostHelpToCurrentChannel()
    {
        this.CurrentChannelText.text += HelpText;
    }

    public void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string message)
    {
        if (level == ExitGames.Client.Photon.DebugLevel.ERROR)
        {
            UnityEngine.Debug.LogError(message);
        }
        else if (level == ExitGames.Client.Photon.DebugLevel.WARNING)
        {
            UnityEngine.Debug.LogWarning(message);
        }
        else
        {
            UnityEngine.Debug.Log(message);
        }
    }

    public void OnConnected()
    {
        if (this.ChannelsToJoinOnConnect != null && this.ChannelsToJoinOnConnect.Length > 0)
        {
            this.chatClient.Subscribe(this.ChannelsToJoinOnConnect, this.HistoryLengthToFetch);
        }

        this.chatClient.AddFriends(new string[] {"tobi", "ilya"}); // Add some users to the server-list to get their status updates
        this.chatClient.SetOnlineStatus(ChatUserStatus.Online); // You can set your online state (without a mesage).
    }

    public void OnDisconnected()
    {
    }

    public void OnChatStateChange(ChatState state)
    {
        // use OnConnected() and OnDisconnected()
        // this method might become more useful in the future, when more complex states are being used.

        this.StateText.text = state.ToString();
    }

    public void OnSubscribed(string[] channels, bool[] results)
    {
        // in this demo, we simply send a message into each channel. This is NOT a must have!
        foreach (string channel in channels)
        {
            this.chatClient.PublishMessage(channel, "says 'hi'."); // you don't HAVE to send a msg on join but you could.

            if (this.ChannelToggleToInstantiate != null)
            {
                this.InstantiateChannelButton(channel);

            }
        }

        Debug.Log("OnSubscribed: " + string.Join(", ", channels));

        // select first subscribed channel in alphabetical order
        if (this.chatClient.PublicChannels.Count > 0)
        {
            var l = new List<string>(this.chatClient.PublicChannels.Keys);
            l.Sort();
            string selected = l[0];
            if (this.channelToggles.ContainsKey(selected))
            {
                ShowChannel(selected);
                foreach (var c in this.channelToggles)
                {
                    c.Value.isOn = false;
                }
                this.channelToggles[selected].isOn = true;
                AddMessageToSelectedChannel(WelcomeText);
            }
        }
    }

    private void InstantiateChannelButton(string channelName)
    {
        if (this.channelToggles.ContainsKey(channelName))
        {
            Debug.Log("Skipping creation for an existing channel toggle.");
            return;
        }

        Toggle cbtn = (Toggle)GameObject.Instantiate(this.ChannelToggleToInstantiate);
        cbtn.gameObject.SetActive(true);
        cbtn.GetComponentInChildren<ChannelSelector>().SetChannel(channelName);
        cbtn.transform.SetParent(this.ChannelToggleToInstantiate.transform.parent, false);

        this.channelToggles.Add(channelName, cbtn);
    }

    public void OnUnsubscribed(string[] channels)
    {
        foreach (string channelName in channels)
        {
            if (this.channelToggles.ContainsKey(channelName))
            {
                Toggle t = this.channelToggles[channelName];
                Destroy(t);

                this.channelToggles.Remove(channelName);
            }
        }
    }

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        if (channelName.Equals(this.selectedChannelName))
        {
            // update text
            ShowChannel(this.selectedChannelName);
        }
    }

    public void OnPrivateMessage(string sender, object message, string channelName)
    {
        // as the ChatClient is buffering the messages for you, this GUI doesn't need to do anything here
        // you also get messages that you sent yourself. in that case, the channelName is determinded by the target of your msg
        this.InstantiateChannelButton(channelName);

        if (this.selectedChannelName.Equals(channelName))
        {
            ShowChannel(channelName);
        }
    }

    public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
    {
        // this is how you get status updates of friends.
        // this demo simply adds status updates to the currently shown chat.
        // you could buffer them or use them any other way, too.

        // TODO: add status updates
        //if (activeChannel != null)
        //{
        //    activeChannel.Add("info", string.Format("{0} is {1}. Msg:{2}", user, status, message));
        //}

        Debug.LogWarning("status: " + string.Format("{0} is {1}. Msg:{2}", user, status, message));
    }

    public void AddMessageToSelectedChannel(string msg)
    {
        ChatChannel channel = null;
        bool found = this.chatClient.TryGetChannel(this.selectedChannelName, out channel);
        if (!found)
        {
            Debug.Log("AddMessageToSelectedChannel failed to find channel: " + this.selectedChannelName);
            return;
        }

        if (channel != null)
        {
            channel.Add("Bot", msg);
        }
    }

    

    public void ShowChannel(string channelName)
    {
        if (string.IsNullOrEmpty(channelName))
        {
            return;
        }

        ChatChannel channel = null;
        bool found = this.chatClient.TryGetChannel(channelName, out channel);
        if (!found)
        {
            Debug.Log("ShowChannel failed to find channel: " + channelName);
            return;
        }

        this.selectedChannelName = channelName;
        this.CurrentChannelText.text = channel.ToStringMessages();
        Debug.Log("ShowChannel: " + this.selectedChannelName);
    }
}