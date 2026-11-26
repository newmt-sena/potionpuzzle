using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    public enum StoryMode
    {
        Opening,
        Ending
    }

    public enum DialogueMode
    {
        Intro,
        Interlude
    }

    [Header("シーン名設定")]
    [SerializeField] private string titleSceneName = "titlescene";
    [SerializeField] private string storySceneName = "storyscene";
    [SerializeField] private string dialogueSceneName = "TalkScene"; // 実際の会話シーン名に合わせる
    [SerializeField] private string mixingSceneName = "MixingScene";     // 実際の調合シーン名に合わせる
    [SerializeField] private string bonusSceneName = "BonusScene";

    [Header("幕間・エンディング条件")]
    [SerializeField] private int interludeRequiredCount = 1; // 幕間に入るために必要な埋まり数

    //[Header("現在の進行状態")]
    public StoryMode CurrentStoryMode { get; private set; } = StoryMode.Opening;
    public DialogueMode CurrentDialogueMode { get; private set; } = DialogueMode.Intro;


    public bool HasReachedMainLoop { get; private set; } = false;
    public bool HasSeenTutorial { get; set; } = false;
    public bool HasSeenInterlude { get; set; } = false;

    private void Awake()
    {
        SoundManager.Instance.StopBGM();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ReturnToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }

    public void OpenBonusScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(bonusSceneName);
    }

    /// <summary>
    /// ニューゲーム開始（タイトル画面から呼ぶ）
    /// </summary>
    public void StartNewGame()
    {
        RecipeDataStore.ResetAll();

        HasSeenTutorial = false;
        HasSeenInterlude = false;
        HasReachedMainLoop = false;
        CurrentStoryMode = StoryMode.Opening;
        Time.timeScale = 1f;

        SceneManager.LoadScene(storySceneName);
    }

    public bool CanContinueGame()
    {
        return RecipeDataStore.HasSavedProgress() || HasReachedMainLoop;
    }

    public void ContinueGame()
    {
        Debug.Log($"ContinueGame開始 / HasSavedProgress={RecipeDataStore.HasSavedProgress()} / HasSeenInterlude={HasSeenInterlude} / confirmed={GetSavedConfirmedCount()} / mixingSceneName={mixingSceneName}");

        if (!CanContinueGame())
        {
            StartNewGame();
            return;
        }

        Time.timeScale = 1f;

        // もし規定数に到達していて、まだ幕間を見ていない状態なら幕間会話へ
        int confirmedCount = GetSavedConfirmedCount();
        if (!HasSeenInterlude && confirmedCount >= interludeRequiredCount)
        {
            StartInterludeDialogue();
            return;
        }

        // それ以外は調合シーンへ復帰（手帳やログは RecipeDataStore から自動復元される）
        SceneManager.LoadScene(mixingSceneName);
    }

    private int GetSavedConfirmedCount()
    {
        int count = 0;
        foreach (var pair in RecipeDataStore.SavedPages)
        {
            if (pair.Value != null && pair.Value.isConfirmed)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// storyscene（紙芝居）が終わったときに呼ぶ
    /// </summary>
    public void OnStoryFinished()
    {
        Time.timeScale = 1f;

        if (CurrentStoryMode == StoryMode.Opening)
        {
            CurrentDialogueMode = DialogueMode.Intro;
            SceneManager.LoadScene(dialogueSceneName);
        }
        else if (CurrentStoryMode == StoryMode.Ending)
        {
            SceneManager.LoadScene(titleSceneName);
        }
    }

    /// <summary>
    /// 会話シーンが終わったときに呼ぶ
    /// </summary>
    public void OnDialogueFinished()
    {
        Time.timeScale = 1f;

        if (CurrentDialogueMode == DialogueMode.Intro)
        {
            HasReachedMainLoop = true; // 導入完了・調合パートへ突入
            SceneManager.LoadScene(mixingSceneName);
        }
        else if (CurrentDialogueMode == DialogueMode.Interlude)
        {
            SceneManager.LoadScene(mixingSceneName);
        }
    }

    /// <summary>
    /// 現在の埋まり数を渡して、幕間へ進むべきか判定する
    /// </summary>
    public bool TryStartInterludeByCount(int unlockedCount)
    {
        if (HasSeenInterlude) return false;
        if (unlockedCount < interludeRequiredCount) return false;

        StartInterludeDialogue();
        return true;
    }

    /// <summary>
    /// 幕間会話を開始する（調合／手帳側で一定数到達時に呼ぶ）
    /// </summary>
    public void StartInterludeDialogue()
    {
        if (HasSeenInterlude) return;

        HasSeenInterlude = true;
        CurrentDialogueMode = DialogueMode.Interlude;
        Time.timeScale = 1f;
        SceneManager.LoadScene(dialogueSceneName);
    }

    /// <summary>
    /// エンディング紙芝居を開始する（手帳コンプリート時に呼ぶ）
    /// </summary>
    public void StartEndingStory()
    {
        CurrentStoryMode = StoryMode.Ending;
        Time.timeScale = 1f;
        SceneManager.LoadScene(storySceneName);
    }

    /// <summary>
    /// ゲームを終了する
    /// </summary>
    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}