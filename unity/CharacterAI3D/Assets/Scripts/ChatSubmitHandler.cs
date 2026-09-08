using UnityEngine;
using TMPro;

public class ChatSubmitHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private ChatWebSocketClient chatClient;
    [SerializeField] private EmotionWebSocketClient emotionClient;

    private void Start()
    {
        messageInput.onSubmit.AddListener(OnSubmit);
    }

    // Вызывается по Enter (через onSubmit у TMP_InputField)
    private void OnSubmit(string text)
    {
        Send(text);
    }

    // Вызывается по кнопке (назначь в инспекторе на Button.OnClick)
    public void OnSendButtonClicked()
    {
        Send(messageInput.text);
    }

    private void Send(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        chatClient.SendMessageToChat(text);

        if (emotionClient != null)
        {
            emotionClient.SendText(text);
        }

        messageInput.text = "";
    }
}