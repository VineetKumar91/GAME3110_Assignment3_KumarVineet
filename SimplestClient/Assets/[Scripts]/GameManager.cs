using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum CurrentMode
{
   Login,
   Lobby,
   GameRoom,
}


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
        currentMode = CurrentMode.Login;

        _instance = this;
    }

    public static GameManager GetInstance()
    {
        return _instance;
    }

    private void Update()
    {
    }

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
