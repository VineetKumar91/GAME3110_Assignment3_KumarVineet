using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    GameObject username_InputField;
    GameObject password_InputField;
    GameObject submit_Button;
    GameObject login_Toggle;
    GameObject create_Toggle;

    GameObject networkedClient;

    // Start is called before the first frame update
    void Start()
    {
        GameObject[] ObjectsOfScene = FindObjectsOfType<GameObject>();

        foreach (GameObject gameobject in ObjectsOfScene)
        {
            if(gameobject.name == "UsernameInputField")
            {
                username_InputField = gameobject;
            }
            else if (gameobject.name == "PasswordInputField")
            {
                password_InputField = gameobject;
            }
            else if (gameobject.name == "SubmitButton")
            {
                submit_Button = gameobject;
            }
            else if(gameobject.name == "LoginToggle")
            {
                login_Toggle = gameobject;
            }
            else if(gameobject.name == "CreateToggle")
            {
                create_Toggle = gameobject;
            }
            else if(gameobject.name == "NetworkedClient")
            {
                networkedClient = gameobject;
            }    
        }

        submit_Button.GetComponent<Button>().onClick.AddListener(OnSubmitPressed);
        login_Toggle.GetComponent<Toggle>().onValueChanged.AddListener(LoginToggleChanged);
        create_Toggle.GetComponent<Toggle>().onValueChanged.AddListener(CreateToggleChanged);

        // Login by default
        login_Toggle.GetComponent<Toggle>().isOn = true;
    }


    // Update is called once per frame
    void Update()
    {
        
    }


    /// <summary>
    /// Submit button pressed listener
    /// </summary>
    public void OnSubmitPressed()
    {
        // Submit button pressed
        Debug.Log("Submit button pressed.");

        string username = username_InputField.GetComponent<InputField>().text;
        string password = password_InputField.GetComponent<InputField>().text;

        string message;

        // use signifiers for giving the first character of data sent
        // to distinguish between create and login for server
        if(create_Toggle.GetComponent<Toggle>().isOn)
        {
            message = ClientToServerSignifiers.CreateAccount + "," + username + ',' + password;
        }
        else
        {
            message = ClientToServerSignifiers.Login + "," + username + ',' + password;
        }


        //Debug.Log(message);

        networkedClient.GetComponent<NetworkedClient>().SendMessageToHost(message);
    }

    /// <summary>
    /// Login Toggle Pressed listner
    /// </summary>
    public void LoginToggleChanged (bool newValue)
    {
        // Login toggle pressed, switch off create toggle
        Debug.Log("Login toggle pressed.");
        create_Toggle.GetComponent<Toggle>().SetIsOnWithoutNotify(!newValue);
    }

    /// <summary>
    /// Create Toggle pressed listener
    /// </summary>
    public void CreateToggleChanged(bool newValue)
    {
        // Create toggle pressed, switch off login toggle
        Debug.Log("Create toggle pressed.");
        login_Toggle.GetComponent<Toggle>().SetIsOnWithoutNotify(!newValue);
    }
}
