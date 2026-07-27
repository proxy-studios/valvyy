using UnityEngine;
using valvy;

namespace valvy
{
    public class Disconnect : MonoBehaviour
    {
        public void Disconnectt()
        {
            System.Net.Sockets.TcpClient tcp = GetComponent<System.Net.Sockets.TcpClient>();
            if (tcp != null && tcp.Connected)
            {
                tcp.Close();
            }

            Debug.Log("[Valvy] Disconnected.");
        }
    }
}