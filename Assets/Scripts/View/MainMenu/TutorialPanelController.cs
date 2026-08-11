using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.MainMenu
{
    public class TutorialPanelController : MonoBehaviour
    {
        [SerializeField] private TMP_Text placeholderText;
        [SerializeField] private Button backButton;
        [SerializeField] private MainMenuController mainMenu;

        private void Awake()
        {
            if (placeholderText != null)
                placeholderText.text = "Tutorial coming soon.";
            backButton.onClick.AddListener(() => mainMenu.ShowRoot());
        }
    }
}
