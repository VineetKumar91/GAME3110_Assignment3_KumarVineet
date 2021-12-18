using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;

public class GameRoomSystem : MonoBehaviour
{
    // Game Room instance
    private static GameRoomSystem _instance;

    // Get player and spectator list
    private List<string> playerList;
    private List<string> spectatorList;

    [Header("Player")]
    [SerializeField]
    private Text playerName;


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

    [SerializeField]
    private Text currentTurn;

    [SerializeField]
    private Text PresetMessagePlayer1;

    [SerializeField]
    private Text PresetMessagePlayer2;

    [Header("TicTacToe")]
    // Buttons list
    public List<Button> AllButtons;

    [SerializeField]
    private GameObject TicTacToeObject;

    [SerializeField]
    private GameObject buttonPrefab;

    [Header("Networked Client")]
    [SerializeField]
    private NetworkedClient networkedClient;


    [Header("Replay")]
    [SerializeField]
    private ReplaySystem replaySystemRef;
    public bool isReplayOn = false;
    public GameObject savedReplayText;

    [Header("Spectator")]
    public bool isPlayer = false;
    public GameObject SpectatorPanel;
    public GameObject SpectatorUsernameTextObject;

    [Header("Back Button")]
    [SerializeField]
    private Text FinishGameFirst;

    [Header("Exit Game Room")]
    public Text exitGameRoom;

    [Header("Prefixed Messages")]
    [SerializeField]
    private Text Player1TextField;

    [SerializeField]
    private Text Player2TextField;

    public bool isVisible1 = false;
    public bool isVisible2 = false;

    public float timerStart = 0;
    public float duration = 4;

    public static GameRoomSystem GetInstance()
    {
        return _instance;
    }

    void Start()
    {
        _instance = this;

        // ARGH!!! WHY DO I ALWAYS FORGET

        if (playerList == null)
        {
            playerList = new List<string>();
        }

        if (spectatorList == null)
        {
            spectatorList = new List<string>();
        }

        // Array initialization by size
        TicTacToeGame = new int[3, 3];

        isReplayOn = false;

        playerName.text = GameManager.currentUsername;
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

        // Take a second before updating this because of the latency
        StartCoroutine("SetPlayerOrSpectator");

    }

