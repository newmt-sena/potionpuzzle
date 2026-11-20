using UnityEngine;
using UnityEngine.UI;

public class TitleSceneController : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(() =>
            {
                if (GameFlowManager.Instance != null)
                {
                    GameFlowManager.Instance.StartNewGame();
                }
            });
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            GameFlowManager.Instance.CanContinueGame();
            continueButton.onClick.AddListener(() =>
            {
                if (GameFlowManager.Instance != null)
                {
                    GameFlowManager.Instance.ContinueGame();
                }
            });
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() =>
            {
                if (GameFlowManager.Instance != null)
                {
                    GameFlowManager.Instance.QuitGame();
                }
                else
                {
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #else
                    Application.Quit();
                    #endif
                }
            });
        }
    }
}