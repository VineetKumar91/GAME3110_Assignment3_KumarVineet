using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum CurrentMode
{
   Login,
   GameRoom,
   Game,
   Chat
}


public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    public static CurrentMode currentMode;

    [SerializeField] 
    Canvas LoginCanvas;

    [SerializeField] 
    Canvas GameRoomCanvas;


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
                LoginCanvas.gameObject.SetActive(true);
                break;


            case CurrentMode.GameRoom:
                LoginCanvas.gameObject.SetActive(false);
                GameRoomCanvas.gameObject.SetActive(true);
                break;

            case CurrentMode.Game:

                break;


            case CurrentMode.Chat:

                break;
        }
    }

}
