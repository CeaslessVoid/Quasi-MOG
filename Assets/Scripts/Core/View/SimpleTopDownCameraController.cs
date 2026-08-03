using UnityEngine;

namespace RoomGen
{
    public class SimpleTopDownCameraController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float panSpeed = 10f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 2f;
        [SerializeField] private float maxZoom = 60f;

        [Tooltip("Screen-space pixels reserved on the left for an IMGUI tool panel, if any - scrolling over that region zooms the panel's own scroll view instead of the world. Leave at 0 if there's no panel.")]
        [SerializeField] private float reservedPanelWidth = 0f;

        public void Configure(Camera cam, float reservedPanelWidthOverride = -1f)
        {
            targetCamera = cam;
            if (reservedPanelWidthOverride >= 0f) reservedPanelWidth = reservedPanelWidthOverride;
        }

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void Update()
        {
            if (targetCamera == null || IsTypingInField) return;

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

        private static bool IsTypingInField => GUIUtility.keyboardControl != 0;
    }
}