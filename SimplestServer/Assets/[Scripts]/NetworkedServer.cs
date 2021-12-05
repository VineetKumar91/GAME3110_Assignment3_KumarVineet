using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using System;

public class NetworkedServer : MonoBehaviour
{
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;

    // General practice is to select a socket port that is not reserved for our system
    // So go for anything above 5000
    // Also host and client need to connect to the same port
    int socketPort = 5111; // Why this socket port number ??
    int clientID1;


    public LinkedList<PlayerAccount> playerAccountsList;

    public LinkedList<PlayerAccount> onlinePlayerAccounts;

    public string filePath;


    // Grid layout update of online player accounts
    public GameObject userPanel;
    public GameObject userpanelText;

    public List<PlayerAccount> GameRoomPlayerList;
    public List<PlayerAccount> GameRoomSpectatorList;

    // Server side replay
    public List<MovesOrderClass> MovesOrderList;

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the network transport layer with either default settings or custom settings
        // To initialize with custom settings you need GlobalConfig which can be used to
        // tweak settings such as maximum hosts, maxmimum packet size, thread awake timeout et cetera
        NetworkTransport.Init();

        // ConnectionConfig has parameters that should be set when defining a connection between two peers or between a client and a host.
        // There are number of parameters that can be set, the primary ones are given below
        ConnectionConfig config = new ConnectionConfig();

        // Quality-of-Service types for the specific channels -> determines the guarantee of packet delivery, packet loss
        // Different channels for different functions and types of data
        // Currently Using Reliable(3) and Unrealiable(0)
        reliableChannelID = config.AddChannel(QosType.Reliable);
        unreliableChannelID = config.AddChannel(QosType.Unreliable);
        HostTopology topology = new HostTopology(config, maxConnections);
        hostID = NetworkTransport.AddHost(topology, socketPort, null);


        // Create a new Linked List
        playerAccountsList = new LinkedList<PlayerAccount>();

        // create a filepath
        filePath = Application.dataPath + Path.DirectorySeparatorChar + "AccountDatabase.txt";

        // create new online player account LL
        onlinePlayerAccounts = new LinkedList<PlayerAccount>();

        // create new gameroom player list with capacity 2
        GameRoomPlayerList = new List<PlayerAccount>();
        GameRoomPlayerList.Capacity = 2;

        GameRoomSpectatorList = new List<PlayerAccount>();

