using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class UsernameText : MonoBehaviour
{
    public bool isSelected = false;

    public Text textComponent;

    public GameObject PlayerTextPanel;
    // Start is called before the first frame update
    void Start()
    {
        textComponent = GetComponent<Text>();
        PlayerTextPanel = transform.parent.gameObject;
    }

    /// <summary>
    /// Toggle Selection
    /// </summary>
    /// <returns></returns>
    public void ToggleSelection()
    {
        foreach (Transform child in PlayerTextPanel.transform)
        {
            if (child.gameObject.GetComponent<UsernameText>() == this)
            {
                continue;
            }

            child.gameObject.GetComponent<UsernameText>().isSelected = false;
            child.gameObject.GetComponent<UsernameText>().textComponent.color = Color.yellow;
        }

        if (!isSelected && textComponent.text != GameManager.currentUsername)
        {
            textComponent.color = new Color(1f, 0.6f, 1f, 1f);
            isSelected = true;
            LobbySystem.GetInstance().PMUser = textComponent.text;
            LobbySystem.GetInstance().isPMUserSelected = true;
        }
        else
        {
            textComponent.color = Color.yellow;
            isSelected = false;
            LobbySystem.GetInstance().PMUser = "";
            LobbySystem.GetInstance().isPMUserSelected = false;
        }
    }
}
