using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    public GameObject loginPanel;
    public GameObject registerPanel;

    public TMP_Text statusText;

    private string loginUrl = "http://127.0.0.1:8000/auth/login";

    public void Login()
    {
        StartCoroutine(LoginRequest());
    }

    IEnumerator LoginRequest()
    {
        statusText.text = Texts.LoginLoading;

        string json =
            "{\"email\":\"" + emailInput.text +
            "\",\"password\":\"" + passwordInput.text + "\"}";

        byte[] body = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request =
            new UnityWebRequest(loginUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            statusText.text = Texts.LoginSuccess;

            Debug.Log(request.downloadHandler.text);

            SceneManager.LoadScene("MainScene");
        }
        else
        {
            statusText.text = GetErrorMessage(request);

            Debug.LogError(request.downloadHandler.text);
        }
    }

    string GetErrorMessage(UnityWebRequest request)
    {
        string response = request.downloadHandler.text;

        if (response.Contains("INVALID_LOGIN"))
        {
            return Texts.InvalidLogin;
        }

        if (response.Contains("INVALID_EMAIL"))
        {
            return Texts.InvalidEmail;
        }

        return Texts.LoginError;
    }

    public void OpenRegister()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);

        statusText.text = "";
    }
}