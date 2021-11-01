using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginSystem : MonoBehaviour
{
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

    [SerializeField]
    GameObject networkedClient;

    // Start is called before the first frame update
    void Start()
    {
        login_Toggle.GetComponent<Toggle>().onValueChanged.AddListener(LoginToggleChanged);
        create_Toggle.GetComponent<Toggle>().onValueChanged.AddListener(CreateToggleChanged);

        // Login by default
        login_Toggle.GetComponent<Toggle>().isOn = true;
    }

    /// <summary>
    /// Submit button pressed listener
    /// </summary>
    public void OnSubmitPressed()
    {
        // Submit button pressed
        string username = username_InputField.GetComponent<InputField>().text;
        string password = password_InputField.GetComponent<InputField>().text;

        string message;

        // use signifiers for giving the first character of data sent
        // to distinguish between create and login for server
        if (create_Toggle.GetComponent<Toggle>().isOn)
        {
            message = ClientToServerSignifiers.CreateAccount + "," + username + ',' + password;
        }
        else
        {
            message = ClientToServerSignifiers.Login + "," + username + ',' + password;
        }

        // Send message to host
        networkedClient.GetComponent<NetworkedClient>().SendMessageToHost(message);
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
}
