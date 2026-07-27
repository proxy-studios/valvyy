public interface IValvyPlugin
{
    string PluginName { get; }

    void OnPluginInitialize();

    void OnPlayerJoined(int clientId);

    void OnPlayerLeft(int clientId);

    void OnRoomCreated(string roomId);

    void OnNetworkUpdate();

    void OnCustomPacketReceived(string packetType, string data, int senderId);
}