using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class ChatClient : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField messageInput;
    public TMP_Text responseText;

    [Header("Server")]
    public string serverUrl = "http://127.0.0.1:8000/chat";


    public void SendMessage()
    {

        string message = messageInput.text;

        if (string.IsNullOrWhiteSpace(message))
            return;

        responseText.text = "AI думает...";

        StartCoroutine(SendRequest(message));
    }


    IEnumerator SendRequest(string message)
    {
        string json = JsonUtility.ToJson(new ChatRequest(message));

        byte[] body = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();


        if (request.result == UnityWebRequest.Result.Success)
        {
            ChatResponse response =
                JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);

            responseText.text = response.response;
        }
        else
        {
            responseText.text = "Ошибка: " + request.error;
            Debug.LogError(request.error);
        }
    }


    [System.Serializable]
    public class ChatRequest
    {
        public string message;

        public ChatRequest(string message)
        {
            this.message = message;
        }
    }


    [System.Serializable]
    public class ChatResponse
    {
        public string response;
    }
}