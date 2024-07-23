using UnityEngine;
using WebSocketSharp;

public class WebSocketClientBehavior : MonoBehaviour
{
#if !UNITY_SERVER || UNITY_EDITOR

    private WebSocket ws;

    private void Start()
    {
        RunWebSocketClient();
    }

    private void RunWebSocketClient()
    {
        // Initialize WebSocket
        ws = new WebSocket("ws://localhost:8080/Echo");

        // Set up message received handler
        ws.OnMessage += (sender, e) =>
            Debug.Log("Received from server: " + e.Data);

        // Connect to WebSocket server
        ws.Connect();
    }

    private void Update()
    {
        // Send a message when the space bar is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ws.Send("Space bar pressed!");
            Debug.Log("Message sent to server");
        }
    }

    private void OnDestroy()
    {
        // Clean up WebSocket connection
        if (ws != null)
        {
            ws.Close();
            ws = null;
        }
    }
#endif
}
