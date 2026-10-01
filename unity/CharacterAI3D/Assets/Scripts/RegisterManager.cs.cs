using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;

public class RegisterManager : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField passwordCheckInput;

    public GameObject loginPanel;
    public GameObject registerPanel;

    public TMP_Text statusText;

    private string registerUrl = "http://127.0.0.1:8000/auth/register";

    public void Register()
    {
        StartCoroutine(RegisterRequest());
    }

    IEnumerator RegisterRequest()
    {
        if (passwordInput.text != passwordCheckInput.text)
        {
            statusText.text = Texts.PasswordsDoNotMatch;
            yield break;
        }

        statusText.text = Texts.RegisterLoading;

        string json =
            "{\"username\":\"" + usernameInput.text +
            "\",\"email\":\"" + emailInput.text +
            "\",\"password\":\"" + passwordInput.text + "\"}";

        byte[] body = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request =
            new UnityWebRequest(registerUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader(
            "Content-Type",
            "application/json"
        );

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            statusText.text = Texts.RegisterSuccess;

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

        if (response.Contains("USERNAME_AND_EMAIL_TAKEN"))
        {
            return Texts.UsernameAndEmailTaken;
        }

        if (response.Contains("USERNAME_TAKEN"))
        {
            return Texts.UsernameTaken;
        }

        if (response.Contains("EMAIL_TAKEN"))
        {
            return Texts.EmailTaken;
        }

        if (response.Contains("INVALID_USERNAME"))
        {
            return Texts.InvalidUsername;
        }

        if (response.Contains("INVALID_EMAIL"))
        {
            return Texts.InvalidEmail;
        }

        if (response.Contains("PASSWORD_TOO_SHORT"))
        {
            return Texts.PasswordTooShort;
        }

        return Texts.UnknownError;
    }

    public void OpenLogin()
    {
        registerPanel.SetActive(false);
        loginPanel.SetActive(true);

        statusText.text = "";
    }
}