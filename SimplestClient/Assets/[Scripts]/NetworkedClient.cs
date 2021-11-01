using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

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
    byte error;
    bool isConnected = false;
    int ourClientID;

    // My tweak for basic chat send
    [SerializeField]
    private InputField inputFieldToServer;      // input field for text entry

    [SerializeField]
    private Text fromServerTextField;           // text field for showing sent message

    // Start is called before the first frame update
    void Start()
    {
        Connect();
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
                    Debug.Log("disconnected.  " + recConnectionID);
                    break;
            }
        }
    }
    
    private void Connect()
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

    // Button Pressed function to send the message to server
    public void ButtonPress()
    {

        // Get text from input field and use NetworkTransport.Send to send it
        SendMessageToHost(inputFieldToServer.text);

        // Remove Text that was written to acknowledge the message was sent (checks remaining to be performed)
        inputFieldToServer.text = "";
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

        fromServerTextField.text = "Server: " + msg;
        Debug.Log("msg recieved = " + msg + ".  connection id = " + id);

        if (int.Parse(msg) == ServerToClientSignifiers.LoginComplete)
        {
            fromServerTextField.text = "Login Successful";
        }
        else if (int.Parse(msg) == ServerToClientSignifiers.LoginFailed)
        {
            fromServerTextField.text = "Login Failed";
        }
        else if (int.Parse(msg) == ServerToClientSignifiers.AccountCreationComplete)
        {
            fromServerTextField.text = "Account successfully created";
        }
        else if(int.Parse(msg) == ServerToClientSignifiers.AccountCreationFailed)
        {
            fromServerTextField.text = "Account creation failed";
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
    public static int CreateAccount = 1;
    public static int Login = 2;
}


public static class ServerToClientSignifiers
{
    public static int LoginComplete = 1;
    public static int LoginFailed = 2;
    public static int AccountCreationComplete = 3;
    public static int AccountCreationFailed = 4;
}