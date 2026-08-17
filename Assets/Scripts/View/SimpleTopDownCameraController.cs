using UnityEngine;
using Util;

namespace RoomGen
{
    public class SimpleTopDownCameraController : MonoBehaviour
    {
        public static SimpleTopDownCameraController Instance { get; private set; }

        [SerializeField] private Camera targetCamera;
        [SerializeField] private float panSpeed = 10f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 2f;
        [SerializeField] private float maxZoom = 60f;

        [SerializeField] private float reservedPanelWidth = 0f;

        public void Configure(Camera cam, float reservedPanelWidthOverride = -1f)
        {
            targetCamera = cam;
            if (reservedPanelWidthOverride >= 0f) reservedPanelWidth = reservedPanelWidthOverride;
        }

        public void CenterOn(Vector3 worldPosition)
        {
            if (targetCamera == null) return;
            var pos = targetCamera.transform.position;
            targetCamera.transform.position = new Vector3(worldPosition.x, worldPosition.y, pos.z);
        }

        private void Awake()
        {
            Instance = this;
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (targetCamera == null || InputFocusUtility.IsTypingInField) return;

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move.y += 1f;
            if (Input.GetKey(KeyCode.S)) move.y -= 1f;
            if (Input.GetKey(KeyCode.A)) move.x -= 1f;
            if (Input.GetKey(KeyCode.D)) move.x += 1f;

            if (move != Vector3.zero)
            {
                float speedScale = targetCamera.orthographicSize / 10f;
                targetCamera.transform.position += move.normalized * (panSpeed * speedScale * Time.deltaTime);
            }

            bool overUI = UnityEngine.EventSystems.EventSystem.current != null &&
              UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (Input.mousePosition.x >= reservedPanelWidth && !overUI)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.0001f)
                    targetCamera.orthographicSize = Mathf.Clamp(targetCamera.orthographicSize - scroll * zoomSpeed, minZoom, maxZoom);
            }
        }
    }
}