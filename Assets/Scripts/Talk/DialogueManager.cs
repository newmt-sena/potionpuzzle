using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{

    [Header("共有エモートのマスターリスト")]
    [SerializeField] private Sprite[] sharedEmoteSprites;

    [System.Serializable]
    public class DialogueEvent
    {
        public string[] sentences;  // セリフ
        public Sprite[] characterSprites; // 変更するキャラ画像
        public bool[] isLCharacter; // 主人公キャラかどうか (true:左, false:右)

        [Header("このセリフで表示するアイテム画像（ない場合はNone）")]
        public Sprite[] popupItemSprites;

        [Header("★ エモート設定（共通リストの番号を指定）")]
        public bool[] showEmote;                // エモートを表示するかどうか
        public bool[] emoteOnLeftChar;          // trueなら左キャラ、falseなら右キャラ
        public int[] emoteIndices;              // sharedEmoteSprites の何番目を使うか (-1 なら非表示など)
    }

    public TMP_Text dialogueText;  // テキスト表示
    public TMP_Text NameText;  // 名前表示

    public Image LCharacterImage;  // 左キャラの画像
    public string LChNameText;//左キャラの名前


    public Image RCharacterImage; // 右キャラの画像
    public string RChNameText;//右キャラの名前


    public Animator nextArrow; // ページ送り
    public GameObject dialogueUI; // UI全体
    [Header("名前ウィンドウの見た目設定")]
    [SerializeField] private Image nameWindowBackground; // 名前ウィンドウの背景Image
    [SerializeField] private Sprite leftCharNameWindowSprite;  // 主人公側の名前ウィンドウ画像
    [SerializeField] private Sprite rightCharNameWindowSprite; // 右側キャラ側の名前ウィンドウ画像

    [Header("アイテムポップアップ用UI")]
    [SerializeField] private GameObject itemPopupPanel; // ポップアップ全体の背景や枠
    [SerializeField] private Image itemPopupImage;      // アイテムを表示するImage

    [Header("エモート用UI参照")]
    [SerializeField] private Image leftEmoteImage;         // 左キャラのエモート画像コンポーネント
    [SerializeField] private Image rightEmoteImage;        // 右キャラのエモート画像コンポーネント

    [Header("オート・スキップ設定")]
    [SerializeField] private float autoPlayDelay = 1.5f; // オート時のセリフ読み終わり待機時間
    private bool isAutoMode = false;
    private bool isSkipMode = false;
    private Coroutine autoPlayCoroutine;
    [SerializeField] private ToggleGroup toggleGroup;


    [Header("バックログ用UI参照")]
    [SerializeField] private GameObject logPanel;          // バックログ画面全体
    [SerializeField] private Transform logContentContainer;// ログの行を並べるContent
    [SerializeField] private GameObject logItemPrefab;     // 1行分のプレハブ（Text用）

    

    // バックログ1行分のデータ構造
    public struct LogData
    {
        public string speakerName; // 話者名
        public string message;     // セリフ本文
        public bool isLeftChar;    // trueなら主人公側、falseなら右側キャラ
    }

    private List<LogData> dialogueHistory = new List<LogData>();

    [Header("会話データ（導入・幕間）")]
    [SerializeField] private DialogueEvent[] introDialogueEvents;     // 導入会話データ
    [SerializeField] private DialogueEvent[] interludeDialogueEvents; // 幕間会話データ

    private DialogueEvent[] activeDialogueEvents; // 今回実際に再生する会話データ
    private int currentDialogueIndex = 0; // 現在の会話イベント
    private int currentSentenceIndex = 0; // 現在のセリフ
    private bool isTyping = false; // 文字表示中フラグ

    [SerializeField] private Color inactiveColor = new Color(1f, 0.8f, 0.2f, 1f); 
    private Color activeColor = Color.white;

    
    void Start()
    {
        
        dialogueUI.SetActive(false);
        StartDialogue();

    }

    void Update()
    {
        if (dialogueUI.activeSelf && Time.timeScale > 0f && Input.GetMouseButtonDown(0)) 
        {
            // UI（ボタンなど）の上をクリックした場合は進まないようにする場合のガード
            // ※画面全体どこでもクリックさせたい場合はこのif文を外してもOKです
            // ★ PC（マウス）の場合のUI判定
            #if UNITY_EDITOR || UNITY_STANDALONE
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return; // オプションボタンやUIの上をクリックしたときは会話を進めない
           
            #else
            // ★ スマホ（タッチ操作）の場合のUI判定
            if (EventSystem.current != null && Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
             return; // オプションボタンやUIの上をタップしたときは会話を進めない
          
            #endif

            NextSentence();
        }

        // スキップモード中、タイピングが終わっていたら自動で次へ進める
        if (isSkipMode && dialogueUI.activeSelf && !isTyping)
        {
            NextSentence();
        }

    }

    void StartDialogue()
    {
        if (toggleGroup != null)
        {
            toggleGroup.SetAllTogglesOff();
        }
        isAutoMode = false;
        isSkipMode = false;
        isTyping = false;
        dialogueHistory.Clear();

        // GameFlowManager の状態に応じて使用する会話データを切り替え
        if (GameFlowManager.Instance != null &&
            GameFlowManager.Instance.CurrentDialogueMode == GameFlowManager.DialogueMode.Interlude)
        {
            activeDialogueEvents = interludeDialogueEvents;
        }
        else
        {
            activeDialogueEvents = introDialogueEvents;
        }

        if (dialogueUI != null)
        {
            dialogueUI.SetActive(true);
        }

        currentDialogueIndex = 0;
        currentSentenceIndex = 0;

        if (activeDialogueEvents != null &&
            activeDialogueEvents.Length > 0 &&
            activeDialogueEvents[0].sentences != null &&
            activeDialogueEvents[0].sentences.Length > 0)
        {
            StartCoroutine(TypeSentence(activeDialogueEvents[currentDialogueIndex].sentences[currentSentenceIndex]));
            UpdateCharacterImage();
        }
        else
        {
            CloseDialogue();
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        bool isLeft = activeDialogueEvents[currentDialogueIndex].isLCharacter[currentSentenceIndex];

        // 新しいセリフが始まる前に、前のセリフのオート待機が残っていれば止める
        StopAutoWait();

        isTyping = true;
        dialogueText.text = "";

        if (nextArrow != null) nextArrow.gameObject.SetActive(false);

        // **\n を正しく改行に変換**
        sentence = sentence.Replace("\\n", "\n");
        int charCount = 0;

        // バックログにこのセリフを記録する
        string speakerName = isLeft ? LChNameText : RChNameText;
        AddLogHistory(speakerName, sentence, isLeft);

        float typeSpeed = isSkipMode ? 0.01f : 0.05f;

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;

            charCount++;
            if (!char.IsWhiteSpace(letter) && charCount % 2 == 0)
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.SEType voiceType = isLeft
                          ? SoundManager.SEType.Voice_Ether
                          : SoundManager.SEType.Voice_Arcana;

                    SoundManager.Instance.PlaySE(voiceType);
                }
            }

            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
        if (nextArrow != null) nextArrow.gameObject.SetActive(true);

        // 文字送りが完了した時点でオート中なら、ここで待機タイマーを開始する
        if (isAutoMode && !isSkipMode && dialogueUI != null && dialogueUI.activeSelf)
        {
            StartAutoWait();
        }
    }

    void NextSentence()
    {
        if (!dialogueUI.activeSelf || activeDialogueEvents == null) return;

        if (isTyping)
        {
            StopAllCoroutines();
            autoPlayCoroutine = null;
            dialogueText.text = activeDialogueEvents[currentDialogueIndex].sentences[currentSentenceIndex].Replace("\\n", "\n");
            isTyping = false;

            if (nextArrow != null) nextArrow.gameObject.SetActive(true);

            if (isAutoMode && !isSkipMode)
            {
                StartAutoWait();
            }
        }
        else
        {
            StopAutoWait();
            currentSentenceIndex++;

            if (currentSentenceIndex < activeDialogueEvents[currentDialogueIndex].sentences.Length)
            {
                StartCoroutine(TypeSentence(activeDialogueEvents[currentDialogueIndex].sentences[currentSentenceIndex]));
                UpdateCharacterImage();
            }
            else
            {
                currentSentenceIndex = 0;
                currentDialogueIndex++;

                if (currentDialogueIndex < activeDialogueEvents.Length)
                {
                    StartCoroutine(TypeSentence(activeDialogueEvents[currentDialogueIndex].sentences[currentSentenceIndex]));
                    UpdateCharacterImage();
                }
                else
                {
                    CloseDialogue();
                }
            }
        }
    }

    private void StopAutoWait()
    {
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }
    }

    void UpdateCharacterImage()
    {
        if (activeDialogueEvents == null) return;

        // インデックスが範囲内かチェック
        if (currentDialogueIndex < activeDialogueEvents.Length &&
            currentSentenceIndex < activeDialogueEvents[currentDialogueIndex].isLCharacter.Length)
        {
            bool isLeft = activeDialogueEvents[currentDialogueIndex].isLCharacter[currentSentenceIndex];
            Sprite charSprite = activeDialogueEvents[currentDialogueIndex].characterSprites[currentSentenceIndex];
            if (isLeft)
            {
                LCharacterImage.sprite = charSprite;
                LCharacterImage.color = activeColor; // 主人公を明るく
                RCharacterImage.color = inactiveColor; // 敵を薄く

                NameText.text = LChNameText;

                if (nameWindowBackground != null && leftCharNameWindowSprite != null)
                {
                    nameWindowBackground.sprite = leftCharNameWindowSprite;
                }
            }
            else
            {
                RCharacterImage.sprite = charSprite;
                RCharacterImage.color = activeColor; // 敵を明るく
                LCharacterImage.color = inactiveColor; // 主人公を薄く

                NameText.text = RChNameText;

                if (nameWindowBackground != null && rightCharNameWindowSprite != null)
                {
                    nameWindowBackground.sprite = rightCharNameWindowSprite;
                }
            }
        }
        UpdateItemPopup();
        UpdateEmote();
    }

    void CloseDialogue()
    {

        GameFlowManager.Instance.OnDialogueFinished();
    }

    void UpdateItemPopup()
    {
        if (itemPopupPanel == null || itemPopupImage == null) return;

        // 現在のインデックスに対応するアイテム画像があるかチェック
        Sprite itemSprite = null;
        if (currentDialogueIndex < activeDialogueEvents.Length &&
            currentSentenceIndex < activeDialogueEvents[currentDialogueIndex].popupItemSprites.Length)
        {
            itemSprite = activeDialogueEvents[currentDialogueIndex].popupItemSprites[currentSentenceIndex];
        }

        if (itemSprite != null)
        {
            itemPopupImage.sprite = itemSprite;
            itemPopupPanel.SetActive(true);

      
        }
        else
        {
            itemPopupPanel.SetActive(false);
        }
    }


    void UpdateEmote()
    {
        // まず両方のエモートを非表示にする
        if (leftEmoteImage != null) leftEmoteImage.enabled = false;
        if (rightEmoteImage != null) rightEmoteImage.enabled = false;

        if (currentDialogueIndex >= activeDialogueEvents.Length) return;
        var ev = activeDialogueEvents[currentDialogueIndex];

        // そもそもセリフデータが存在しない場合は抜ける
        if (ev.sentences == null || currentSentenceIndex >= ev.sentences.Length) return;

        // ★ 各エモート用配列の長さが、セリフの数より短い（設定し忘れ）場合は安全にスキップする
        if (ev.showEmote == null || currentSentenceIndex >= ev.showEmote.Length || !ev.showEmote[currentSentenceIndex])
            return;

        if (ev.emoteIndices == null || currentSentenceIndex >= ev.emoteIndices.Length)
            return;

        int emoteIndex = ev.emoteIndices[currentSentenceIndex];
        // 無効なインデックス（-1など）や、マスターリストの範囲外なら何もしない
        if (emoteIndex < 0 || sharedEmoteSprites == null || emoteIndex >= sharedEmoteSprites.Length)
            return;

        Sprite targetEmoteSprite = sharedEmoteSprites[emoteIndex];

        // 左右の判定（配列の長さが足りていない場合はデフォルトで左にするなどの安全策）
        bool isLeft = true;
        if (ev.emoteOnLeftChar != null && currentSentenceIndex < ev.emoteOnLeftChar.Length)
        {
            isLeft = ev.emoteOnLeftChar[currentSentenceIndex];
        }

        if (isLeft)
        {
            if (leftEmoteImage != null)
            {
                leftEmoteImage.sprite = targetEmoteSprite;
                leftEmoteImage.enabled = true;
            }
        }
        else
        {
            if (rightEmoteImage != null)
            {
                rightEmoteImage.sprite = targetEmoteSprite;
                rightEmoteImage.enabled = true;
            }
        }
    }

    /// <summary>
    /// オートモードのON/OFFを切り替える（ボタンに登録）
    /// </summary>
    public void ToggleAutoMode(bool isOn)
    {
        isAutoMode = isOn;
        if (isAutoMode)
        {
            isSkipMode = false;

            if (!isTyping && dialogueUI != null && dialogueUI.activeSelf)
            {
                StartAutoWait();
            }
        }
        else
        {
            if (autoPlayCoroutine != null)
            {
                StopCoroutine(autoPlayCoroutine);
                autoPlayCoroutine = null;
            }
        }
        Debug.Log($"オートモード: {isAutoMode}");
    }

    /// <summary>
    /// オート時の待機時間を挟んで次のセリフに進むコルーチンを安全に起動する
    /// </summary>
    private void StartAutoWait()
    {
        // 既に動いているオート待機があれば一度止める（重複実行防止）
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
        }
        autoPlayCoroutine = StartCoroutine(AutoWaitCoroutine());
    }

    IEnumerator AutoWaitCoroutine()
    {
        // 読み終わり待ち時間
        yield return new WaitForSeconds(autoPlayDelay);

        // 待機が終わった時点で、まだオート中かつタイピング中でなく、会話画面が開いていれば次へ進む
        if (isAutoMode && !isSkipMode && !isTyping && dialogueUI.activeSelf)
        {
            NextSentence();
        }
    }

    /// <summary>
    /// スキップモードのON/OFFを切り替える（ボタンに登録）
    /// </summary>
    public void ToggleSkipMode(bool isOn)
    {
        isSkipMode = isOn;

        if (isSkipMode)
        {
            isAutoMode = false;

            if (autoPlayCoroutine != null)
            {
                StopCoroutine(autoPlayCoroutine);
                autoPlayCoroutine = null;
            }
        }
        Debug.Log($"スキップモード: {isSkipMode}");
    }

    /// <summary>
    /// ログ履歴に1行追加する
    /// </summary>
    private void AddLogHistory(string speaker, string message, bool isLeft)
    {
        string cleanMessage = message.Replace("\n", " ");
        dialogueHistory.Add(new LogData
        {
            speakerName = speaker,
            message = cleanMessage,
            isLeftChar = isLeft
        });
    }

   
    /// <summary>
    /// バックログ画面を開く（ボタンに登録）
    /// </summary>
    public void OpenLogPanel()
    {
        if (logPanel == null || logItemPrefab == null || logContentContainer == null) return;

        // 既存のログ要素をクリア
        foreach (Transform child in logContentContainer)
        {
            Destroy(child.gameObject);
        }

        // 履歴を上から順に生成して表示
        foreach (LogData log in dialogueHistory)
        {
            GameObject itemObj = Instantiate(logItemPrefab, logContentContainer);

            DialogueLogItemUI itemUI = itemObj.GetComponent<DialogueLogItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(log);
            }
        }

        logPanel.SetActive(true);
        Time.timeScale = 0f; // ログ閲覧中は時間を止める
    }

    /// <summary>
    /// バックログ画面を閉じる（ボタンに登録）
    /// </summary>
    public void CloseLogPanel()
    {
        if (logPanel != null)
        {
            logPanel.SetActive(false);
        }
        Time.timeScale = 1f; // 時間再開
    }
}