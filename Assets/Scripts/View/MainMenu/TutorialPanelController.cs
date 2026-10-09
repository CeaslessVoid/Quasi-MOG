using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu
{
    public class TutorialPanelController : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private MainMenuController mainMenu;

        private void Awake()
        {
            backButton.onClick.AddListener(() => mainMenu.ShowRoot());
        }
    }
}
