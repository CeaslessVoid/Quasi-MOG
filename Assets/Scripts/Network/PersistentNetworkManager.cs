using UnityEngine;
using Unity.Netcode;

namespace Networking
{
    [RequireComponent(typeof(NetworkManager))]
    public class PersistentNetworkManager : MonoBehaviour
    {
        private static bool _created;

        private void Awake()
        {
            if (_created)
            {
                Destroy(gameObject);
                return;
            }
            _created = true;
            DontDestroyOnLoad(gameObject);
        }
    }
}
