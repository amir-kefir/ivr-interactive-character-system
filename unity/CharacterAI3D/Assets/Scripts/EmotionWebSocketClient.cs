using System;
using System.Collections;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;
using TMPro;

public class EmotionWebSocketClient : MonoBehaviour
{
    [Header("Emotion Updates")]
    [SerializeField]
    private int wordsPerSend = 5;

    [SerializeField]
    private float debounceTime = 0.5f;

    [SerializeField]
    private int minCharsToSend = 3;

    [SerializeField]
    private EmotionController emotionController;

    private ClientWebSocket webSocket;

    private Coroutine debounceCoroutine;

    private int lastWordCount = 0;
    private int lastSentWordCount = 0;


    private async void Start()
    {
        await Connect();
    }


    private async System.Threading.Tasks.Task Connect()
    {
        webSocket = new ClientWebSocket();

        Uri serverUri =
            new Uri("ws://127.0.0.1:8000/ws/emotion");

        try
        {
            Debug.Log(
                "Connecting to Emotion WebSocket..."
            );

            await webSocket.ConnectAsync(
                serverUri,
                CancellationToken.None
            );

            Debug.Log(
                "Emotion WebSocket connected!"
            );

            StartCoroutine(
                ReceiveMessages()
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "WebSocket connection error: " +
                e.Message
            );
        }
    }


    public async void SendText(string text)
    {
        if (webSocket == null ||
            webSocket.State != WebSocketState.Open)
        {
            Debug.LogWarning(
                "WebSocket is not connected."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < minCharsToSend)
        {
            return;
        }

        byte[] bytes =
            Encoding.UTF8.GetBytes(text);

        await webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );

        Debug.Log(
            "Sent to Emotion Agent: " +
            text
        );
    }


    private IEnumerator ReceiveMessages()
    {
        while (
            webSocket != null &&
            webSocket.State == WebSocketState.Open
        )
        {
            var receiveTask =
                ReceiveMessageAsync();

            while (!receiveTask.IsCompleted)
                yield return null;

            if (receiveTask.Exception != null)
            {
                Debug.LogError(
                    "WebSocket receive error: " +
                    receiveTask.Exception
                );

                yield break;
            }

            string message =
                receiveTask.Result;

            if (!string.IsNullOrEmpty(message))
            {
                Debug.Log(
                    "Received from server: " + message
                );

                if (emotionController != null)
                {
                    emotionController.ApplyEmotion(message);
                }
            }

            yield return null;
        }
    }


    private async System.Threading.Tasks.Task<string>
        ReceiveMessageAsync()
    {
        byte[] buffer =
            new byte[4096];

        var result =
            await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None
            );

        if (
            result.MessageType ==
            WebSocketMessageType.Close
        )
        {
            return "";
        }

        return Encoding.UTF8.GetString(
            buffer,
            0,
            result.Count
        );
    }


    // =========================================================
    // TEXT INPUT
    // =========================================================

    public void OnTextChanged(string text)
    {
        int currentWordCount =
            CountWords(text);


        // -----------------------------------------------------
        // 1. Проверяем количество слов
        // -----------------------------------------------------

        if (
            currentWordCount > 0 &&
            currentWordCount - lastSentWordCount
                >= wordsPerSend
        )
        {
            SendText(text);

            lastSentWordCount =
                currentWordCount;

            Debug.Log(
                $"Emotion update: " +
                $"{currentWordCount} words"
            );
        }


        // -----------------------------------------------------
        // 2. Обычный debounce 500 ms
        // -----------------------------------------------------

        if (debounceCoroutine != null)
        {
            StopCoroutine(
                debounceCoroutine
            );
        }

        debounceCoroutine =
            StartCoroutine(
                DebounceSend(text)
            );


        lastWordCount =
            currentWordCount;
    }


    // =========================================================
    // DEBOUNCE
    // =========================================================

    private IEnumerator DebounceSend(string text)
    {
        yield return new WaitForSeconds(
            debounceTime
        );


        if (
            !string.IsNullOrWhiteSpace(text)
        )
        {
            // Если этот текст ещё не отправлялся
            int currentWordCount =
                CountWords(text);

            if (
                currentWordCount >
                lastSentWordCount
            )
            {
                SendText(text);

                lastSentWordCount =
                    currentWordCount;

                Debug.Log(
                    "Emotion update by debounce"
                );
            }
        }


        debounceCoroutine = null;
    }


    // =========================================================
    // WORD COUNT
    // =========================================================

    private int CountWords(string text)
    {
        if (
            string.IsNullOrWhiteSpace(text)
        )
        {
            return 0;
        }

        string[] words =
            text.Trim().Split(
                new char[]
                {
                    ' ',
                    '\t',
                    '\n'
                },
                StringSplitOptions.RemoveEmptyEntries
            );

        return words.Length;
    }


    // =========================================================
    // CLEANUP
    // =========================================================

    private async void OnDestroy()
    {
        if (webSocket != null)
        {
            try
            {
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    CancellationToken.None
                );
            }
            catch
            {
                // Соединение уже могло быть закрыто
            }

            webSocket.Dispose();
        }
    }
}