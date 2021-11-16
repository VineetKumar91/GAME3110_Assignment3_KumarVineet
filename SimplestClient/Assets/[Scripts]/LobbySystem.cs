using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LobbySystem : MonoBehaviour
{
    [Header("Networked Client")] 
    [SerializeField]
    private NetworkedClient networkedClient;

    [Header("UI Connection Status Elements")]
    // Loading Circle
    [SerializeField]
    Image loadingCircleImage;
    [SerializeField]
    Text loadingText;


    [Header("Chat System")] 
    [SerializeField]
    private List<Message> messagesList;
    public GameObject ChatPanel_ScrollView;
    public GameObject TextObject;
    public InputField ChatInputField;

    public bool isPMUserSelected = false;
    public string PMUser = "";

    [Header("Player System")] 
    [SerializeField]
    private List<string> playerNameList;
    public GameObject UsersPanel;
    public GameObject UsernameTextObject;

    [Header("Join Game System")]
    [SerializeField]
    private Text JoinGameStatus;

    private bool isRequestAlreadySent = false;



    // reference to login instance
    private static LobbySystem _instance;

    // create a return function
    public static LobbySystem GetInstance()
    {
        return _instance;
    }

    // Start is called before the first frame update
    void Start()
    {
        _instance = this;


        // Create message list
        messagesList = new List<Message>();
        if (networkedClient.IsConnected())
        {
            loadingCircleImage.color = Color.green;
            loadingText.text = "Connected";
        }

        // Add the current username as the first one !
        playerNameList.Add(GameManager.currentUsername);
        LoadOurselfInPlayerList();

        JoinGameStatus.text = "Press Join to join game room queue";

        StartCoroutine("DelayedUpdate");
    }


    // Set username when enabled
    private void OnEnable()
    {
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    /// <summary>
    /// Request the player list
    /// </summary>
    void GetPlayerListFromServer()
    {
        string message;

        if (networkedClient.IsConnected())
        {
            message = ClientToServerSignifiers.PlayerListRequest + "," + GameManager.currentUsername;
            networkedClient.SendMessageToHost(message);
        }
    }


    /// <summary>
    /// Handle server response
    /// </summary>
    /// <param name="msg"></param>
    public void HandleResponseFromServer(string msg)
    {
        string[] receivedMessageSplit = msg.Split(',');
        int signifer = int.Parse(receivedMessageSplit[0]);

        if (signifer == ServerToClientSignifiers.PlayerListSend)
        {
            LoadPlayerList(receivedMessageSplit);
        }
        else if (signifer == ServerToClientSignifiers.PlayerListRefresh)
        {
            RefreshPlayerList(receivedMessageSplit);
        }
        else if (signifer == ServerToClientSignifiers.PlayerJoinGameSendWaiting)
        {
            JoinGameStatus.text = "Waiting for Player 2...";
        }
        else if (signifer == ServerToClientSignifiers.PlayerJoinGameSendYes)
        {
            GameManager.GetInstance().ChangeMode(CurrentMode.GameRoom);
            JoinGameStatus.text = "";
        }
        else if (signifer == ServerToClientSignifiers.PlayerJoinGameSendNo)
        {
            JoinGameStatus.text = "Match in Progress, can Spectate";
        }
        else if (signifer == ServerToClientSignifiers.PlayerSpectateGameSend)
        {
            GameManager.GetInstance().ChangeMode(CurrentMode.GameRoom);
            JoinGameStatus.text = "";
        }
        else if (signifer == ServerToClientSignifiers.LobbyReceiveGlobalMessage)
        {
            LobbyReceiveGlobalMessage(receivedMessageSplit);
        }
        else if (signifer == ServerToClientSignifiers.LobbyReceivePersonalMessage)
        {
            LobbyReceivePersonalMessage( receivedMessageSplit);
        }
    }

    /// <summary>
    /// Add self to player list (only for first time)
    /// </summary>
    void LoadOurselfInPlayerList()
    {
        GameObject usernameText = Instantiate(UsernameTextObject, UsersPanel.transform);
        usernameText.GetComponent<Text>().text = GameManager.currentUsername;
    }

    /// <summary>
    /// Load PlayerList Panel
    /// </summary>
    void LoadPlayerList(string[] msg)
    {
        // Clear the player list text in UI Panel first
        foreach (Transform child in UsersPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Clear the player list  
       playerNameList.Clear();

       // Add current username
       playerNameList.Add(GameManager.currentUsername);

       for (int i = 1; i < msg.Length; i++)
       {
            // Skip empty usernames
            if (msg[i] != "")
            {
                playerNameList.Add(msg[i]);
            }
       }

        // Load that UI Panel with updated player list
        int firstUserName = 0;
        foreach (var username in playerNameList)
        {
            GameObject usernameText = Instantiate(UsernameTextObject, UsersPanel.transform);
            usernameText.GetComponent<Text>().text = username;
           //if (firstUserName == 0)
           //{
           //    usernameText.GetComponent<Text>().color = Color.green;
           //}
            Debug.Log("Usernames:" + username);
        }
    }


    /// <summary>
    /// Refresh Player list
    /// </summary>
    /// <param name="msg"></param>
    void RefreshPlayerList(string[] msg)
    {
        // Clear the player list text in UI Panel first
        foreach (Transform child in UsersPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Clear the player list  
        playerNameList.Clear();

        // Add current username
        playerNameList.Add(GameManager.currentUsername);

        for (int i = 1; i < msg.Length; i++)
        {
            // Skip empty usernames
            if (msg[i] != "" && msg[i] != GameManager.currentUsername)
            {
                playerNameList.Add(msg[i]);
            }
        }

        int firstUserName = 0;
        // Load that UI Panel with updated player list
        foreach (var username in playerNameList)
        {
            GameObject usernameText = Instantiate(UsernameTextObject, UsersPanel.transform);
            usernameText.GetComponent<Text>().text = username;
            //if (firstUserName == 0)
            //{
            //    usernameText.GetComponent<Text>().color = Color.green;
            //}

            firstUserName++;
            
            Debug.Log("RefreshUsernames:" + username);
        }
    }

   

    /// <summary>
    /// Join Game Press function
    /// </summary>
    public void Button_JoinGame()
    {
        string message = "";

        if (networkedClient.IsConnected() && !isRequestAlreadySent)
        {
            Debug.Log("Request Sent Once");
            isRequestAlreadySent = true;
            message = ClientToServerSignifiers.PlayerJoinGameRequest + "," + GameManager.currentUsername;
            networkedClient.SendMessageToHost(message);
        }
    }

    /// <summary>
    /// Spectate Game function
    /// </summary>
    public void Button_SpectateGame()
    {
        string message = "";

        if (networkedClient.IsConnected() && !isRequestAlreadySent)
        {
            isRequestAlreadySent = true;
            message = ClientToServerSignifiers.PlayerSpectateGameRequest + "," + GameManager.currentUsername;
            networkedClient.SendMessageToHost(message);
        }
    }

    /// <summary>
    /// PM Player
    /// </summary>
    private void GetPMPlayer()
    {
        if (isPMUserSelected)
        {
            Debug.Log("PM to " +PMUser);
        }
        else
        {
            Debug.Log("PM player not selected");
        }
    }

    // Receive Messages
    private void LobbyReceivePersonalMessage(string[] receivedMessageSplit)
    {
        string message = receivedMessageSplit[1];

        // Create the message and set its text
        Message msg = new Message();

        msg.text = message;

        // Instantiate the text game object to the scroll view
        GameObject textPrefab = Instantiate(TextObject, ChatPanel_ScrollView.transform);

        // get the textPrefab's text component and set it to the list
        msg.textObject = textPrefab.GetComponent<Text>();

        // set the textUI's textObject text to text version here.
        msg.textObject.text = msg.text;

        msg.textObject.color = new Color(1f, 0.6f, 1f, 1f);

        messagesList.Add(msg);
    }

    private void LobbyReceiveGlobalMessage(string[] receivedMessageSplit)
    {
        string message = receivedMessageSplit[1];

        // Create the message and set its text
        Message msg = new Message();

        msg.text = message;

        // Instantiate the text game object to the scroll view
        GameObject textPrefab = Instantiate(TextObject, ChatPanel_ScrollView.transform);

        // get the textPrefab's text component and set it to the list
        msg.textObject = textPrefab.GetComponent<Text>();

        // set the textUI's textObject text to text version here.
        msg.textObject.text = msg.text;

        msg.textObject.color = Color.black;

        messagesList.Add(msg);

    }

    /// <summary>
    /// Send message
    /// 1. Create locally first
    /// @TODO Do Server next
    /// </summary>
    /// <param name="text"></param>
    public void SendMessageToChat(string text, string username)
    {
        // Create the message and set its text
        Message msg = new Message();

        msg.text += text;

        if (isPMUserSelected)
        {
            // Instantiate the text game object to the scroll view
            GameObject textPrefab = Instantiate(TextObject, ChatPanel_ScrollView.transform);

            // get the textPrefab's text component and set it to the list
            msg.textObject = textPrefab.GetComponent<Text>();

            // set the textUI's textObject text to text version here.
            msg.textObject.text = "To " + PMUser + ": " + msg.text;

            msg.textObject.color = new Color(1f, 0.6f, 1f, 1f);
            
            string message = "";

            message += PMUser + "," + text;

            networkedClient.SendMessageToHost(ClientToServerSignifiers.LobbySendPersonalMessage + "," + message);
            
            messagesList.Add(msg);
        }
        else
        {
            // Instantiate the text game object to the scroll view
            GameObject textPrefab = Instantiate(TextObject, ChatPanel_ScrollView.transform);

            // get the textPrefab's text component and set it to the list
            msg.textObject = textPrefab.GetComponent<Text>();

            // set the textUI's textObject text to text version here.
            msg.textObject.text = GameManager.currentUsername + ": " + msg.text;

            msg.textObject.color = Color.black;
            
            string message = "";

            message += msg.text;

            networkedClient.SendMessageToHost(ClientToServerSignifiers.LobbySendGlobalMessage + "," + message);
            
            messagesList.Add(msg);
        }

    }

    /// <summary>
    /// Send Button onClick
    /// </summary>
    public void Button_SendMessage()
    {

        bool isSending = true;

        // If empty, do nothing
        if (ChatInputField.text != "")
        {
            // send the text to chat
            SendMessageToChat(ChatInputField.text,  GameManager.currentUsername);

            // empty the chat input field
            ChatInputField.text = "";
        }
    }

    /// <summary>
    /// Get PlayerList from server after 1.5s delay
    /// </summary>
    /// <returns></returns>
    IEnumerator DelayedUpdate()
    {
        yield return new WaitForSeconds(1.5f);
        GetPlayerListFromServer();
    }
}


/// <summary>
/// Message Class
/// </summary>
[System.Serializable]
public class Message
{
    // text version
    public string text;

    // TextUI of the same
    public Text textObject;
}