        // Server side replay
        MovesOrderList = new List<MovesOrderClass>();
    }

    // Update is called once per frame
    void Update()
    {
        int recHostID;
        int recConnectionID;
        int recChannelID;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error = 0;

        // Types of events that NetworkTransport.Receive can give -> connect, data, disconnect, nothing
        NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID,
            out recChannelID, recBuffer, bufferSize, out dataSize, out error);

        switch (recNetworkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Connection, " + recConnectionID);
                break;
            case NetworkEventType.DataEvent:
                // Convert from byte array to string
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                ProcessRecievedMsg(msg, recConnectionID);
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Disconnection, " + recConnectionID);
                ClientDisconnect(recConnectionID);
                break;
        }
    }

    // Button Pressed function to initiate the message to be sent to client
    public void ButtonPress()
    {
        Debug.Log("To ID: " + clientID1);

    }

    // Send message to the client using client ID and textinput's text
    public void SendMessageToClient(string msg, int id)
    {
        byte error = 0;
        // Convert the string to a byte array to send it across the network
        byte[] buffer = Encoding.Unicode.GetBytes(msg);

        // Server to Client send  using host and client ID
        if (NetworkTransport.Send(hostID, id, reliableChannelID, buffer, msg.Length * sizeof(char), out error))
        {
            Debug.Log("Message sent to client successfully."); // Success!
        }
        else
        {
            Debug.Log("Unsuccessful atttempt: ");
            Debug.Log("Host ID: " + hostID); // Host ID
            Debug.Log("recid ID: " + id); // Client ID
            Debug.Log("error: " + error); // Error Code
        }
    }

    /// <summary>
    /// Process received message from client(s)
    /// </summary>
    /// <param name="msg">message received</param>
    /// <param name="id">id</param>
    private void ProcessRecievedMsg(string msg, int id)
    {
        Debug.Log("msg recieved = " + msg + ".  connection id = " + id);

        // Store the client ID (temporary way, optimized one will be researched and implemented later..)
        clientID1 = id;

        string[] receivedMessageSplit = msg.Split(',');
        int signifer = int.Parse(receivedMessageSplit[0]);

        // Perform the function as interpreted by the signifier
        switch (signifer)
        {
            case (ClientToServerSignifiers.CreateAccount):
                CreateAccount(receivedMessageSplit, id);
                break;

            case ClientToServerSignifiers.Login:
                Login(receivedMessageSplit, id);
                break;

            case ClientToServerSignifiers.PlayerListRequest:
                SendPlayerListToClients(receivedMessageSplit, id);
                break;

            case ClientToServerSignifiers.PlayerJoinGameRequest:
                JoinGameRequest(receivedMessageSplit, id);
                break;

            case ClientToServerSignifiers.PlayerSpectateGameRequest:
                SpectateGameRequest(receivedMessageSplit, id);
                break;

            case ClientToServerSignifiers.GameRoomPlayersRequest:
                GameRoomPlayersRequest(receivedMessageSplit, id);
                break;

            case ClientToServerSignifiers.GameRoomSpectatorsRequest:
                GameRoomSpectatorsRequest(receivedMessageSplit, id);
                break;

            case ClientToServerSignifiers.GameRoomSpectatorLeave:
                GameRoomSpectatorLeave(receivedMessageSplit, id);
                break;


            case ClientToServerSignifiers.PlayedPlayer1Turn:
                Player1TurnPlayed(receivedMessageSplit,id);
                break;

            case ClientToServerSignifiers.PlayedPlayer2Turn:
                Player2TurnPlayed(receivedMessageSplit, id);
                break;

            case ClientToServerSignifiers.SpectatorAnnounceWinner:
                SpectatorAnnounceWinner(receivedMessageSplit, id);
                break;

            case ClientToServerSignifiers.GameRoomPlayerLeave:
                GameRoomPlayerLeave(receivedMessageSplit, id);
                break;

            case ClientToServerSignifiers.PrefixedMessageSent:
                PrefixedMessageSent(receivedMessageSplit, id);
                break;

            case ClientToServerSignifiers.LobbySendGlobalMessage:
                LobbySendGlobalMessage(receivedMessageSplit, id);
                break;

            case ClientToServerSignifiers.LobbySendPersonalMessage:
                LobbySendPersonalMessage(receivedMessageSplit, id);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Create account
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    /// <param name="id"></param>
    void CreateAccount(string[] receivedMessageSplit, int id)
    {
        bool isUsernameInuse = false;

        string username = receivedMessageSplit[1];
        string password = receivedMessageSplit[2];


        // Read from file - load up the list
        Account_ReadFromFile();

        Debug.Log("Creating Account");
        // check if player account exists,
        // if yes, creation will fail and respond
        // if no, creation will succeed and respond
        foreach (PlayerAccount playerAcc in playerAccountsList)
        {
            Debug.Log(playerAcc);
            if (username == playerAcc.username)
            {
                // account creation failed
                isUsernameInuse = true;
                break;
            }
        }

        // if username is in use, send response to client
        if (isUsernameInuse)
        {
            SendMessageToClient(ServerToClientSignifiers.AccountCreationFailed + "", id);
        }
        else
        {
            // username not in use, so create a username
            playerAccountsList.AddLast(new PlayerAccount(username, password));

            Account_WriteToFile();

            // Send message to client confirming it worked yayy..
            SendMessageToClient(ServerToClientSignifiers.AccountCreationComplete + "", id);
        }
    }

    /// <summary>
    /// login system
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    /// <param name="id"></param>
    void Login(string[] receivedMessageSplit, int id)
    {
        string username = receivedMessageSplit[1];
        string password = receivedMessageSplit[2];

        bool isUsernameExists = false;

        // Read from file - load up the list
        Account_ReadFromFile();
        PlayerAccount playerAccount = new PlayerAccount(null, null);

        // check if player account exists via username
        // if no, login fails
        // if yes, check password
        foreach (PlayerAccount playerAcc in playerAccountsList)
        {

            if (username == playerAcc.username)
            {
                // account creation failed
                isUsernameExists = true;
                playerAccount.username = playerAcc.username;
                playerAccount.password = playerAcc.password;
                break;
            }
        }

        // If username actually exists
        if (isUsernameExists)
        {
            if (password != playerAccount.password)
            {
                SendMessageToClient(ServerToClientSignifiers.LoginFailedPassword + "", id);
                Debug.Log("Password not right");
            }
            else
            {
                SendMessageToClient(ServerToClientSignifiers.LoginComplete + "", id);

                // If login is successful, set the ID correctly
                // ID IS ALLOCATED TO ONLY ONLINE AND LOGGED IN USERS SINCE THAT IS MODE OF REFERENCE
                playerAccount.clientID = id;
                onlinePlayerAccounts.AddLast(playerAccount);

                // Refresh server side list
                RefreshServerSideList(playerAccount);
                Debug.Log("Login Completed");
            }
        }
        else
        {
            SendMessageToClient(ServerToClientSignifiers.LoginFailedUsername + "", id);
            Debug.Log("Username does not exist!");
        }

    }


    /// <summary>
    /// Update server user panel
    /// </summary>
    void RefreshServerSideList(PlayerAccount playerAccount)
    {
        // Do some server side stuff first
        GameObject userTextForPanel = Instantiate(userpanelText, userPanel.transform);
        userTextForPanel.GetComponent<Text>().text = playerAccount.clientID + ": " + playerAccount.username;

        // Refresh player UI list after 2 seconds
        StartCoroutine("DelayedPlayerListUpdate");
    }

    /// <summary>
    /// Send Player List
    /// </summary>
    void SendPlayerListToClients(string[] receivedMessageSplit, int id)
    {
        string message = "";

        foreach (var playerAccount in onlinePlayerAccounts)
        {
            // skip the requesting user's username 
            if (receivedMessageSplit[1] == playerAccount.username)
            {
                continue;
            }

            message += playerAccount.username + ",";
        }

        SendMessageToClient(ServerToClientSignifiers.PlayerListSend + "," + message, id);
    }


    /// <summary>
    /// Handle Client Disconnect to remove from current list
    /// </summary>
    /// <param name="id"></param>
    public void ClientDisconnect(int id)
    {
        Debug.Log("Before Removing Count = " + onlinePlayerAccounts.Count);
        DebugPurposes_DisplayOnlinePlayerAccounts();

        foreach (var playerAccount in onlinePlayerAccounts.ToList())
        {
            if (playerAccount.clientID == id)
            {
                // Remove from GameRoomPlayer
                foreach (var gameRoomPlayer in GameRoomPlayerList)
                {
                    if (gameRoomPlayer.clientID == id)
                    {
                        GameRoomPlayerList.Remove(gameRoomPlayer);
                        break;
                    }
                }

                // Remove from GameRoomPlayer
                foreach (var gameRoomSpectator in GameRoomSpectatorList)
                {
                    if (gameRoomSpectator.clientID == id)
                    {
                        GameRoomPlayerList.Remove(gameRoomSpectator);
                        break;
                    }
                }

                // Remove from Display
                foreach (Transform child in userPanel.transform)
                {
                    if (child.gameObject.GetComponent<Text>().text ==
                        playerAccount.clientID + ": " + playerAccount.username)
                    {
                        Destroy(child.gameObject);
                        break;
                    }
                }
                onlinePlayerAccounts.Remove(playerAccount);
            }
        }

        Debug.Log("After Removing");
        DebugPurposes_DisplayOnlinePlayerAccounts();

        // Refresh Client side list
        RefreshClientPlayerUIList();

        // Refresh Spectator List
        RefreshSpectatorListForPlayerAndSpectator();
    }

    /// <summary>
    /// Refresh Client Player UI panel on disconnect or new user join
    /// </summary>
    void RefreshClientPlayerUIList()
    {
        // Send Refresh List to Client
        string message = "";

        foreach (var playerAccount in onlinePlayerAccounts)
        {
            message += playerAccount.username + ",";
        }

        foreach (var playerAccount in onlinePlayerAccounts)
        {
            SendMessageToClient(ServerToClientSignifiers.PlayerListRefresh + "," + message, playerAccount.clientID);
        }
    }

    //-------Player JOIN, SPECTATE-------//
    /// <summary>
    /// Join Game Request
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    /// <param name="id"></param>
    void JoinGameRequest(string[] receivedMessageSplit, int id)
    {
        //Debug.Log("ReceivedMessage 0 "+ receivedMessageSplit[0] );
        //Debug.Log("ReceivedMessage 1 "+ receivedMessageSplit[1] );
        if (GameRoomPlayerList.Count < 2) // Check if game room is available
        {
            PlayerAccount joinPlayerAccount = new PlayerAccount();
            foreach (var playerAccount in onlinePlayerAccounts)
            {
                if (playerAccount.username == receivedMessageSplit[1])
                {
                    joinPlayerAccount = playerAccount;
                    break;
                }
            }

            GameRoomPlayerList.Add(joinPlayerAccount);

            // If 1, then waiting
            if (GameRoomPlayerList.Count == 1)
            {
                SendMessageToClient(ServerToClientSignifiers.PlayerJoinGameSendWaiting + ",", id);
            }
            else if (GameRoomPlayerList.Count == 2)
            {
                // If 2, then ready
                foreach (var gameRoomPlayer in GameRoomPlayerList)
                {
                    SendMessageToClient(ServerToClientSignifiers.PlayerJoinGameSendYes + ",", gameRoomPlayer.clientID);
                }
            }
        }
        else    // Game Room occupied
        {
            SendMessageToClient(ServerToClientSignifiers.PlayerJoinGameSendNo + ",", id);
        }
    }


    /// <summary>
    /// Spectate game Request to join the game room as spectator
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    /// <param name="id"></param>
    void SpectateGameRequest(string[] receivedMessageSplit, int id)
    {
        PlayerAccount spectatorRequestAcc = new PlayerAccount();
        foreach (var playerAccount in onlinePlayerAccounts)
        {
            if (playerAccount.username.Equals(receivedMessageSplit[1]))
            {
                spectatorRequestAcc = playerAccount;
                break;
            }
        }

        foreach (var playerAcc in GameRoomSpectatorList)
        {
            if (playerAcc.username.Equals(receivedMessageSplit[1]))
            {
                return;
            }
        }

        // Add it to the spectator list
        GameRoomSpectatorList.Add(spectatorRequestAcc);

        // Match is already ongoing
        if (GameRoomPlayerList.Count >= 2)
        {
            SendMessageToClient(ServerToClientSignifiers.PlayerSpectateGameSend + ",", id);
        }

        if (MovesOrderList.Count > 0)
        {
            SendSpectatorMovesHistory(id);
        }

        RefreshSpectatorListForPlayerAndSpectator();

    }

    /// <summary>
    /// Send move history to the new spectator
    /// </summary>
    /// <param name="id"></param>
    private void SendSpectatorMovesHistory(int id)
    {
        string msg = "";

        foreach (var moves in MovesOrderList)
        {
            msg += moves.moveLocation.x + "," + moves.moveLocation.y + "," + moves.player + ",";
        }

        // Send message to client
        SendMessageToClient(ServerToClientSignifiers.SpectatorMovesHistoryReceive + "," + msg, id);
    }


    /// <summary>
    /// Spectator Leave
    /// </summary>
    private void GameRoomSpectatorLeave(string[] receivedMessageSplit, int id)
    {
        foreach (var playerAccount in GameRoomSpectatorList)
        {
            if (playerAccount.clientID == id)
            {
                GameRoomSpectatorList.Remove(playerAccount);
                break;
            }
        }

        RefreshSpectatorListForPlayerAndSpectator();

        SendMessageToClient(ServerToClientSignifiers.GameRoomSpectatorLeft + ",", id);
    }

    /// <summary>
    /// Game Room Player Leave
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    /// <param name="id"></param>
    private void GameRoomPlayerLeave(string[] receivedMessageSplit, int id)
    {
        string playerName = "";

        foreach (var playerAccount in GameRoomPlayerList)
        {
            if (playerAccount.clientID == id)
            {
                GameRoomPlayerList.Remove(playerAccount);
                break;
            }
        }

        // Send message to other player that opponent has left
        foreach (var playerAccount in GameRoomPlayerList)
        {
            SendMessageToClient(ServerToClientSignifiers.GameRoomPlayerLeft + ",", playerAccount.clientID);
        }

        foreach (var playerAccount in GameRoomSpectatorList)
        {
            SendMessageToClient(ServerToClientSignifiers.GameRoomPlayerLeft + ",", playerAccount.clientID);
        }

        // Clear all list
        GameRoomPlayerList.Clear();
        MovesOrderList.Clear();
        GameRoomSpectatorList.Clear();
    }



    /// <summary>
    /// Refresh spectator list
    /// </summary>
    void RefreshSpectatorListForPlayerAndSpectator()
    {
        string msg = "";

        // create a msg of all spectators
        for (int i = 0; i < GameRoomSpectatorList.Count; i++)
        {
            msg += GameRoomSpectatorList[i].username + ",";
        }

        // send refresh to all spectators, probably not the one that got added right now..
        for (int i = 0; i < GameRoomSpectatorList.Count; i++)
        {
            SendMessageToClient(ServerToClientSignifiers.PlayerSpectatorRefresh + "," + msg, GameRoomSpectatorList[i].clientID);
        }

        // send refresh to all gameroom players
        for (int i = 0; i < GameRoomPlayerList.Count; i++)
        {
            SendMessageToClient(ServerToClientSignifiers.PlayerSpectatorRefresh + "," + msg, GameRoomPlayerList[i].clientID);
        }
    }


    /// <summary>
    /// Current players in game room
    /// </summary>
    void GameRoomPlayersRequest(string[] receivedMessageSplit, int id)
    {
        string msg = "";

        foreach (var playerInGameRoom in GameRoomPlayerList.ToList())
        {
            msg += playerInGameRoom.username + "," ;
        }

        SendMessageToClient(ServerToClientSignifiers.GameRoomPlayersSend + "," + msg, id);

        //Debug.Log("WHAT THE FK AM I SENDING count =  " + GameRoomPlayerList.Count + " Content = " + ServerToClientSignifiers.GameRoomPlayersSend + "," + msg);
    }

    /// <summary>
    /// Current spectators in game room
    /// </summary>
    void GameRoomSpectatorsRequest(string[] receivedMessageSplit, int id)
    {
        string msg = "";

        foreach (var playerAccountSpectator in GameRoomSpectatorList)
        {
            msg += playerAccountSpectator.username + ",";
        }

        SendMessageToClient(ServerToClientSignifiers.PlayerSpectatorRefresh + "," + msg, id);
    }


    // GAME PLAY FUNCTIONS -------
    /// <summary>
    /// Move done by player 1
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    /// <param name="id"></param>
    private void Player1TurnPlayed(string[] receivedMessageSplit, int id)
    {
        Vector2Int positionPlayed =
            new Vector2Int(int.Parse(receivedMessageSplit[1]), 
                int.Parse(receivedMessageSplit[2]));


        int player2ID = 0;

        foreach (var playerAccount in GameRoomPlayerList)
        {
            if (playerAccount.clientID != id)
            {
                player2ID = playerAccount.clientID;
            }
        }

        // Store moves in server too now
        MovesOrderClass move = new MovesOrderClass(positionPlayed,1);
        MovesOrderList.Add(move);
        
        string message = "";

        message = receivedMessageSplit[1] + "," + receivedMessageSplit[2] + ",";
        SendMessageToClient(ServerToClientSignifiers.Player2TurnReceive + "," + message, player2ID);

        message += "X" + ",";

        // send to spectator also
        foreach (var spectator in GameRoomSpectatorList)
        {
            SendMessageToClient(ServerToClientSignifiers.SpectatorTurnReceive + "," + message, spectator.clientID);
        }
    }


    /// <summary>
    /// Move done by player 2
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    /// <param name="id"></param>
    private void Player2TurnPlayed(string[] receivedMessageSplit, int id)
    {
        Vector2Int positionPlayed =
            new Vector2Int(int.Parse(receivedMessageSplit[1]),
                int.Parse(receivedMessageSplit[2]));

        int player1ID = 0;

        foreach (var playerAccount in GameRoomPlayerList)
        {
            if (playerAccount.clientID != id)
            {
                player1ID = playerAccount.clientID;
            }
        }

        // Store moves in server too now
        MovesOrderClass move = new MovesOrderClass(positionPlayed, 2);
        MovesOrderList.Add(move);

        string message = "";

        message = receivedMessageSplit[1] + "," + receivedMessageSplit[2] + ",";
        SendMessageToClient(ServerToClientSignifiers.Player1TurnReceive + "," + message, player1ID);

        message += "O" + ",";
        // send to spectator also
        foreach (var spectator in GameRoomSpectatorList)
        {
            SendMessageToClient(ServerToClientSignifiers.SpectatorTurnReceive + "," + message, spectator.clientID);
        }
    }

    /// <summary>
    /// Handle Prefixed Message
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    /// <param name="id"></param>
    private void PrefixedMessageSent(string[] receivedMessageSplit, int id)
    {
        string msg = "";
        
        // 1 is the player id, 2 is the message
        msg += receivedMessageSplit[1] + "," + receivedMessageSplit[2];

        foreach (var playerAccount in GameRoomPlayerList)
        {
            if (playerAccount.clientID != id)
            {
                SendMessageToClient(ServerToClientSignifiers.PrefixedMessageReceived + "," + msg, playerAccount.clientID);
            }
        }


        // putting condition just for safety
        foreach (var playerAccount in GameRoomSpectatorList)
        {
            if (playerAccount.clientID != id)
            {
                SendMessageToClient(ServerToClientSignifiers.PrefixedMessageReceived + "," + msg, playerAccount.clientID);
            }
        }
    }


    /// <summary>
    /// Announce winner to spectator(s)
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    /// <param name="id"></param>
    private void SpectatorAnnounceWinner(string[] receivedMessageSplit, int id)
    {
        // Clear moves
        MovesOrderList.Clear();

        string winner = receivedMessageSplit[1];

        foreach (var spectator in GameRoomSpectatorList)
        {
            SendMessageToClient(ServerToClientSignifiers.SpectatorAnnounceWinner + "," + winner, spectator.clientID);
        }
    }

    /// <summary>
    /// PM in Lobby
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    /// <param name="id"></param>
    private void LobbySendPersonalMessage(string[] receivedMessageSplit, int id)
    {
        string PMUser = receivedMessageSplit[1];
        string message = "";


        foreach (var playerAccount in onlinePlayerAccounts)
        {
            if (playerAccount.clientID == id)
            {
                message += "From " + playerAccount.username + ": " + receivedMessageSplit[2];
            }
        }

        foreach (var playerAccount in onlinePlayerAccounts)
        {
            if (playerAccount.username.Equals(PMUser))
            {
                SendMessageToClient(ServerToClientSignifiers.LobbyReceivePersonalMessage + "," + message, playerAccount.clientID);
                break;
            }
        }

    }

    /// <summary>
    /// Lobby Global message
    /// </summary>
    /// <param name="receivedMessageSplit"></param>
    /// <param name="id"></param>
    private void LobbySendGlobalMessage(string[] receivedMessageSplit, int id)
    {
        string message = "";

        foreach (var playerAccount in onlinePlayerAccounts)
        {
            if (playerAccount.clientID == id)
            {
                message += playerAccount.username + ": " + receivedMessageSplit[1];
            }
        }

        foreach (var playerAccount in onlinePlayerAccounts)
        {
            if (playerAccount.clientID != id)
            {
                SendMessageToClient(ServerToClientSignifiers.LobbyReceiveGlobalMessage + "," + message, playerAccount.clientID);
            }
        }

    }


    /// <summary>
    /// Debug purposes function
    /// </summary>
    private void DebugPurposes_DisplayOnlinePlayerAccounts()
    {
        // Online players
        foreach (var playerAccount in onlinePlayerAccounts)
        {
            Debug.Log(playerAccount.username + "_" + playerAccount.password + "_" + playerAccount.clientID);
        }

        // Queue
        foreach (var playerAccount in GameRoomPlayerList)
        {
            Debug.Log("GameRoom Queue Players: " + playerAccount.username);
        }
    }


    /// <summary>
    /// Delay of 2s and call refresh client player UI list
    /// </summary>
    /// <returns></returns>
    IEnumerator DelayedPlayerListUpdate()
    {
        yield return new WaitForSeconds(2f);
        RefreshClientPlayerUIList();
    }

    /// <summary>
    /// Functions for writing and reading to file 
    /// </summary>
    void Account_WriteToFile()
    {
        // Streamwriter - write to file
        StreamWriter streamWriter = new StreamWriter(filePath, false);

        foreach (PlayerAccount playerAcc in playerAccountsList)
        {            
            Debug.Log("username = " + playerAcc.username);
            streamWriter.WriteLine(playerAcc.username + ","
                + playerAcc.password + ",");
        
        }

        // Flush for clearing the streamwriter
        streamWriter.Flush();
        
        // Close the stream writer
        streamWriter.Close();
    }

    /// <summary>
    /// Read account details from file
    /// </summary>
    void Account_ReadFromFile()
    {
        // Clear the file contents before loading up the list
        playerAccountsList.Clear();

        StreamReader streamReader = new StreamReader(filePath);
        
        string line;

        while ((line = streamReader.ReadLine()) != null)
        {
            string[] splitLine = line.Split(',');

            string username = splitLine[0];
            string password = splitLine[1];

            playerAccountsList.AddLast(new PlayerAccount(username, password));
        }

        streamReader.Close();
    }
}


/// <summary>
/// STEP 1 of making replay server side!
/// </summary>
public class MovesOrderClass
{
    public Vector2Int moveLocation;
    public int player;

    /// <summary>
    /// Default constructor
    /// </summary>
    public MovesOrderClass()
    { }

    /// <summary>
    /// Parameterized Constructor
    /// </summary>
    /// <param name="mL"></param>
    /// <param name="Player"></param>
    public MovesOrderClass(Vector2Int mL, int Player)
    {
        moveLocation = mL;
        player = Player;
    }
}



/// <summary>
/// Create Player Account class
/// </summary>
public class PlayerAccount
{
    public string username, password;
    public int clientID;

    public PlayerAccount() {}
    public PlayerAccount(string username, string password)
    {
        this.username = username;
        this.password = password;
    }
}


/// <summary>
/// Client to Server Signifiers
/// </summary>
public static class ClientToServerSignifiers
{
    public const int CreateAccount = 1;
    public const int Login = 2;

    public const int PlayerListRequest = 10;
    public const int PlayerListRefresh = 11;

    public const int PlayerJoinGameRequest = 20;
    public const int PlayerSpectateGameRequest = 21;

    public const int GameRoomPlayersRequest = 30;
    public const int GameRoomSpectatorsRequest = 31;
    public const int GameRoomSpectatorLeave = 32;
    public const int GameRoomPlayerLeave = 33;

    public const int PrefixedMessageSent = 40;

    public const int LobbySendGlobalMessage = 50;
    public const int LobbySendPersonalMessage = 51;

    public const int PlayedPlayer1Turn = 100;
    public const int PlayedPlayer2Turn = 101;

    public const int SpectatorAnnounceWinner = 104;

    public const int ReplayListRequest = 60;
}

/// <summary>
/// Server to Client signifiers
/// </summary>
public static class ServerToClientSignifiers
{
    public const int LoginComplete = 1;
    public const int LoginFailedPassword = 2;
    public const int LoginFailedUsername = 3;
    public const int AccountCreationComplete = 4;
    public const int AccountCreationFailed = 5;

    public const int PlayerListSend = 10;
    public const int PlayerListRefresh = 11;

    public const int PlayerJoinGameSendYes = 20;
    public const int PlayerJoinGameSendNo = 21;
    public const int PlayerJoinGameSendWaiting = 22;
    public const int PlayerSpectateGameSend = 23;
    public const int PlayerSpectatorRefresh = 24;

    public const int GameRoomPlayersSend = 30;
    public const int GameRoomSpectatorsSend = 31;
    public const int GameRoomSpectatorLeft = 32;
    public const int GameRoomPlayerLeft = 33;

    public const int PrefixedMessageReceived = 40;

    public const int LobbyReceiveGlobalMessage = 50;
    public const int LobbyReceivePersonalMessage = 51;

    public const int Player2TurnReceive = 100;
    public const int Player1TurnReceive = 101;
    public const int SpectatorTurnReceive = 102;
    public const int SpectatorMovesHistoryReceive = 103;
    public const int SpectatorAnnounceWinner = 104;
}