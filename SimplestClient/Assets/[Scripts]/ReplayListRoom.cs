using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplayListRoom : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Change mode to replay room to get player into the replay list menu
    /// </summary>
    public void ChangeRoomToReplayList()
    {
        GameManager.GetInstance().ChangeMode(CurrentMode.ReplayRoom);
    }

    /// <summary>
    /// If back button is pressed
    /// </summary>
    public void BackButtonPress()
    {
        GameManager.GetInstance().ChangeMode(CurrentMode.Lobby);
    }
}
