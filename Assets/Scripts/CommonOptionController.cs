using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

public class CommonOptionController : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private GameObject optionPanel;     // 設定ウィンドウ本体（Panel）
    [SerializeField] private Button openButton;          // オプションを開くボタン
    [SerializeField] private Button closeButton;         // 設定ウィンドウを閉じるボタン
    [SerializeField] private Slider bgmSlider;           // BGM音量スライダー
    [SerializeField] private Slider seSlider;            // SE音量スライダー
    [SerializeField] private Button quitOrTitleButton;   // ゲーム終了 または タイトルへ戻るボタン

    [Header("遷移設定")]
    [SerializeField] private bool isReturnToTitle = true; // trueならタイトルシーンへ、falseならアプリ終了
    [SerializeField] private string titleSceneName = "TitleScene";

    private void Awake()
    {
        // ★ 起動した瞬間に確実にボタンのイベント登録を行う（Awakeを使うことで初回クリック漏れを防ぐ）
        if (openButton != null) openButton.onClick.AddListener(OpenOption);
        if (closeButton != null) closeButton.onClick.AddListener(CloseOption);

        if (quitOrTitleButton != null)
        {
            if (isReturnToTitle)
                quitOrTitleButton.onClick.AddListener(ReturnToTitle);
            else
                quitOrTitleButton.onClick.AddListener(QuitGame);
        }

        // スライダーのイベント登録
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (seSlider != null) seSlider.onValueChanged.AddListener(OnSeChanged);
    }

    void Start()
    {
        // 起動時にオプションパネルを確実に閉じておく
        if (optionPanel != null)
        {
            optionPanel.SetActive(false);
        }

        // 保存されている音量をロードしてスライダーとシステムに反映
        float savedBgm = PlayerPrefs.GetFloat("BGM_Volume", 1f);
        float savedSe = PlayerPrefs.GetFloat("SE_Volume", 1f);

        if (bgmSlider != null) bgmSlider.value = savedBgm;
        if (seSlider != null) seSlider.value = savedSe;

        ApplyVolume(savedBgm, savedSe);
    }

    public void OpenOption()
    {
        if (optionPanel != null)
        {
            optionPanel.SetActive(true);
            optionPanel.transform.SetAsLastSibling(); // 手前に表示

            if (EventSystem.current != null && closeButton != null)
            {
                EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
            }
        }

        SoundManager.Instance.PlaySE(SoundManager.SEType.UI_Click);

        Time.timeScale = 0f; // ポーズ
    }

    public void CloseOption()
    {
        if (optionPanel != null)
        {
            optionPanel.SetActive(false);
        }

        SoundManager.Instance.PlaySE(SoundManager.SEType.UI_Cancel);
        Time.timeScale = 1f; // 再開
    }

    private void OnBgmChanged(float value)
    {
        PlayerPrefs.SetFloat("BGM_Volume", value);
        PlayerPrefs.Save();
        ApplyVolume(value, seSlider != null ? seSlider.value : 1f);
    }

    private void OnSeChanged(float value)
    {
        PlayerPrefs.SetFloat("SE_Volume", value);
        PlayerPrefs.Save();
        ApplyVolume(bgmSlider != null ? bgmSlider.value : 1f, value);
    }

    private void ApplyVolume(float bgmVol, float seVol)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetBGMVolume(bgmVol);
            SoundManager.Instance.SetSEVolume(seVol);
        }
    }

    private void ReturnToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}