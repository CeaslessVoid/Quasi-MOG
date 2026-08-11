using UnityEngine;
using Networking.App;

namespace UI.MainMenu
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private GameObject playPanel;
        [SerializeField] private GameObject multiplayerPanel;

        private void Awake()
        {
            AppState.EnsureExists();
            ShowRoot();
        }

        public void ShowRoot()
        {
            SetAll(false);
            rootPanel.SetActive(true);
        }

        public void ShowTutorial()
        {
            SetAll(false);
            tutorialPanel.SetActive(true);
        }

        public void ShowPlay()
        {
            SetAll(false);
            playPanel.SetActive(true);
        }

        public void ShowMultiplayer()
        {
            SetAll(false);
            multiplayerPanel.SetActive(true);
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetAll(bool state)
        {
            rootPanel.SetActive(state);
            tutorialPanel.SetActive(state);
            playPanel.SetActive(state);
            multiplayerPanel.SetActive(state);
        }
    }
}
