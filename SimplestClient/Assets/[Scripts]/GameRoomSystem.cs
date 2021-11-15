using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameRoomSystem : MonoBehaviour
{
    // Game Room instance
    private static GameRoomSystem _instance;

    // Get player and spectator list
    private List<string> playerList;
    private List<string> spectatorList;

    public int player1 = 1;
    public int player2 = 2;
    public string player1Symbol = "X";
    public string player2Symbol = "O";

    public bool isPlayer1 = false;

    public bool isMatchActive;

    public List<Button> AllButtons;

    [SerializeField] 
    private GameObject TicTacToeObject;

    [SerializeField] private GameObject buttonPrefab;


    [SerializeField]
    private NetworkedClient networkedClient;

    public static GameRoomSystem GetInstance()
    {
        return _instance;
    }

    void Start()
    {
        _instance = this;

        // ARGH!!! WHY DO I ALWAYS FORGET
        playerList = new List<string>();
        spectatorList = new List<string>();
    }


    private void OnEnable()
    {
        // Send request to ask for players in the game
        RequestPlayerList();
        //RequestSpectatorList();

        if (AllButtons == null)
        {
            AllButtons = new List<Button>();
        }

        //int i = 0;
        //foreach (Transform buttonObject in TicTacToeObject.transform)
        //{
        //    AllButtons.Add(buttonObject.gameObject.GetComponent<Button>());
        //}
        //Debug.Log("Enabled Button count = " + AllButtons.Count);

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

    void Update()
    {
        
    }

    /// <summary>
    /// Handle response from server
    /// </summary>
    /// <param name="msg"></param>
    public void HandleResponseFromServer(string msg)
    {
        string[] receivedMessageSplit = msg.Split(',');
        int signifer = int.Parse(receivedMessageSplit[0]);

        if (signifer == ServerToClientSignifiers.GameRoomPlayersSend)
        {
            GameRoomPlayerSend(receivedMessageSplit);
        }
        else if (signifer == ServerToClientSignifiers.GameRoomSpectatorsSend)
        {

        }
        else if (signifer == ServerToClientSignifiers.Player1TurnReceive)
        {
            ReceivePlayer2Turn(receivedMessageSplit);
        }
        else if (signifer == ServerToClientSignifiers.Player2TurnReceive)
        {
            ReceivePlayer1Turn(receivedMessageSplit);
        }
    }



    /// <summary>
    /// Handle gameroom players sent request
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    private void GameRoomPlayerSend(string[] receivedMessageSplit)
    {
        // iterate through the sent player list
        for (int i = 1; i < receivedMessageSplit.Length; i++)
        {
            playerList.Add(receivedMessageSplit[i]);
        }

        if (playerList[0] == GameManager.currentUsername)
        {
            isPlayer1 = true;
        }
    }

    /// <summary>
    /// Play Turn
    /// </summary>
    public void PlayTurn(Vector2Int buttonPosition)
    {
        string message = "";

        if (isPlayer1)
        {
            message = buttonPosition.x.ToString() + "," + buttonPosition.y.ToString() + ",";

            networkedClient.SendMessageToHost(ClientToServerSignifiers.PlayedPlayer1Turn + "," + message);
        }
        else
        {
            message = buttonPosition.x.ToString() + "," + buttonPosition.y.ToString() + ",";

            networkedClient.SendMessageToHost(ClientToServerSignifiers.PlayedPlayer2Turn + "," + message);
        }
        
    }


    /// <summary>
    /// Request current player list
    /// </summary>
    public void RequestPlayerList()
    {
        string message = "";

        message = ClientToServerSignifiers.GameRoomPlayersRequest + ",";

        if (networkedClient.IsConnected())
        {
            networkedClient.SendMessageToHost(message);
        }
    }


    private void ReceivePlayer2Turn(string[] receivedMessageSplit)
    {
        if (isPlayer1)
        {
            Vector2Int positionPlayed =
                new Vector2Int(int.Parse(receivedMessageSplit[1]),
                    int.Parse(receivedMessageSplit[2]));

            foreach (var button in AllButtons)
            {
                ButtonHandler buttonHandler = button.GetComponent<ButtonHandler>();
                if (positionPlayed == buttonHandler.buttonPosition && button.interactable)
                {
                    button.GetComponentInChildren<Text>().text = player2Symbol;
                    button.interactable = false;
                }
            }
        }
    }

    private void ReceivePlayer1Turn(string[] receivedMessageSplit)
    {
        if (!isPlayer1)
        {
            Vector2Int positionPlayed =
                new Vector2Int(int.Parse(receivedMessageSplit[1]),
                    int.Parse(receivedMessageSplit[2]));


            foreach (var button in AllButtons)
            {
                ButtonHandler buttonHandler = button.GetComponent<ButtonHandler>();
                if (positionPlayed == buttonHandler.buttonPosition && button.interactable)
                {

                    button.GetComponentInChildren<Text>().text = player1Symbol;
                    button.interactable = false;
                }
            }
        }
    }


    /// <summary>
    /// Request current spectators list
    /// </summary>
    public void RequestSpectatorList()
    {
        string message = "";

        if (networkedClient.IsConnected())
        {
            networkedClient.SendMessageToHost(message);
        }
    }
}
