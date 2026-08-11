using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using RoomGen;

namespace Networking
{
    [Serializable]
    public struct RoomInfo
    {
        public string roomName;
        public string hostAddress;
        public int hostPort;
        public int playerCount;
        public int maxPlayers;
    }

    public class LanRoomDiscovery : MonoBehaviour
    {
        private const int BroadcastPort = 47657;
        private const float AdvertiseInterval = 1f;
        private const float RoomTimeout = 3f;

        private UdpClient _listenClient;
        private UdpClient _advertiseClient;
        private float _advertiseTimer;
        private bool _isAdvertising;

        private string _advertisedName;
        private int _advertisedPort;
        private Func<int> _playerCountProvider;
        private int _maxPlayers;

        private readonly Dictionary<string, (RoomInfo info, float lastSeen)> _rooms = new Dictionary<string, (RoomInfo, float)>();

        public IReadOnlyCollection<RoomInfo> DiscoveredRooms => GetSnapshot();

        public void StartBrowsing()
        {
            StopListening();
            _listenClient = new UdpClient();
            _listenClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listenClient.Client.Bind(new IPEndPoint(IPAddress.Any, BroadcastPort));
            _listenClient.BeginReceive(OnReceive, null);
        }

        public void StartAdvertising(string roomName, int hostPort, int maxPlayers, Func<int> playerCountProvider)
        {
            _advertisedName = roomName;
            _advertisedPort = hostPort;
            _maxPlayers = maxPlayers;
            _playerCountProvider = playerCountProvider;
            _isAdvertising = true;
            _advertiseTimer = 0f;

            if (_advertiseClient == null)
            {
                _advertiseClient = new UdpClient();
                _advertiseClient.EnableBroadcast = true;
            }
        }

        public void StopAdvertising() => _isAdvertising = false;

        public void StopAll()
        {
            _isAdvertising = false;
            StopListening();
            _advertiseClient?.Close();
            _advertiseClient = null;
            lock (_rooms) { _rooms.Clear(); }
        }

        private void StopListening()
        {
            _listenClient?.Close();
            _listenClient = null;
        }

        private void OnDestroy() => StopAll();

        private void Update()
        {
            PruneStaleRooms();

            if (!_isAdvertising || _advertiseClient == null) return;
            _advertiseTimer -= Time.unscaledDeltaTime;
            if (_advertiseTimer > 0f) return;
            _advertiseTimer = AdvertiseInterval;
            SendAdvertisement();
        }

        private void SendAdvertisement()
        {
            var info = new RoomInfo
            {
                roomName = _advertisedName,
                hostAddress = GetLocalIPAddress(),
                hostPort = _advertisedPort,
                playerCount = _playerCountProvider != null ? _playerCountProvider() : 0,
                maxPlayers = _maxPlayers
            };

            string payload = "ROOMGEN|" + JsonUtility.ToJson(info);
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            try
            {
                _advertiseClient.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, BroadcastPort));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LanRoomDiscovery: failed to send advertisement: {e.Message}");
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            if (_listenClient == null) return;
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            byte[] data;
            try
            {
                data = _listenClient.EndReceive(ar, ref remote);
                _listenClient.BeginReceive(OnReceive, null);
            }
            catch
            {
                return;
            }

            string text = Encoding.UTF8.GetString(data);
            if (!text.StartsWith("ROOMGEN|")) return;

            try
            {
                var info = JsonUtility.FromJson<RoomInfo>(text.Substring("ROOMGEN|".Length));
                string key = info.hostAddress + ":" + info.hostPort;
                lock (_rooms) { _rooms[key] = (info, Time.realtimeSinceStartup); }
            }
            catch
            {
            }
        }

        private void PruneStaleRooms()
        {
            List<string> stale = null;
            lock (_rooms)
            {
                foreach (var kvp in _rooms)
                {
                    if (Time.realtimeSinceStartup - kvp.Value.lastSeen > RoomTimeout)
                    {
                        stale ??= new List<string>();
                        stale.Add(kvp.Key);
                    }
                }
                if (stale != null)
                    foreach (var key in stale) _rooms.Remove(key);
            }
        }

        private List<RoomInfo> GetSnapshot()
        {
            lock (_rooms)
            {
                var list = new List<RoomInfo>(_rooms.Count);
                foreach (var kvp in _rooms) list.Add(kvp.Value.info);
                return list;
            }
        }

        private static string GetLocalIPAddress()
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            return "127.0.0.1";
        }
    }
}
