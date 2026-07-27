using System.Collections.Generic;
using UnityEngine;

public enum ValvyUIMode
{
    None,       // Auto-connects, no UI rendered
    BuiltInGUI, // Uses the default OnGUI panel
    CustomCanvas // Uses your custom Unity Canvas setup via ValvyUIManager
}

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Server Settings")]
    public int port = 7777;
    public string serverIP = "127.0.0.1";
    public int maxPlayers = 8;
    public string currentRoomId = "DEFAULT";

    [Header("UI System")]
    public ValvyUIMode uiMode = ValvyUIMode.BuiltInGUI;

    [Header("Status")]
    public bool isServer;
    public bool isClient;
    public int localClientId = -1;

    public ValvyServer Server { get; private set; }
    public ValvyClient Client { get; private set; }
    public Sync PlayerSync { get; private set; }

    private readonly List<IValvyPlugin> registeredPlugins = new List<IValvyPlugin>();
    private string roomIdInput = "ROOM123";

    private void Update()
    {
        // Press Escape to disconnect instantly
        if (Input.GetKeyDown(KeyCode.Escape) && (isClient || isServer))
        {
            Disconnect();
            Debug.Log("[Valvy] Disconnected via hotkey.");
        }

        foreach (var p in registeredPlugins) p.OnNetworkUpdate();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Server = gameObject.AddComponent<ValvyServer>();
        Client = gameObject.AddComponent<ValvyClient>();
        PlayerSync = gameObject.AddComponent<Sync>();
    }

    private void Start()
    {
        RegisterPluginsFromScene();

        if (uiMode == ValvyUIMode.None)
        {
            AutoConnect();
        }
    }

    #region Plugin System

    public void RegisterPlugin(IValvyPlugin plugin)
    {
        if (!registeredPlugins.Contains(plugin))
        {
            registeredPlugins.Add(plugin);
            plugin.OnPluginInitialize();
            Debug.Log($"[Valvy] Registered Plugin: {plugin.PluginName}");
        }
    }

    private void RegisterPluginsFromScene()
    {
        MonoBehaviour[] scripts = FindObjectsOfType<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script is IValvyPlugin plugin)
            {
                RegisterPlugin(plugin);
            }
        }
    }

    public void TriggerOnPlayerJoined(int id)
    {
        foreach (var p in registeredPlugins) p.OnPlayerJoined(id);
    }

    public void TriggerOnPlayerLeft(int id)
    {
        foreach (var p in registeredPlugins) p.OnPlayerLeft(id);
    }

    public void TriggerOnRoomCreated(string room)
    {
        foreach (var p in registeredPlugins) p.OnRoomCreated(room);
    }

    public void TriggerCustomPacket(string type, string data, int sender)
    {
        foreach (var p in registeredPlugins) p.OnCustomPacketReceived(type, data, sender);
    }

   

    #endregion

    #region Connection Flow

    public void AutoConnect()
    {
        if (!Client.ConnectToServer(serverIP, port))
        {
            Debug.Log("[Valvy] Starting Host...");
            StartHost(currentRoomId);
        }
    }

    public void StartHost(string roomId)
    {
        currentRoomId = roomId;
        Server.StartServer(port, maxPlayers);
        Client.ConnectToServer(serverIP, port);
        TriggerOnRoomCreated(currentRoomId);
    }

    public void JoinRoom(string roomId)
    {
        currentRoomId = roomId;
        Client.ConnectToServer(serverIP, port);
    }

    public void Disconnect()
    {
        Server.StopServer();
        Client.DisconnectClient();
        PlayerSync.ClearPlayers();

        isServer = false;
        isClient = false;
        localClientId = -1;
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    #endregion

    #region Built-In GUI Renderer

    private void OnGUI()
    {
        if (uiMode != ValvyUIMode.BuiltInGUI) return;

        Texture2D bgTex = MakeTex(2, 2, new Color(0.118f, 0.118f, 0.118f, 0.95f));
        Texture2D btnTex = MakeTex(2, 2, new Color(0.18f, 0.18f, 0.18f, 1f));
        Texture2D btnHoverTex = MakeTex(2, 2, new Color(0.25f, 0.25f, 0.25f, 1f));

        GUIStyle panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = bgTex;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.normal.background = btnTex;
        buttonStyle.hover.background = btnHoverTex;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.fontStyle = FontStyle.Bold;

        GUIStyle textFieldStyle = new GUIStyle(GUI.skin.textField);
        textFieldStyle.normal.background = btnTex;
        textFieldStyle.normal.textColor = Color.cyan;
        textFieldStyle.alignment = TextAnchor.MiddleCenter;
        textFieldStyle.fontSize = 14;

        GUI.Box(new Rect(20, 20, 260, 230), "VALVY NETWORK", panelStyle);

        if (!isClient && !isServer)
        {
            GUI.Label(new Rect(35, 50, 230, 20), "ROOM CODE / ID:");
            roomIdInput = GUI.TextField(new Rect(35, 75, 230, 30), roomIdInput, textFieldStyle);

            if (GUI.Button(new Rect(35, 115, 230, 35), "HOST ROOM", buttonStyle))
            {
                StartHost(roomIdInput);
            }

            if (GUI.Button(new Rect(35, 160, 230, 35), "JOIN ROOM", buttonStyle))
            {
                JoinRoom(roomIdInput);
            }
        }
        else
        {
            GUI.Label(new Rect(35, 55, 230, 20), $"STATUS: {(isServer ? "HOSTING" : "CONNECTED")}");
            GUI.Label(new Rect(35, 80, 230, 20), $"ROOM: {currentRoomId}");
            GUI.Label(new Rect(35, 105, 230, 20), $"ID: {localClientId}");

            if (GUI.Button(new Rect(35, 150, 230, 35), "DISCONNECT", buttonStyle))
            {
                Disconnect();
            }
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    #endregion
}