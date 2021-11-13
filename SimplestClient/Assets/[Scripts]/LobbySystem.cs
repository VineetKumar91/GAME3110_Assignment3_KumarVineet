using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    [SerializeField] private List<Message> messagesList;
    public GameObject ChatPanel_ScrollView;
    public GameObject TextObject;
    public InputField ChatInputField;

    // Start is called before the first frame update
    void Start()
    {
        // Create message list
        messagesList = new List<Message>();
        if (networkedClient.IsConnected())
        {
            loadingCircleImage.color = Color.green;
            loadingText.text = "Connected";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    /// <summary>
    /// Handle server response
    /// </summary>
    /// <param name="msg"></param>
    public void HandleResponseFromServer(string msg)
    {

    }


    /// <summary>
    /// Send message
    /// 1. Create locally first
    /// @TODO Do Server next
    /// </summary>
    /// <param name="text"></param>
    public void SendMessageToChat(string text)
    {
        // Create the message and set its text
        Message msg = new Message();
        msg.text = text;

        // Instantiate the text game object to the scroll view
        GameObject textPrefab = Instantiate(TextObject, ChatPanel_ScrollView.transform);

        // get the textPrefab's text component and set it to the list
        msg.textObject = textPrefab.GetComponent<Text>();

        // set the textUI's textObject text to text version here.
        msg.textObject.text = msg.text;

        messagesList.Add(msg);
    }

    /// <summary>
    /// Send Button onClick
    /// </summary>
    public void SendChatButton()
    {
        // If empty, do nothing
        if (ChatInputField.text != "")
        {
            // send the text to chat
            SendMessageToChat(ChatInputField.text);

            // empty the chat input field
            ChatInputField.text = "";
        }
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