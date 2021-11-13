using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginSystem : MonoBehaviour
{
    // UI elements
    [Header("UI Input Elements")]
    [SerializeField]
    GameObject username_InputField;
    [SerializeField]
    GameObject password_InputField;
    [SerializeField]
    GameObject submit_Button;
    [SerializeField]
    GameObject login_Toggle;
    [SerializeField]
    GameObject create_Toggle;

    [Header("UI Connection Status Elements")]
    // Loading Circle
    [SerializeField] 
    Image loadingCircleImage;
    [SerializeField] 
    Text loadingText;

    [SerializeField] 
    Text fromServerTextField;

    // Client Object
    [SerializeField]
    NetworkedClient networkedClient;

    private string username;
    private string password;

    // Connection variables
    private bool isEstablishingConnection = false;
    private bool executeOnce_establishConnection = true;

    // reference to login instance
    private static LoginSystem _instance;

    // create a return function
    public static LoginSystem GetInstance()
    {
        return _instance;
    }

    // Start is called before the first frame update
    void Start()
    {
        // self reference shortcut
        _instance = this;

        // Login toggle
        login_Toggle.GetComponent<Toggle>().onValueChanged.AddListener(LoginToggleChanged);
        create_Toggle.GetComponent<Toggle>().onValueChanged.AddListener(CreateToggleChanged);

        // Login by default
        login_Toggle.GetComponent<Toggle>().isOn = true;


        // Connect to server
        Connect();
    }


    // Login Attempt
    private void Update()
    {
        // Attempt the connection text
        if (isEstablishingConnection && executeOnce_establishConnection)
        {
            executeOnce_establishConnection = false;
            loadingCircleImage.color = new Color(1f, 0.4f, 0f, 1f);
            loadingText.text = "Connecting...";
            // Initiate the loading screen here
        }

        // Connection Established
        if (isEstablishingConnection && networkedClient.ourClientID != -1)
        {
            loadingCircleImage.color = Color.green;
            loadingText.text = "Connected";
            executeOnce_establishConnection = true;
        }

        // Connection Failed

        if (isEstablishingConnection && !networkedClient.IsConnected())
        {
            Debug.Log("Connection Attempt Failed!");
            loadingCircleImage.color = new Color(1f, 0f, 0f, 1f);
            loadingText.text = "Not Connected";
            executeOnce_establishConnection = true;
        }
    }


    /// <summary>
    /// Connect function
    /// </summary>
    public void Connect()
    {
        isEstablishingConnection = true;

        if (!networkedClient.IsConnected())
        {
            networkedClient.Connect();
        }
    }

    /// <summary>
    /// Submit button pressed listener
    /// </summary>
    public void OnSubmitPressed()
    {
        // Submit button pressed
        username = username_InputField.GetComponent<InputField>().text;
        password = password_InputField.GetComponent<InputField>().text;

        string message;

        // use signifiers for giving the first character of data sent
        // to distinguish between create and login for server

        if (networkedClient.IsConnected())
        {
            if (create_Toggle.GetComponent<Toggle>().isOn)
            {
                message = ClientToServerSignifiers.CreateAccount + "," + username + ',' + password;
            }
            else
            {
                message = ClientToServerSignifiers.Login + "," + username + ',' + password;
            }

            // Send message to host
            networkedClient.SendMessageToHost(message);
        }
        else
        {
            // If attempting to press submit without a valid connection, attempt connection
            fromServerTextField.color = Color.red;
            
            fromServerTextField.text = "Attempting connection";

            if (!networkedClient.IsConnected())
            {
                Connect();
            }
        }
    }

    /// <summary>
    /// Login Toggle Pressed listner
    /// </summary>
    public void LoginToggleChanged(bool newValue)
    {
        // Login toggle pressed, switch off create toggle
        create_Toggle.GetComponent<Toggle>().SetIsOnWithoutNotify(!newValue);
    }

    /// <summary>
    /// Create Toggle pressed listener
    /// </summary>
    public void CreateToggleChanged(bool newValue)
    {
        // Create toggle pressed, switch off login toggle
        login_Toggle.GetComponent<Toggle>().SetIsOnWithoutNotify(!newValue);
    }


    /// <summary>
    /// Handle server response
    /// </summary>
    /// <param name="msg"></param>
    public void HandleResponseFromServer(string msg)
    {
        if (int.Parse(msg) == ServerToClientSignifiers.LoginComplete)
        {
            fromServerTextField.color = Color.yellow;
            fromServerTextField.text = "Logging in!!!";

            // Login successful, so this is the username that has to be set
            GameManager.currentUsername = username;

            StartCoroutine(LoginDelay());
        }
        else if (int.Parse(msg) == ServerToClientSignifiers.LoginFailedPassword)
        {
            fromServerTextField.color = Color.red;
            fromServerTextField.text = "Incorrect Password!";
        }
        else if (int.Parse(msg) == ServerToClientSignifiers.LoginFailedUsername)
        {
            fromServerTextField.color = Color.red;
            fromServerTextField.text = "Username does not exist!";
        }
        else if (int.Parse(msg) == ServerToClientSignifiers.AccountCreationComplete)
        {
            fromServerTextField.color = Color.green;
            fromServerTextField.text = "Account successfully created!";
        }
        else if (int.Parse(msg) == ServerToClientSignifiers.AccountCreationFailed)
        {
            fromServerTextField.color = Color.red;
            fromServerTextField.text = "Account creation failed :(";
        }
    }

    /// <summary>
    /// Coroutine for UX
    /// </summary>
    /// <returns></returns>
    IEnumerator LoginDelay()
    {
        yield return new WaitForSeconds(0.5f);
        GameManager.GetInstance().ChangeMode(CurrentMode.Lobby);
    }
}
