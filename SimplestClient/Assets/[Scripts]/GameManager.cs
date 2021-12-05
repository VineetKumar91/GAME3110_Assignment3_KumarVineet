using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Which screen is the player in ENUM
/// </summary>
public enum CurrentMode
{
   Login,
   Lobby,
   GameRoom,
}


/// <summary>
/// Standard Game Mgr with monobehaviour
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    public static CurrentMode currentMode;

    public static string currentUsername;

    [SerializeField] 
    Canvas LoginCanvas;

    [SerializeField] 
    GameObject Lobby;

    [SerializeField]
    GameObject GameRoom;


    private void Start()
    {
        // Initially Start is the current mode
        currentMode = CurrentMode.Login;

        // Reference to self for lazy singleton
        _instance = this;
    }

    // Reference of Game Manager Instance, in case singleton needs to be implemented
    public static GameManager GetInstance()
    {
        return _instance;
    }

    /// <summary>
    /// Change current mode by disabling all the modes except for this mode
    /// </summary>
    /// <param name="currentmode"></param>
    public void ChangeMode(CurrentMode currentmode)
    {
        currentMode = currentmode;

        switch (currentMode)
        {
            case CurrentMode.Login:
                GameRoom.gameObject.SetActive(false);
                Lobby.gameObject.SetActive(false);

                LoginCanvas.gameObject.SetActive(true);
                break;


            case CurrentMode.Lobby:
                LoginCanvas.gameObject.SetActive(false);
                GameRoom.gameObject.SetActive(false);

                Lobby.gameObject.SetActive(true);
                break;

            case CurrentMode.GameRoom:
                LoginCanvas.gameObject.SetActive(false);
                Lobby.gameObject.SetActive(false);

                GameRoom.gameObject.SetActive(true);
                break;
        }
    }

}
