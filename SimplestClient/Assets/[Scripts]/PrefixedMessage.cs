using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PrefixedMessage : MonoBehaviour, IPointerClickHandler
{
    [Header("Prefixed Messages")]
    [SerializeField]
    private Text Player1TextField;

    [SerializeField]
    private Text Player2TextField;

    public bool isVisible1 = false;
    public bool isVisible2 = false;

    public float timerStart = 0;
    public float duration = 4;

    /// <summary>
    /// On Pointer Click is Event Trigger
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameRoomSystem.GetInstance().isPlayer)
        {
            if (GameRoomSystem.GetInstance().isPlayer1)
            {
               Player1TextField.text = eventData.pointerClick.gameObject.GetComponentInChildren<Text>().text;
               isVisible1 = true;
               timerStart = Time.time;

               GameRoomSystem.GetInstance().SendPrefixedMessage(Player1TextField.text);
            }
            else
            {
                Player2TextField.text = eventData.pointerClick.gameObject.GetComponentInChildren<Text>().text;
                isVisible2 = true;
                timerStart = Time.time;

                GameRoomSystem.GetInstance().SendPrefixedMessage(Player2TextField.text);
            }
        }
    }


    // Clear after 4 seconds in fixed Update
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
}
