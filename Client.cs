using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class ValvyClient : MonoBehaviour
{
    private TcpClient clientSocket;
    private NetworkStream clientStream;

    private readonly ConcurrentQueue<string> incomingPackets = new ConcurrentQueue<string>();
    private readonly ConcurrentQueue<Action> mainThreadQueue = new ConcurrentQueue<Action>();

    public bool ConnectToServer(string ip, int port)
    {
        try
        {
            clientSocket = new TcpClient();
            clientSocket.Connect(ip, port);
            clientStream = clientSocket.GetStream();
            NetworkManager.Instance.isClient = true;

            byte[] buffer = new byte[4096];
            clientStream.BeginRead(buffer, 0, buffer.Length, OnClientDataReceived, new ClientState { client = clientSocket, buffer = buffer });
            Debug.Log($"[Valvy] Connected to server at {ip}:{port}");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void DisconnectClient()
    {
        if (!NetworkManager.Instance.isClient) return;

        clientStream?.Close();
        clientSocket?.Close();
        NetworkManager.Instance.isClient = false;
    }

    private void Update()
    {
        while (mainThreadQueue.TryDequeue(out Action action))
        {
            action?.Invoke();
        }

        while (incomingPackets.TryDequeue(out string packet))
        {
            NetworkManager.Instance.PlayerSync.ProcessPacket(packet);
        }
    }

    public void EnqueuePacket(string packet)
    {
        incomingPackets.Enqueue(packet);
    }

    public void SendPacket(string packet)
    {
        if (!NetworkManager.Instance.isClient || clientStream == null) return;
        byte[] data = Encoding.UTF8.GetBytes(packet + "\n");
        try
        {
            clientStream.Write(data, 0, data.Length);
        }
        catch { }
    }

    private void OnClientDataReceived(IAsyncResult ar)
    {
        ClientState state = (ClientState)ar.AsyncState;
        try
        {
            int bytesRead = clientStream.EndRead(ar);
            if (bytesRead <= 0) return;

            string rawData = Encoding.UTF8.GetString(state.buffer, 0, bytesRead);
            state.streamBuilder.Append(rawData);

            string currentData = state.streamBuilder.ToString();
            string[] packets = currentData.Split('\n');

            for (int i = 0; i < packets.Length - 1; i++)
            {
                string p = packets[i].Trim();
                if (!string.IsNullOrEmpty(p))
                {
                    incomingPackets.Enqueue(p);
                }
            }

            state.streamBuilder.Clear();
            state.streamBuilder.Append(packets[packets.Length - 1]);

            clientStream.BeginRead(state.buffer, 0, state.buffer.Length, OnClientDataReceived, state);
        }
        catch { }
    }

    private class ClientState
    {
        public TcpClient client;
        public byte[] buffer;
        public StringBuilder streamBuilder = new StringBuilder();
    }
}