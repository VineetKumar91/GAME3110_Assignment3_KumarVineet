using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReplayListRoom : MonoBehaviour
{
    [Header("Networked Client")]
    [SerializeField]
    private NetworkedClient networkedClient;

    [Header("Replay Elements")] 
    [SerializeField]
    private Dropdown replaylistDropDown;

    [SerializeField] 
    private Button playButton;

    [Header("TicTacToe")]
    public List<Button> AllButtons;

    [SerializeField]
    private GameObject TicTacToeObject;

    [SerializeField]
    private GameObject buttonPrefab;

    // Replay Room instance
    private static ReplayListRoom _instance;

    // Start is called before the first frame update
    void Start()
    {
        _instance = this;
    }

    /// <summary>
    /// Gets instance of the replay room
    /// </summary>
    /// <returns></returns>
    public static ReplayListRoom GetInstance()
    {
        return _instance;
    }

    /// <summary>
    /// Adding on enable to make sure only the needed functions need to be called during
    /// game object activation
    /// </summary>
    private void OnEnable()
    {
        // When enabled, first thing is to populate the drop down menu
        // Request the replay list from server
        RequestPlayerReplayList();
    }


    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Change mode to replay room to get player into the replay list menu
    /// </summary>
    public void ChangeRoomToReplayList()
    {
        GameManager.GetInstance().ChangeMode(CurrentMode.ReplayRoom);
    }

    /// <summary>
    /// If back button is pressed
    /// </summary>
    public void BackButtonPress()
    {
        GameManager.GetInstance().ChangeMode(CurrentMode.Lobby);
    }


    /// <summary>
    /// Handle response from server
    /// </summary>
    /// <param name="msg"></param>
    public void HandleResponseFromServer(string msg)
    {
        string[] receivedMessageSplit = msg.Split(',');
        int signifer = int.Parse(receivedMessageSplit[0]);

        if (signifer == ServerToClientSignifiers.ReplayListSend)
        {   
            // Received replay list from server
            ReceiveReplayListFromServer(receivedMessageSplit);
        }
    }


    /// <summary>
    /// Request Player Replay List
    /// </summary>
    public void RequestPlayerReplayList()
    {
        string message = "";

        message = ClientToServerSignifiers.ReplayListRequest + ",";

        if (networkedClient.IsConnected())
        {
            networkedClient.SendMessageToHost(message);
        }
    }


    /// <summary>
    /// Receive(d) Replay List from server and process it
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    public void ReceiveReplayListFromServer(string[] receivedMessageSplit)
    {

    }
}
