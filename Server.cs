using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class ValvyServer : MonoBehaviour
{
    private TcpListener serverListener;
    private readonly Dictionary<int, TcpClient> connectedClients = new Dictionary<int, TcpClient>();
    private readonly Dictionary<int, Vector3> targetPositions = new Dictionary<int, Vector3>();
    private readonly Dictionary<int, Quaternion> targetRotations = new Dictionary<int, Quaternion>();

    private int nextClientId = 1;
    private int maxAllowedPlayers = 8;

    public void StartServer(int port, int maxPlayers)
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.isServer) return;
        maxAllowedPlayers = maxPlayers;

        try
        {
            if (NetworkManager.Instance != null) NetworkManager.Instance.isServer = true;

            serverListener = new TcpListener(IPAddress.Any, port);
            serverListener.Start();
            serverListener.BeginAcceptTcpClient(OnClientConnected, null);
            Debug.Log($"[Valvy] Server started on port {port} (Max Players: {maxAllowedPlayers})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Valvy] Server start error: {ex.Message}");
            if (NetworkManager.Instance != null) NetworkManager.Instance.isServer = false;
        }
    }

    public void StopServer()
    {
        if (NetworkManager.Instance != null && !NetworkManager.Instance.isServer) return;

        if (NetworkManager.Instance != null) NetworkManager.Instance.isServer = false;

        lock (connectedClients)
        {
            foreach (var kvp in connectedClients)
            {
                try
                {
                    kvp.Value?.GetStream()?.Close();
                    kvp.Value?.Close();
                }
                catch (ObjectDisposedException) { }
                catch (Exception) { }
            }
            connectedClients.Clear();
            targetPositions.Clear();
            targetRotations.Clear();
        }

        try
        {
            serverListener?.Stop();
        }
        catch (ObjectDisposedException) { }
        catch (Exception) { }
    }

    private void OnClientConnected(IAsyncResult ar)
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.isServer) return;

        TcpClient client = null;
        try
        {
            client = serverListener.EndAcceptTcpClient(ar);
        }
        catch (ObjectDisposedException)
        {
            return; // Server socket was shut down
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Valvy] Accept client error: {ex.Message}");
            return;
        }

        try
        {
            lock (connectedClients)
            {
                if (connectedClients.Count + 1 >= maxAllowedPlayers)
                {
                    SendToClient(client, "FULL|Server room is full");
                    client.Close();
                    serverListener.BeginAcceptTcpClient(OnClientConnected, null);
                    return;
                }

                int assignedId = nextClientId++;

                SendToClient(client, $"INIT|{assignedId}");

                foreach (var kvp in connectedClients)
                {
                    int existingId = kvp.Key;
                    SendToClient(client, $"SPAWN|{existingId}");
                    SendToClient(kvp.Value, $"SPAWN|{assignedId}");

                    if (targetPositions.TryGetValue(existingId, out Vector3 existingPos) &&
                        targetRotations.TryGetValue(existingId, out Quaternion existingRot))
                    {
                        string posSync = string.Format(CultureInfo.InvariantCulture, "POS|{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}",
                            existingId, existingPos.x, existingPos.y, existingPos.z, existingRot.x, existingRot.y, existingRot.z, existingRot.w);
                        SendToClient(client, posSync);
                    }
                }

                connectedClients.Add(assignedId, client);

                serverListener.BeginAcceptTcpClient(OnClientConnected, null);

                byte[] buffer = new byte[4096];
                NetworkStream stream = client.GetStream();
                stream.BeginRead(buffer, 0, buffer.Length, OnServerDataReceived, new ServerClientState { client = client, id = assignedId, buffer = buffer });
            }
        }
        catch (ObjectDisposedException)
        {
            if (client != null) client.Close();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Valvy] Connection setup error: {ex.Message}");
        }
    }

    private void OnServerDataReceived(IAsyncResult ar)
    {
        ServerClientState state = (ServerClientState)ar.AsyncState;
        if (state == null || state.client == null) return;

        try
        {
            NetworkStream stream;
            try
            {
                stream = state.client.GetStream();
            }
            catch (ObjectDisposedException)
            {
                HandleClientDisconnect(state.id);
                return;
            }

            int bytesRead = stream.EndRead(ar);
            if (bytesRead <= 0)
            {
                HandleClientDisconnect(state.id);
                return;
            }

            string rawData = Encoding.UTF8.GetString(state.buffer, 0, bytesRead);
            state.streamBuilder.Append(rawData);

            string currentData = state.streamBuilder.ToString();
            string[] packets = currentData.Split('\n');

            for (int i = 0; i < packets.Length - 1; i++)
            {
                string p = packets[i].Trim();
                if (!string.IsNullOrEmpty(p))
                {
                    UpdateServerState(p);
                    BroadcastPacket(p, state.id);
                    if (NetworkManager.Instance != null && NetworkManager.Instance.Client != null)
                    {
                        NetworkManager.Instance.Client.EnqueuePacket(p);
                    }
                }
            }

            state.streamBuilder.Clear();
            state.streamBuilder.Append(packets[packets.Length - 1]);

            // Continue reading
            stream.BeginRead(state.buffer, 0, state.buffer.Length, OnServerDataReceived, state);
        }
        catch (ObjectDisposedException)
        {
            HandleClientDisconnect(state.id);
        }
        catch (Exception)
        {
            HandleClientDisconnect(state.id);
        }
    }

    private void UpdateServerState(string packet)
    {
        string[] parts = packet.Split('|');
        if (parts.Length >= 9 && (parts[0] == "POS" || parts[0] == "VRPOS"))
        {
            if (int.TryParse(parts[1], out int id))
            {
                if (float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float px) &&
                    float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float py) &&
                    float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float pz) &&
                    float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float rx) &&
                    float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float ry) &&
                    float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out float rz) &&
                    float.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out float rw))
                {
                    targetPositions[id] = new Vector3(px, py, pz);
                    targetRotations[id] = new Quaternion(rx, ry, rz, rw);
                }
            }
        }
    }

    private void HandleClientDisconnect(int id)
    {
        lock (connectedClients)
        {
            if (connectedClients.ContainsKey(id))
            {
                try
                {
                    connectedClients[id]?.Close();
                }
                catch (ObjectDisposedException) { }
                catch (Exception) { }

                connectedClients.Remove(id);
                targetPositions.Remove(id);
                targetRotations.Remove(id);
            }
        }

        BroadcastPacket($"DESPAWN|{id}", -1);
        if (NetworkManager.Instance != null && NetworkManager.Instance.Client != null)
        {
            NetworkManager.Instance.Client.EnqueuePacket($"DESPAWN|{id}");
        }
    }

    public void BroadcastPacket(string packet, int excludeId)
    {
        byte[] data = Encoding.UTF8.GetBytes(packet + "\n");
        lock (connectedClients)
        {
            foreach (var kvp in connectedClients)
            {
                if (kvp.Key != excludeId && kvp.Value != null)
                {
                    try
                    {
                        if (kvp.Value.Connected)
                        {
                            kvp.Value.GetStream().Write(data, 0, data.Length);
                        }
                    }
                    catch (ObjectDisposedException) { }
                    catch (Exception) { }
                }
            }
        }
    }

    private void SendToClient(TcpClient client, string packet)
    {
        if (client == null) return;
        byte[] data = Encoding.UTF8.GetBytes(packet + "\n");
        try
        {
            if (client.Connected)
            {
                client.GetStream().Write(data, 0, data.Length);
            }
        }
        catch (ObjectDisposedException) { }
        catch (Exception) { }
    }

    private void OnDestroy()
    {
        StopServer();
    }

    private class ServerClientState
    {
        public TcpClient client;
        public int id;
        public byte[] buffer;
        public StringBuilder streamBuilder = new StringBuilder();
    }
}