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

    [SerializeField] 
    private Text player1Name;
    [SerializeField] 
    private Text player2Name;

    private string nameOfReplay = "";

    // Replay Room instance
    private static ReplayListRoom _instance;
    
    // Replay System
    [SerializeField]
    private ReplaySystem replaySystemRef;

    private bool isReplayOn = false;

    // Start is called before the first frame update
    void Start()
    {
        _instance = this;

        if (AllButtons == null)
        {
            AllButtons = new List<Button>();
        }

        int row = 0;
        int col = 0;

        for (int i = 0; i < 9; i++)
        {
            GameObject button = Instantiate(buttonPrefab, TicTacToeObject.transform);
            button.GetComponent<ButtonHandler>().buttonPosition = new Vector2Int(row, col);

            AllButtons.Add(button.GetComponent<Button>());

            if (col >= 2)
            {
                row++;
                col = 0;
            }
            else
            {
                col++;
            }
        }
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

        InitializeDropDown();
    }

    /// <summary>
    /// Initialize the drop down menu
    /// </summary>
    private void InitializeDropDown()
    {
        replaylistDropDown.onValueChanged.RemoveAllListeners();
        replaylistDropDown.onValueChanged.AddListener(delegate
        {
            DropDownItemSelected(replaylistDropDown);
        });
    }

    private void DropDownItemSelected(Dropdown dropdown)
    {
        int menuIndex = dropdown.value;
        Debug.Log(dropdown.options[menuIndex].text);

        string message = "";
        
        message = ClientToServerSignifiers.ReplayRequest + "," + dropdown.options[menuIndex].text;

        nameOfReplay = dropdown.options[menuIndex].text;

        if (networkedClient.IsConnected())
        {
            networkedClient.SendMessageToHost(message);
        }
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
        isReplayOn = false;
        ClearButtons();
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
        else if (signifer == ServerToClientSignifiers.ReplaySend)
        {
            // Received replay list from server
            ReceiveReplayFromServer(receivedMessageSplit);
        }
    }

    /// <summary>
    /// Receive Replay from server
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    private void ReceiveReplayFromServer(string[] receivedMessageSplit)
    {
        bool isPlayer1 = true;
        for (int i = 1; i <= receivedMessageSplit.Length - 2; i += 2)
        {
            if (isPlayer1)
            {
                replaySystemRef.movesOrder.Enqueue(new ReplaySystem.MovesOrderClass(new Vector2Int(int.Parse(receivedMessageSplit[i]), int.Parse(receivedMessageSplit[i+1])), 0));
                isPlayer1 = false;
            }
            else
            {
                replaySystemRef.movesOrder.Enqueue(new ReplaySystem.MovesOrderClass(new Vector2Int(int.Parse(receivedMessageSplit[i]), int.Parse(receivedMessageSplit[i + 1])), 1));
                isPlayer1 = true;
            }
        }
    }


    /// <summary>
    /// Activate Replay
    /// </summary>
    public void Button_ActivateReplay()
    {
        string[] playerNames = nameOfReplay.Split('-');
        player1Name.text = playerNames[2] + ":" + " X";
        player2Name.text = playerNames[4] + ":" + " O";

        if (!isReplayOn)
        {
            isReplayOn = true;
            ClearButtons();
            StartCoroutine("ReplayModePlay");
        }
    }

    /// <summary>
    /// A standard coroutine for showing replays
    /// </summary>
    /// <returns></returns>
    IEnumerator ReplayModePlay()
    {
        ReplaySystem.MovesOrderClass movesOrderTemp = new ReplaySystem.MovesOrderClass();
        while (ReplaySystem.GetInstance().movesOrder.Count > 0)
        {
            yield return new WaitForSeconds(1f);

            // Formula to convert from 3x3 matrix of Vector2Int (x,y) to List index
            // 3x + y
            movesOrderTemp = ReplaySystem.GetInstance().movesOrder.Dequeue();
            if (movesOrderTemp.player == 0)
            {
                int index = 3 * movesOrderTemp.moveLocation.x + movesOrderTemp.moveLocation.y;
                AllButtons[index].GetComponentInChildren<Text>().text = "X";
            }
            else
            {
                int index = 3 * movesOrderTemp.moveLocation.x + movesOrderTemp.moveLocation.y;
                AllButtons[index].GetComponentInChildren<Text>().text = "O";
            }
        }
    }

    /// <summary>
    /// Clear and deactivate all buttons
    /// </summary>
    public void ClearButtons()
    {
        foreach (var button in AllButtons)
        {
            button.interactable = false;
            button.GetComponentInChildren<Text>().text = "";
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
        string tempNameOfReplay = "";
        for (int i = 1; i < receivedMessageSplit.Length; i++)
        {
            tempNameOfReplay = receivedMessageSplit[i];
            if (tempNameOfReplay != "")
            {
                replaylistDropDown.options.Add(new Dropdown.OptionData(tempNameOfReplay));
            }
        }
    }
}
