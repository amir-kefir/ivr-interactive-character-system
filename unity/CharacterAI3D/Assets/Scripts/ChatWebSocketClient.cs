using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

public class ChatWebSocketClient : MonoBehaviour
{
    public TMP_Text chatText;
    public float secondsPerChar = 0.04f;

    private ClientWebSocket ws;
    private readonly Queue<char> pendingChars = new Queue<char>();
    private bool isReceivingMessage = false;

    async void Start()
    {
        ws = new ClientWebSocket();
        await ws.ConnectAsync(new System.Uri("ws://127.0.0.1:8000/ws/chat"), CancellationToken.None);
        Debug.Log("Chat WebSocket connected!");
        _ = ReceiveLoop();
        StartCoroutine(TypewriterCoroutine());
    }

    public async void SendMessageToChat(string text)
    {
        chatText.text = "";
        pendingChars.Clear();
        isReceivingMessage = true;

        var bytes = Encoding.UTF8.GetBytes(text);
        await ws.SendAsync(new System.ArraySegment<byte>(bytes),
            WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task ReceiveLoop()
    {
        var buffer = new byte[4096];

        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new System.ArraySegment<byte>(buffer), CancellationToken.None);
            string json = Encoding.UTF8.GetString(buffer, 0, result.Count);

            var parsed = JsonUtility.FromJson<ChatChunk>(json);

            if (parsed.chunk != null)
            {
                foreach (char c in parsed.chunk)
                    pendingChars.Enqueue(c);
            }

            if (parsed.done)
            {
                isReceivingMessage = false;
            }
        }
    }

    private IEnumerator TypewriterCoroutine()
    {
        while (true)
        {
            if (pendingChars.Count > 0)
            {
                chatText.text += pendingChars.Dequeue();
                yield return new WaitForSeconds(secondsPerChar);
            }
            else
            {
                yield return null;
            }
        }
    }

    [System.Serializable]
    private class ChatChunk
    {
        public string chunk;
        public bool done;
    }
}