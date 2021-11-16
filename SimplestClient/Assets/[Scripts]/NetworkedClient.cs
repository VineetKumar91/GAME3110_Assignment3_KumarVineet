using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// 1/11/12
/// Integrating login system and chat system
/// </summary>
public class NetworkedClient : MonoBehaviour
{
    int connectionID;
    int maxConnections = 1000;
    int reliableChannelID;
    int unreliableChannelID;
    int hostID;

    // General practice is to select a socket port that is not reserved for our system
    // So go for anything above 5000
    // Also host and client need to connect to the same port
    int socketPort = 5111;
    byte error = 1;
    private bool isConnected;
    public int ourClientID;

    [SerializeField]
    private Text fromServerTextField;           // text field for showing sent message

    // Start is called before the first frame update
    void Start()
    {
        ourClientID = -1;
        //Connect();
    }

    // Update is called once per frame
    void Update()
    {
        //if(Input.GetKeyDown(KeyCode.S))
        //    SendMessageToHost("Hello from client");

        UpdateNetworkConnection();

        // Press X to disconnect
        // Just testing if disconnect works <- It does
        //if (Input.GetKeyDown(KeyCode.X))
        //    Disconnect();
    }

    private void UpdateNetworkConnection()
    {
        if (isConnected)
        {
            int recHostID;
            int recConnectionID;
            int recChannelID;
            byte[] recBuffer = new byte[1024];
            int bufferSize = 1024;
            int dataSize;
            
           

            // Types of events that NetworkTransport.Receive can give -> connect, data, disconnect, nothing
            NetworkEventType recNetworkEvent = NetworkTransport.Receive(out recHostID, out recConnectionID, out recChannelID, recBuffer, bufferSize, out dataSize, out error);
            
            switch (recNetworkEvent)
            {
                case NetworkEventType.ConnectEvent:
                    Debug.Log("connected.  " + recConnectionID);
                    ourClientID = recConnectionID;
                    break;
                case NetworkEventType.DataEvent:
                    // Convert from byte array to string
                    string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                    ProcessRecievedMsg(msg, recConnectionID);

                    // Receive message from server
                    Debug.Log("got msg = " + msg);
                    break;
                case NetworkEventType.DisconnectEvent:
                    isConnected = false;
                    ourClientID = -1;
                    Debug.Log("disconnected.  " + recConnectionID);
                    break;
            }
        }
    }
    
    public void Connect()
    {

        if (!isConnected)
        {
            Debug.Log("Attempting to create connection");

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

            // Host Topology
            HostTopology topology = new HostTopology(config, maxConnections);

            // Sept 20: 0 socket number -> find any free/available socket
            hostID = NetworkTransport.AddHost(topology, 0);
            Debug.Log("Socket open.  Host ID = " + hostID);

            connectionID = NetworkTransport.Connect(hostID, "192.168.0.10", socketPort, 0, out error); // server is local on network

            if (error == 0)
            {
                isConnected = true;
            
                Debug.Log("Connected, id = " + connectionID);
            }
        }
    }
    
    public void Disconnect()
    {
        Debug.Log("Disconnect function");
        // Disconnect will send a disconnect signal to the peer with the given connection ID (second parameter)
        // which will make the peer end the connection
        NetworkTransport.Disconnect(hostID, connectionID, out error);
    }
    
    // Send message to host using the host ID and the text received
    public void SendMessageToHost(string msg)
    {
        // Convert the string to a byte array to send it across the network
        byte[] buffer = Encoding.Unicode.GetBytes(msg);

        // Send Data across the network 
        NetworkTransport.Send(hostID, connectionID, reliableChannelID, buffer, msg.Length * sizeof(char), out error);
    }

    // Received message's string will be put the text field
    private void ProcessRecievedMsg(string msg, int id)
    {

        //fromServerTextField.text = "Server: " + msg;
        Debug.Log("msg recieved = " + msg + ".  connection id = " + id);


        // Receive server message and handle appropriately w.r.t. in which room
        // the player is currently
        if (GameManager.currentMode == CurrentMode.Login)
        {
            LoginSystem.GetInstance().HandleResponseFromServer(msg);
        }
        else if(GameManager.currentMode == CurrentMode.Lobby)
        {
            LobbySystem.GetInstance().HandleResponseFromServer(msg);
        }
        else if (GameManager.currentMode == CurrentMode.GameRoom)
        {
            GameRoomSystem.GetInstance().HandleResponseFromServer(msg);
        }
    }

    public bool IsConnected()
    {
        return isConnected;
    }
}


/// <summary>
/// LOGIN/CREATE ACCOUNT - CLIENT TO SERVER, SERVER TO CLIENT 
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

    public const int PlayedPlayer1Turn = 100;
    public const int PlayedPlayer2Turn = 101;

    public const int SpectatorAnnounceWinner = 104;
}

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

    public const int Player2TurnReceive = 100;
    public const int Player1TurnReceive = 101;
    public const int SpectatorTurnReceive = 102;
    public const int SpectatorMovesHistoryReceive = 103;
    public const int SpectatorAnnounceWinner = 104;
}