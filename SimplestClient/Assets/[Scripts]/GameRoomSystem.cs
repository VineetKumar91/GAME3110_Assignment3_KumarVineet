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

    [Header("Player Turns")]
    // Win Condition elements
    public int player1 = 1;
    public int player2 = 2;
    public string player1Symbol = "X";
    public string player2Symbol = "O";

    [SerializeField] public int[,] TicTacToeGame;

    public int movesDone = 0;

    public bool isPlayer1Turn = true;

    // Player 1 or not
    public bool isPlayer1 = false;

    public bool isMatchActive;
    public int winningPlayer = 0;

    [SerializeField] private Text currentTurn;

    [SerializeField] private Text PresetMessagePlayer1;

    [SerializeField] private Text PresetMessagePlayer2;

    [Header("TicTacToe")]
    // Buttons list
    public List<Button> AllButtons;

    [SerializeField] private GameObject TicTacToeObject;

    [SerializeField] private GameObject buttonPrefab;

    [Header("Networked Client")] [SerializeField]
    private NetworkedClient networkedClient;


    [Header("Replay")] [SerializeField] private ReplaySystem replaySystemRef;

    public bool isReplayOn = false;

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

        // Array initialization by size
        TicTacToeGame = new int[3, 3];

        isReplayOn = false;
    }


    private void OnEnable()
    {
        // Send request to ask for players in the game
        RequestPlayerList();
        RequestSpectatorList();

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

        isPlayer1Turn = true;
        isMatchActive = true;
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
            GameRoomSpectatorsSend(receivedMessageSplit);
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
    /// Spectator List Send
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    private void GameRoomSpectatorsSend(string[] receivedMessageSplit)
    {
        for (int i = 1; i < receivedMessageSplit.Length; i++)
        {
            spectatorList.Add(receivedMessageSplit[i]);
        }

        //TODO: Spectator Mode, Prefixed Message
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

            // Set current turn player
            currentTurn.text = "Turn: " + playerList[0];
        }
        else
        {
            isPlayer1 = false;
            currentTurn.text = "Turn: " + playerList[0];
        }

        // Set Preset Player Text
        PresetMessagePlayer1.text = playerList[0];
        PresetMessagePlayer2.text = playerList[1];
    }

    /// <summary>
    /// Play Turn
    /// </summary>
    public void PlayTurn(Vector2Int buttonPosition)
    {
        string message = "";

        // Player 1 Played a turn of tictactoe
        if (isPlayer1)
        {
            message = buttonPosition.x.ToString() + "," + buttonPosition.y.ToString() + ",";

            networkedClient.SendMessageToHost(ClientToServerSignifiers.PlayedPlayer1Turn + "," + message);

            // Update tictactoe
            TicTacToeGame[buttonPosition.x, buttonPosition.y] = player1;

            // Update Replay Order
            replaySystemRef.movesOrder.Enqueue(new ReplaySystem.MovesOrderClass(buttonPosition, player1));


            isPlayer1Turn = false;
            currentTurn.text = "Turn: " + playerList[1];
        }
        else // Player 2 played a turn of tictactoe
        {
            message = buttonPosition.x.ToString() + "," + buttonPosition.y.ToString() + ",";

            networkedClient.SendMessageToHost(ClientToServerSignifiers.PlayedPlayer2Turn + "," + message);

            // Update tictactoe
            TicTacToeGame[buttonPosition.x, buttonPosition.y] = player2;

            // Update Replay Order
            replaySystemRef.movesOrder.Enqueue(new ReplaySystem.MovesOrderClass(buttonPosition, player2));

            isPlayer1Turn = true;
            currentTurn.text = "Turn: " + playerList[0];
        }

        WinConditionCheck();

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


    /// <summary>
    /// Receive player 2's turn by player 1
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
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

                    // Tictactoe update
                    TicTacToeGame[buttonHandler.buttonPosition.x, buttonHandler.buttonPosition.y] = player2;

                    // Update Replay Order
                    replaySystemRef.movesOrder.Enqueue(
                        new ReplaySystem.MovesOrderClass(buttonHandler.buttonPosition, player2));

                    isPlayer1Turn = true;

                    movesDone++;

                    currentTurn.text = "Turn: " + playerList[0];
                }
            }
        }

        WinConditionCheck();
    }

    /// <summary>
    /// Receive Player 1's turn by player 2
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
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

                    // Tictactoe update
                    TicTacToeGame[buttonHandler.buttonPosition.x, buttonHandler.buttonPosition.y] = player1;

                    // Update Replay Order
                    replaySystemRef.movesOrder.Enqueue(
                        new ReplaySystem.MovesOrderClass(buttonHandler.buttonPosition, player1));

                    isPlayer1Turn = false;

                    movesDone++;

                    currentTurn.text = "Turn: " + playerList[1];
                }
            }
        }

        WinConditionCheck();
    }

    /// <summary>
    /// Activate Replay
    /// </summary>
    public void Button_ActivateReplay()
    {
        if (!isMatchActive)
        {
            isReplayOn = true;
            ClearButtons();
            StartCoroutine("ReplayModePlay");
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


    IEnumerator ReplayModePlay()
    {
        ReplaySystem.MovesOrderClass movesOrderTemp = new ReplaySystem.MovesOrderClass();
        while (ReplaySystem.GetInstance().movesOrder.Count > 0)
        {
            yield return new WaitForSeconds(1f);

            // Formula to convert from 3x3 matrix of Vector2Int (x,y) to List index
            // 3x + y
            movesOrderTemp = ReplaySystem.GetInstance().movesOrder.Dequeue();
            if (movesOrderTemp.player == player1)
            {
                int index = 3 * movesOrderTemp.moveLocation.x + movesOrderTemp.moveLocation.y;
                AllButtons[index].GetComponentInChildren<Text>().text = player1Symbol;
            }
            else
            {
                int index = 3 * movesOrderTemp.moveLocation.x + movesOrderTemp.moveLocation.y;
                AllButtons[index].GetComponentInChildren<Text>().text = player2Symbol;
            }
        }
    }


    /// <summary>
    /// Check for win condition
    /// total 8 conditions -
    /// 3 horizontals 00 01 02, 10 11 12, 20 21 22
    /// 3 verticals 00 10 20, 01 11 21, 02 12 22
    /// 2 cross 00 11 22, 20 11 02
    /// </summary>
    public void WinConditionCheck()
    {
        if  (
                //horizontals
                TicTacToeGame[0, 0] == player1 && TicTacToeGame[0, 1] == player1 && TicTacToeGame[0, 2] == player1 ||
                TicTacToeGame[1, 0] == player1 && TicTacToeGame[1, 1] == player1 && TicTacToeGame[1, 2] == player1 ||
                TicTacToeGame[2, 0] == player1 && TicTacToeGame[2, 1] == player1 && TicTacToeGame[2, 2] == player1 ||
                
                // verticals
                TicTacToeGame[0, 0] == player1 && TicTacToeGame[1, 0] == player1 && TicTacToeGame[2, 0] == player1 ||
                TicTacToeGame[0, 1] == player1 && TicTacToeGame[1, 1] == player1 && TicTacToeGame[2, 1] == player1 ||
                TicTacToeGame[0, 2] == player1 && TicTacToeGame[1, 2] == player1 && TicTacToeGame[2, 2] == player1 ||

                 // diagonals
                TicTacToeGame[0, 0] == player1 && TicTacToeGame[1, 1] == player1 && TicTacToeGame[2, 2] == player1 ||
                TicTacToeGame[0, 2] == player1 && TicTacToeGame[1, 1] == player1 && TicTacToeGame[2, 0] == player1
        )
        {
            Debug.Log("Player 1 Won");
            winningPlayer = 1;
            isMatchActive = false;
            currentTurn.text = "Player 1 has Won";
        }
        else if (
            //horizontals
            TicTacToeGame[0, 0] == player2 && TicTacToeGame[0, 1] == player2 && TicTacToeGame[0, 2] == player2 ||
            TicTacToeGame[1, 0] == player2 && TicTacToeGame[1, 1] == player2 && TicTacToeGame[1, 2] == player2 ||
            TicTacToeGame[2, 0] == player2 && TicTacToeGame[2, 1] == player2 && TicTacToeGame[2, 2] == player2 ||

            // verticals
            TicTacToeGame[0, 0] == player2 && TicTacToeGame[1, 0] == player2 && TicTacToeGame[2, 0] == player2 ||
            TicTacToeGame[0, 1] == player2 && TicTacToeGame[1, 1] == player2 && TicTacToeGame[2, 1] == player2 ||
            TicTacToeGame[0, 2] == player2 && TicTacToeGame[1, 2] == player2 && TicTacToeGame[2, 2] == player2 ||

            // diagonals
            TicTacToeGame[0, 0] == player2 && TicTacToeGame[1, 1] == player2 && TicTacToeGame[2, 2] == player2 ||
            TicTacToeGame[0, 2] == player2 && TicTacToeGame[1, 1] == player2 && TicTacToeGame[2, 0] == player2
        )
        {
            Debug.Log("Player 2 Won");
            winningPlayer = 2;
            isMatchActive = false;
            currentTurn.text = "Player 2 has Won";
        }

        // If match is inactive, deactivate all buttons
        if (!isMatchActive)
        {
            foreach (var button in AllButtons)
            {
                button.interactable = false;
            }
        }

    }


    /// <summary>
    /// Request current spectators list
    /// </summary>
    public void RequestSpectatorList()
    {
        string message = "";

        message = ClientToServerSignifiers.GameRoomSpectatorsRequest + ",";

        if (networkedClient.IsConnected())
        {
            networkedClient.SendMessageToHost(message);
        }
    }
}
