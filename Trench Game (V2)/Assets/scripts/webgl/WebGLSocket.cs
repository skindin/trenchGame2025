using UnityEngine;
using System;
using System.Runtime.InteropServices;
using Unity.VisualScripting;

public static class WebGLSocket
{
#if UNITY_WEBGL && !UNITY_EDITOR// || true
    [DllImport("__Internal")]
    public static extern void Connect(string url);

    [DllImport("__Internal")]
    public static extern void Send(byte[] data);

    [DllImport("__Internal")]
    public static extern void Close();
#else
    public static void Connect(string url)
    {
        Debug.Log("WebSocketConnect called, but not in WebGL.");
    }

    public static void Send(byte[] data)
    {
        Debug.Log("WebSocketSend called, but not in WebGL.");
    }

    public static void Close()
    {
        Debug.Log("WebSocketClose called, but not in WebGL.");
    }
#endif

    public static Action onOpen;
    public static Action<byte[]> onMessage;
    public static Action<string> onError;
    public static Action onClose;

    public static void OnOpen()
    {
        onOpen.Invoke();
    }

    public static void OnMessage(byte[] data)
    {
        onMessage.Invoke(data);
    }

    public static void OnError (string errorMessage)
    {
        onError.Invoke(errorMessage);
    }

    public static void OnClose ()
    {
        onClose.Invoke();
    }
}