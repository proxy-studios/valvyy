using System;
using System.Collections.Generic;
using UnityEngine;

public class ValvyPluginManager : MonoBehaviour
{
    public static ValvyPluginManager Instance { get; private set; }

    private static readonly List<IValvyPlugin> activePlugins = new List<IValvyPlugin>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary> Registers an IValvyPlugin so it receives network events. </summary>
    public static void RegisterPlugin(IValvyPlugin plugin)
    {
        if (plugin != null && !activePlugins.Contains(plugin))
        {
            activePlugins.Add(plugin);
            plugin.OnPluginInitialize();
            Console.WriteLine($"<color=#1E1E1E><b>[Valvy Plugin System]</b></color> Registered: {plugin.PluginName}");
        }
    }

    /// <summary> Unregisters a plugin. </summary>
    public static void UnregisterPlugin(IValvyPlugin plugin)
    {
        if (plugin != null && activePlugins.Contains(plugin))
        {
            activePlugins.Remove(plugin);
            Console.WriteLine($"<color=#1E1E1E><b>[Valvy Plugin System]</b></color> Unregistered: {plugin.PluginName}");
        }
    }

    private void Update()
    {
        for (int i = activePlugins.Count - 1; i >= 0; i--)
        {
            activePlugins[i]?.OnNetworkUpdate();
        }
    }

    /// <summary> Route network packets to registered interface plugins. </summary>
    public static void BroadcastPacket(string packet)
    {
        if (string.IsNullOrEmpty(packet)) return;

        string[] parts = packet.Split('|');
        string command = parts[0];

        for (int i = activePlugins.Count - 1; i >= 0; i--)
        {
            var plugin = activePlugins[i];
            if (plugin == null) continue;

            if (command == "SPAWN" && parts.Length >= 2 && int.TryParse(parts[1], out int joinId))
            {
                plugin.OnPlayerJoined(joinId);
            }
            else if (command == "DESPAWN" && parts.Length >= 2 && int.TryParse(parts[1], out int leaveId))
            {
                plugin.OnPlayerLeft(leaveId);
            }
            else if (command == "ROOM_CREATED" && parts.Length >= 2)
            {
                plugin.OnRoomCreated(parts[1]);
            }
            else if (command == "CUSTOM" && parts.Length >= 4 && int.TryParse(parts[3], out int senderId))
            {
                plugin.OnCustomPacketReceived(parts[1], parts[2], senderId);
            }
        }
    }
}