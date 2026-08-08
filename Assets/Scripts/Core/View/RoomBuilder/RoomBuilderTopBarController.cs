using System;
using UnityEngine;
using UnityEngine.UI;

namespace RoomGen.UI
{
    public abstract class TopBarWindowPanel : MonoBehaviour
    {
        protected RoomBuilderController Controller { get; private set; }

        public void Initialize(RoomBuilderController controller)
        {
            Controller = controller;
        }
    }

    [Serializable]
    public struct TopBarWindow
    {
        public Button button;
        public GameObject panel;
    }

    public class RoomBuilderTopBarController : MonoBehaviour
    {
        [SerializeField] private RoomBuilderController controller;
        [SerializeField] private RoomBuilderUIController sideUI;
        [Tooltip("Empty parent object holding a CanvasGroup that all window panels live under. Windows are still individually SetActive(false) when closed - this group is not used to hide them.")]
        [SerializeField] private CanvasGroup windowsGroup;
        [SerializeField] private TopBarWindow[] windows;

        [SerializeField] private Color activeColor = new Color(0.25f, 0.65f, 1f);
        [SerializeField] private Color inactiveColor = Color.white;

        private int _openIndex = -1;

        private void Awake()
        {
            if (windowsGroup != null)
            {
                windowsGroup.interactable = true;
                windowsGroup.blocksRaycasts = true;
                windowsGroup.alpha = 1f;
            }

            for (int i = 0; i < windows.Length; i++)
            {
                int index = i;
                windows[i].button.onClick.AddListener(() => Toggle(index));

                var panel = windows[i].panel.GetComponent<TopBarWindowPanel>();
                if (panel != null) panel.Initialize(controller);

                windows[i].panel.SetActive(false);
            }
        }

        private void Toggle(int index)
        {
            bool wasOpen = _openIndex == index;
            CloseAll();
            if (wasOpen) return;

            windows[index].panel.SetActive(true);
            SetButtonColor(windows[index].button, true);
            _openIndex = index;
            sideUI.CloseSidePanel();
        }

        public void CloseAll()
        {
            for (int i = 0; i < windows.Length; i++)
            {
                windows[i].panel.SetActive(false);
                SetButtonColor(windows[i].button, false);
            }
            _openIndex = -1;
        }

        private void SetButtonColor(Button button, bool active)
        {
            if (button.targetGraphic is Image image) image.color = active ? activeColor : inactiveColor;
        }
    }
}