    IEnumerator SetPlayerOrSpectator()
    {
        yield return new WaitForSeconds(2f);
        foreach (var player in playerList)
        {
            if (GameManager.currentUsername == player)
            {
                isPlayer = true;
            }
        }

        foreach (var spectator in spectatorList)
        {
            if (GameManager.currentUsername == spectator)
            {
                foreach (var button in AllButtons)
                {
                    button.interactable = false;
                }
            }
        }
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
            //GameRoomSpectatorsSend(receivedMessageSplit);
        }
        else if (signifer == ServerToClientSignifiers.Player1TurnReceive)
        {
            ReceivePlayer2Turn(receivedMessageSplit);
        }
        else if (signifer == ServerToClientSignifiers.Player2TurnReceive)
        {
            ReceivePlayer1Turn(receivedMessageSplit);
        }
        else if (signifer == ServerToClientSignifiers.PlayerSpectatorRefresh)
        {
            PlayerSpectatorRefresh(receivedMessageSplit);
        }
        else if (signifer == ServerToClientSignifiers.SpectatorMovesHistoryReceive)
        {
            SpectatorMovesHistoryReceive(receivedMessageSplit);
        }
        else if (signifer == ServerToClientSignifiers.GameRoomSpectatorLeft)
        {
            SpectatorIsLeavingNow(receivedMessageSplit);
        }
        else if (signifer == ServerToClientSignifiers.SpectatorTurnReceive)
        {
            SpectatorTurnReceive(receivedMessageSplit);
        }
        else if (signifer == ServerToClientSignifiers.SpectatorAnnounceWinner)
        {
            SpectatorAnnounceWinner(receivedMessageSplit);
        }
        else if (signifer == ServerToClientSignifiers.GameRoomPlayerLeft)
        {
            GameRoomPlayerLeft(receivedMessageSplit);
        }
        else if (signifer == ServerToClientSignifiers.PrefixedMessageReceived)
        {
            PrefixedMessageReceived(receivedMessageSplit);
        }
    }


    /// <summary>
    /// Receive prefixed messages
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    private void PrefixedMessageReceived(string[] receivedMessageSplit)
    {
        int playerNumber = int.Parse(receivedMessageSplit[1]);
        string message = receivedMessageSplit[2];

        if (playerNumber == 1)
        {
            Player1TextField.text = message;
            isVisible1 = true;
            timerStart = Time.time;
        }
        else if (playerNumber == 2)
        {
            Player2TextField.text = message;
            isVisible2 = true;
            timerStart = Time.time;
        }
    }

    /// <summary>
    /// Fixed update for using manual timer
    /// </summary>
    private void FixedUpdate()
    {
        if (isVisible1)
        {
            if (Time.time - timerStart >= 4f)
            {
                Player1TextField.text = "";
                isVisible1 = false;
            }
        }

        if (isVisible2)
        {
            if (Time.time - timerStart >= 4f)
            {
                Player2TextField.text = "";
                isVisible2 = false;
            }
        }
    }

    /// <summary>
    /// Exiting Game Room
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    private void GameRoomPlayerLeft(string[] receivedMessageSplit)
    {
        if (isPlayer)
        {
            exitGameRoom.text = "Opponent Left. Exiting Game Room in 10 seconds...";
        }
        else
        {
            exitGameRoom.text = "Players Left. Exiting Game Room in 10 seconds...";
        }
        exitGameRoom.gameObject.SetActive(true);

        StartCoroutine("ExitGameRoom");
    }

    IEnumerator ExitGameRoom()
    {
        yield return new WaitForSeconds(9f);

        GameManager.GetInstance().ChangeMode(CurrentMode.Lobby);
    }

    /// <summary>
    /// Announce winner for spectator
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    private void SpectatorAnnounceWinner(string[] receivedMessageSplit)
    {
        Debug.Log(receivedMessageSplit[1]);

        if (int.Parse(receivedMessageSplit[1]) == 0)
        {
            currentTurn.text = "It's a tie!";
        }
        else
        {
            currentTurn.text = receivedMessageSplit[1] + " has won.";
        }

    }

    /// <summary>
    /// Receive Move history
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    private void SpectatorMovesHistoryReceive(string[] receivedMessageSplit)
    {
        Vector2Int positionPlayed = new Vector2Int();
        int player = 0;
        string symbol = "A";

        for (int i = 1; i < receivedMessageSplit.Length; i += 3)
        {
            positionPlayed.x = int.Parse(receivedMessageSplit[i]);
            positionPlayed.y = int.Parse(receivedMessageSplit[i + 1]);
            player = int.Parse(receivedMessageSplit[i + 2]);

            if (player == 1)
            {
                symbol = "X";
            }
            else
            {
                symbol = "O";
            }

            int index = 3 * positionPlayed.x + positionPlayed.y;
            AllButtons[index].GetComponentInChildren<Text>().text = symbol;
        }
    }


    /// <summary>
    /// Spectator has to be removed from game room list
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    private void SpectatorIsLeavingNow(string[] receivedMessageSplit)
    {
        foreach (var playerSpectatorAcc in spectatorList)
        {
            if (playerSpectatorAcc == GameManager.currentUsername)
            {
                spectatorList.Remove(playerSpectatorAcc);
                break;
            }
        }

        // Change the mode to lobby
        GameManager.GetInstance().ChangeMode(CurrentMode.Lobby);
    }

    /// <summary>
    /// Refreshes the spectator list
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    private void PlayerSpectatorRefresh(string[] receivedMessageSplit)
    {
        // Clear the list
        spectatorList.Clear();

        // Destroy the spectator UI game objects
        foreach (Transform child in SpectatorPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Add to local list
        for (int i = 1; i < receivedMessageSplit.Length; i++)
        {
            if (receivedMessageSplit[i] != "")
            {
                spectatorList.Add(receivedMessageSplit[i]);
            }
        }

        // Instantiate those game objects
        foreach (var spectatorUsername in spectatorList)
        {
            GameObject spectatorGameObject = Instantiate(SpectatorUsernameTextObject, SpectatorPanel.transform);
            spectatorGameObject.GetComponent<Text>().text = spectatorUsername;
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

        movesDone++;

        WinConditionCheck();

        // Set the warning message as inactive
        if (FinishGameFirst.IsActive())
        {
            FinishGameFirst.gameObject.SetActive(false);
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
    /// Receive turn of the players by the spectator
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    private void SpectatorTurnReceive(string[] receivedMessageSplit)
    {
        Vector2Int positionPlayed =
            new Vector2Int(int.Parse(receivedMessageSplit[1]),
                int.Parse(receivedMessageSplit[2]));

        string symbol = receivedMessageSplit[3];

        foreach (var button in AllButtons)
        {
            ButtonHandler buttonHandler = button.GetComponent<ButtonHandler>();
            if (positionPlayed == buttonHandler.buttonPosition)
            {

                button.GetComponentInChildren<Text>().text = symbol;

                // Tictactoe update
                TicTacToeGame[buttonHandler.buttonPosition.x, buttonHandler.buttonPosition.y] = player1;

                // Update Replay Order
                replaySystemRef.movesOrder.Enqueue(
                    new ReplaySystem.MovesOrderClass(buttonHandler.buttonPosition, player1));

                isPlayer1Turn = false;

                movesDone++;

                if (symbol == "X")
                {
                    currentTurn.text = "Turn: " + playerList[1];
                }
                else
                {
                    currentTurn.text = "Turn: " + playerList[0];
                }
            }
        }
    }


    /// <summary>
    /// Send Prefixed Message
    /// </summary>
    /// <param name="prefixedMessage"></param>
    public void SendPrefixedMessage(string prefixedMessage)
    {
        string msg = "";

        if (isPlayer1)
        {
            msg += "1" + "," + prefixedMessage;
        }
        else
        {
            msg += "2" + "," + prefixedMessage;
        }

        networkedClient.SendMessageToHost(ClientToServerSignifiers.PrefixedMessageSent + "," + msg);
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
            savedReplayText.SetActive(true);
            StartCoroutine("ReplayModePlay");

            // Send message to sever to save replay
            string msg = "";

            msg = playerList[0] + "," + "vs" + "," + playerList[1] + "," + GameManager.currentUsername + ",";

            networkedClient.SendMessageToHost(ClientToServerSignifiers.ReplayListSave + "," + msg);
        }
    }


    /// <summary>
    /// Back button
    /// </summary>
    public void Button_Back()
    {
        // Match is on and you are a player who wants to go back... NO!
        if (isMatchActive && isPlayer)
        {
            FinishGameFirst.gameObject.SetActive(true);
        }
        else if (!isMatchActive && isPlayer)
        {
            // Leave room
            GameManager.GetInstance().ChangeMode(CurrentMode.Lobby);
            string msg = "";
            msg += ClientToServerSignifiers.GameRoomPlayerLeave + ",";

            networkedClient.SendMessageToHost(msg);
        }
        else if (isMatchActive && !isPlayer)
        {
            string msg = "";

            msg += ClientToServerSignifiers.GameRoomSpectatorLeave + ",";

            networkedClient.SendMessageToHost(msg);
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
        Debug.Log("Total Moves done = " + movesDone);
        if (movesDone >= 9)
        {
            Debug.Log("Tie");
            isMatchActive = false;
            currentTurn.text = "It's a tie!";

            string msg = "";

            msg = 0 + ",";

            networkedClient.SendMessageToHost(ClientToServerSignifiers.SpectatorAnnounceWinner + "," + msg);
        }
        else if (
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
            isMatchActive = false;
            currentTurn.text = playerList[0] + " has Won";

            string msg = "";

            msg = playerList[0] + ",";

            networkedClient.SendMessageToHost(ClientToServerSignifiers.SpectatorAnnounceWinner + "," + msg);
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
            isMatchActive = false;
            currentTurn.text = playerList[1] + " has Won";

            string msg = "";

            msg = playerList[1] + ",";

            networkedClient.SendMessageToHost(ClientToServerSignifiers.SpectatorAnnounceWinner + "," + msg);
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
