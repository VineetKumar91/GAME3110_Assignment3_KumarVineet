using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;

/// <summary>
/// 1/11/12
/// Integrating login system and chat system from client
/// </summary>
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

    // My tweak for basic chat send
    [SerializeField] private InputField inputFieldToClient; // input field for text entry

    [SerializeField] private Text fromClientTextField; // text field for showing sent message

    /// <summary>
    /// 4th Oct
    /// </summary>
    public LinkedList<PlayerAccount> playerAccountsList;

    /// <summary>
    /// 8th Nov
    /// </summary>
    public LinkedList<PlayerAccount> onlinePlayerAccounts;

    public string filePath;


    // Grid layout update of online player accounts
    public GameObject userPanel;
    public GameObject userpanelText;

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

        onlinePlayerAccounts = new LinkedList<PlayerAccount>();
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

        // Take message from input field and send it to the function for NetworkTransport.Send
        // Also send clientID
        SendMessageToClient(inputFieldToClient.text, clientID1);
        inputFieldToClient.text = "";
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

        // Put the received text into the UI text field
        fromClientTextField.text = "Client: " + msg;

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

            message = playerAccount.username + ",";
        }

        SendMessageToClient(ServerToClientSignifiers.PlayerListSend + "," + message, id);
    }


    /// <summary>
    /// Handle Client Disconnect to remove from current list
    /// </summary>
    /// <param name="id"></param>
    public void ClientDisconnect(int id)
    {
        Debug.Log("Before Removing");
        DebugPurposes_DisplayOnlinePlayerAccounts();

        foreach (var playerAccount in onlinePlayerAccounts)
        {
            if (playerAccount.clientID == id)
            {
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
                break;
            }
        }

        Debug.Log("After Removing");
        DebugPurposes_DisplayOnlinePlayerAccounts();

        // Refresh Client side list
        RefreshClientPlayerUIList();
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
            message = playerAccount.username + ",";
        }

        foreach (var playerAccount in onlinePlayerAccounts)
        {
            SendMessageToClient(ServerToClientSignifiers.PlayerListRefresh + "," + message, playerAccount.clientID);
        }
    }

    /// <summary>
    /// Debug purposes function
    /// </summary>
    private void DebugPurposes_DisplayOnlinePlayerAccounts()
    {
        foreach (var playerAccount in onlinePlayerAccounts)
        {
            Debug.Log(playerAccount.username + "_" + playerAccount.password + "_" + playerAccount.clientID);
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
/// Create Player Account class
/// </summary>
public class PlayerAccount
{
    public string username, password;
    public int clientID;
    public PlayerAccount(string username, string password)
    {
        this.username = username;
        this.password = password;
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

}