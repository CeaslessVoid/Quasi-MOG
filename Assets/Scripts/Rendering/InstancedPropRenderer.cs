using System.Collections.Generic;
using UnityEngine;

namespace BattleAngel.Rendering
{
    public class InstancedPropRenderer : MonoBehaviour
    {
        private const int BatchLimit = 1023;

        [SerializeField] private Mesh quadMesh;
        [SerializeField] private Material instancedMaterial;
        [SerializeField] private Bounds cullingBounds = new(Vector3.zero, new Vector3(100000f, 100000f, 10f));

        private struct PropInstance
        {
            public Vector3 position;
            public Quaternion rotation;
            public int spriteIndex;
        }

        private readonly Dictionary<int, PropInstance> instances = new();
        private int nextHandle = 1;

        private readonly Dictionary<int, List<Matrix4x4>> groupedMatrices = new();
        private bool dirty = true;

        private static MaterialPropertyBlock Mpb;
        private static int SpriteIndexId;

        private void Awake()
        {
            if (Mpb == null)
                Mpb = new MaterialPropertyBlock();

            SpriteIndexId = Shader.PropertyToID("_SpriteIndex");
        }

        public int AddInstance(Vector3 worldPos, float rotationDegrees, int spriteIndex)
        {
            int handle = nextHandle++;
            instances[handle] = new PropInstance
            {
                position = worldPos,
                rotation = Quaternion.Euler(0f, 0f, rotationDegrees),
                spriteIndex = spriteIndex
            };
            dirty = true;
            return handle;
        }

        public void RemoveInstance(int handle)
        {
            if (instances.Remove(handle))
            {
                dirty = true;
            }
        }

        public void MoveInstance(int handle, Vector3 newWorldPos)
        {
            if (!instances.TryGetValue(handle, out var inst)) return;
            inst.position = newWorldPos;
            instances[handle] = inst;
            dirty = true;
        }

        /// Use for things like a door swapping from "closed" to "open" sprite.
        public void SetInstanceSprite(int handle, int spriteIndex)
        {
            if (!instances.TryGetValue(handle, out var inst)) return;
            inst.spriteIndex = spriteIndex;
            instances[handle] = inst;
            dirty = true;
        }

        private void RebuildGroups()
        {
            foreach (var list in groupedMatrices.Values) list.Clear();

            foreach (var inst in instances.Values)
            {
                if (!groupedMatrices.TryGetValue(inst.spriteIndex, out var list))
                {
                    list = new List<Matrix4x4>();
                    groupedMatrices[inst.spriteIndex] = list;
                }
                list.Add(Matrix4x4.TRS(inst.position, inst.rotation, Vector3.one));
            }

            dirty = false;
        }

        private void LateUpdate()
        {
            if (instances.Count == 0) return;
            if (dirty) RebuildGroups();

            foreach (var kvp in groupedMatrices)
            {
                var matrices = kvp.Value;
                if (matrices.Count == 0) continue;

                Mpb.SetFloat(SpriteIndexId, kvp.Key);

                var renderParams = new RenderParams(instancedMaterial)
                {
                    worldBounds = cullingBounds,
                    matProps = Mpb
                };

                for (int i = 0; i < matrices.Count; i += BatchLimit)
                {
                    int count = Mathf.Min(BatchLimit, matrices.Count - i);
                    Graphics.RenderMeshInstanced(renderParams, quadMesh, 0,
                        matrices.GetRange(i, count).ToArray());
                }
            }
        }
    }
}
