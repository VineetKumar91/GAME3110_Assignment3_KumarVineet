using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour, IPointerClickHandler
{
    // Button Position set
    public Vector2Int buttonPosition;

    private void Start()
    {
        
    }

    /// <summary>
    /// Event Handler for Pointer Click
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        //Debug.Log("Button " + buttonPosition.x  +","+ buttonPosition.y + " Clicked");

        Button button = eventData.pointerClick.gameObject.GetComponent<Button>();
        Text buttonText = eventData.pointerClick.gameObject.GetComponentInChildren<Text>();

        // Check button is interactable
        if (button.interactable)
        {
            // Symbol
            if (GameRoomSystem.GetInstance().isPlayer1)
            {
                buttonText.text = "X";
                GameRoomSystem.GetInstance().PlayTurn(buttonPosition);
            }
            else
            {
                buttonText.text = "O";
                GameRoomSystem.GetInstance().PlayTurn(buttonPosition);
            }

            button.interactable = false;
        }
    }
}
