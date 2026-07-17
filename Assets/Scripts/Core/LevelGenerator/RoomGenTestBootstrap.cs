using System.Collections.Generic;
using UnityEngine;

namespace RoomGen
{
    [RequireComponent(typeof(RoomGenerator))]
    public class RoomGenTestBootstrap : MonoBehaviour
    {
        private void Start()
        {
            var generator = GetComponent<RoomGenerator>();
            generator.SetTemplates(RoomLibraryLoader.LoadAll());
            generator.Generate();
        }
    }
}
