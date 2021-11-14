using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRoomSystem : MonoBehaviour
{
    private GameRoomSystem _instance;

    private List<string> playerList;
    private List<string> spectatorList;

    public bool isGameRoomActive;

    public GameRoomSystem GetInstance()
    {
        return _instance;
    }

    void Start()
    {
        _instance = this;
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

        if (signifer == ServerToClientSignifiers.PlayerListSend)
        {
            
        }
    }

    private void ListRequest()
    {
        
    }
}
