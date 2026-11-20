using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MixingManager : MonoBehaviour
{
    [Header("データ参照")]
    [SerializeField] private OptionData optionData;
    [SerializeField] private MixingResultDatabase resultDatabase; // 調合結果データベース

    [Header("UI参照 - 素材一覧生成")]
    [SerializeField] private Transform ingredientButtonContainer; // ボタンを並べる親オブジェクト
    [SerializeField] private GameObject ingredientButtonPrefab;     // 素材ボタンのプレハブ

    [Header("UI参照 - 試作ポーションのプレビュー")]
    [SerializeField] private Image potionPreviewImage; // 色を反映させたい Image コンポーネント

    [Header("UI参照 - 選択状態表示")]
    [SerializeField] private TextMeshProUGUI slot1Text; // 選択素材1の表示用テキスト
    [SerializeField] private Image slot1Image;// 選択素材1の表示用イメージ
    [SerializeField] private TextMeshProUGUI slot2Text; // 選択素材2の表示用テキスト
    [SerializeField] private Image slot2Image;// 選択素材1の表示用イメージ
    [SerializeField] private Button mixButton;              // 「決定（調合）」ボタン
    [SerializeField] private TextMeshProUGUI mixButtonText; // ボタンのテキスト（「調合する」⇔「試飲する」の切り替え用）
    [SerializeField] private Button resetButton;          // リセットボタン

    [Header("UI参照 - 温度調整ボタン（3つ）")]
    [SerializeField] private Button coldTempButton;   // 0: 冷却ボタン
    [SerializeField] private Button normalTempButton; // 1: 常温ボタン
    [SerializeField] private Button hotTempButton;    // 2: 加熱ボタン

    [SerializeField] private Sprite[] sprites; // 切り替える画像を格納する配列
    [SerializeField] private Image targetImage; // UIのImageを使う場合

    [Header("UI参照 - 温度テキスト")]
    [SerializeField] private TextMeshProUGUI temperatureStatusText;

    [Header("温度ボタンの色設定")]
    [SerializeField] private Color activeColor = new Color(1f, 0.8f, 0.2f, 1f); // 選択中の色
    [SerializeField] private Color inactiveColor = Color.white;

    [Header("UI参照 - 観察・試飲結果の表示パネル")]
    [SerializeField] private TextMeshProUGUI tasteResultText;     // 【助手の試飲レポート】テキスト

    [Header("失敗時の設定")]
    [SerializeField] private Color failColor = Color.gray;        // ★ 失敗時の灰色

    [Header("金属演出の設定")]
    [SerializeField] private Material metalMaterial;
    [SerializeField] private Material rainbowMaterial;

    // ★ 調合の進行フェーズ
    private enum PotionPhase
    {
        Inputting,  // 素材選択中
        Mixed,      // 調合完了（色と香りが判明、試飲待ち）
        Tasted,      // 試飲完了（味と効果が判明）
        Failed      //調合失敗
    }
    private PotionPhase currentPhase = PotionPhase.Inputting;

    // 今回の結果データを保持しておく変数
    private MixingResultDatabase.MixRecipe cachedResult;
    private bool cachedIsSuccess = false;

    // ★ ログ登録用に、調合時に確定した素材と温度を保持しておくキャッシュ変数
    private string cachedIngredient1 = string.Empty;
    private string cachedIngredient2 = string.Empty;
    private MixingResultDatabase.Temperature cachedTemp;

    // 選択中の素材（最大2つ）
    private string selectedIngredient1 = string.Empty;
    private string selectedIngredient2 = string.Empty;

    // 生成したボタンのインスタンス保持用
    private List<IngredientButton> createdButtons = new List<IngredientButton>();

    // 現在選択されている温度
    private MixingResultDatabase.Temperature currentTemp = MixingResultDatabase.Temperature.Normal;

    // 温度ボタンの Image 参照保持用
    private Image coldBtnImage;
    private Image normalBtnImage;
    private Image hotBtnImage;

    void Start()
    {
        if (coldTempButton != null) coldBtnImage = coldTempButton.GetComponent<Image>();
        if (normalTempButton != null) normalBtnImage = normalTempButton.GetComponent<Image>();
        if (hotTempButton != null) hotBtnImage = hotTempButton.GetComponent<Image>();

        InitializeIngredientList();

        if (coldTempButton != null) coldTempButton.onClick.AddListener(() => SetTemperature(0));
        if (normalTempButton != null) normalTempButton.onClick.AddListener(() => SetTemperature(1));
        if (hotTempButton != null) hotTempButton.onClick.AddListener(() => SetTemperature(2));

        if (mixButton != null)
        {
            // ★ ボタンの役割を動的に切り替えるため、共通のクリックメソッドにまとめる
            mixButton.onClick.AddListener(OnActionButtonClicked);
        }

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetSelection);
        }

        if (tasteResultText != null)
        {
            tasteResultText.text = $"【観察レポート】\n";
        }

        UpdateTemperatureUI();
        UpdateUI();

        SoundManager.Instance.PlayBGMByName("Fancy_Pop");
    }

    private void InitializeIngredientList()
    {
        if (optionData == null || ingredientButtonContainer == null || ingredientButtonPrefab == null) return;

        foreach (Transform child in ingredientButtonContainer)
        {
            Destroy(child.gameObject);
        }
        createdButtons.Clear();

        foreach (var item in optionData.IngredientItems)
        {
            string ingredientName = item.ingredientName;
            Sprite ingredientIcon = item.ingredientIcon;

            if (string.IsNullOrEmpty(ingredientName) || ingredientName == "？？？" || ingredientName == "選択してください")
                continue;

            GameObject btnObj = Instantiate(ingredientButtonPrefab, ingredientButtonContainer);
            IngredientButton btnScript = btnObj.GetComponent<IngredientButton>();

            if (btnScript != null)
            {
                btnScript.Setup(ingredientName, ingredientIcon, OnIngredientClicked);
                createdButtons.Add(btnScript);
            }
        }
    }

    private void OnIngredientClicked(string name)
    {
        // 完了後や試飲後は素材選択不可
        if (currentPhase != PotionPhase.Inputting) return;

        if (selectedIngredient1 == name && selectedIngredient2 == name)
        {
            selectedIngredient2 = string.Empty;
        }
        else if (string.IsNullOrEmpty(selectedIngredient1))
        {
            selectedIngredient1 = name;
        }
        else if (string.IsNullOrEmpty(selectedIngredient2))
        {
            selectedIngredient2 = name;
        }
        else if (selectedIngredient1 == name)
        {
            selectedIngredient1 = string.Empty;
        }
        else if (selectedIngredient2 == name)
        {
            selectedIngredient2 = string.Empty;
        }
        else
        {
            selectedIngredient2 = name;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (slot1Text != null) 
            slot1Text.text = string.IsNullOrEmpty(selectedIngredient1) ? "素材1" : selectedIngredient1;

        if (slot1Image != null)
        {
            Sprite icon1 = GetIngredientIcon(selectedIngredient1);
            slot1Image.sprite = icon1;
            slot1Image.gameObject.SetActive(icon1 != null);
        }


        if (slot2Text != null)
            slot2Text.text = string.IsNullOrEmpty(selectedIngredient2) ? "素材2" : selectedIngredient2;

        if (slot2Image != null)
        {
            Sprite icon2 = GetIngredientIcon(selectedIngredient2);
            slot2Image.sprite = icon2;
            slot2Image.gameObject.SetActive(icon2 != null);
        }


        // ★ 調合中（Inputting）のときだけ温度ボタンを押せるようにする
        bool canChangeTemp = (currentPhase == PotionPhase.Inputting);
        if (coldTempButton != null) coldTempButton.interactable = canChangeTemp;
        if (normalTempButton != null) normalTempButton.interactable = canChangeTemp;
        if (hotTempButton != null) hotTempButton.interactable = canChangeTemp;

        // フェーズに応じたボタンの状態とテキストの切り替え
        if (mixButton != null)
        {
            if (currentPhase == PotionPhase.Inputting)
            {
                bool canMix = !string.IsNullOrEmpty(selectedIngredient1) && !string.IsNullOrEmpty(selectedIngredient2);
                mixButton.interactable = canMix;
                if (mixButtonText != null) mixButtonText.text = "調合する";
                               
            }
            else if (currentPhase == PotionPhase.Mixed)
            {
                mixButton.interactable = true; // 試飲可能
                if (mixButtonText != null) mixButtonText.text = "試飲する";
            }
            else　if(currentPhase == PotionPhase.Failed) // Tasted (試飲完了後)
            {
                mixButton.interactable = false; // これ以上押せない
                if (mixButtonText != null) mixButtonText.text = "失敗";
            }
            else
            {
                mixButton.interactable = false; // これ以上押せない
                if (mixButtonText != null) mixButtonText.text = "試飲済み";
            }
        }

        bool hasSelection = !string.IsNullOrEmpty(selectedIngredient1) ||
                            !string.IsNullOrEmpty(selectedIngredient2) ||
                            currentPhase != PotionPhase.Inputting;
        if (resetButton != null)
        {
            resetButton.interactable = hasSelection;
        }
    }

    public void SetTemperature(int tempIndex)
    {
        if (currentPhase != PotionPhase.Inputting) return; // 選択中以外は温度変更不可

        currentTemp = (MixingResultDatabase.Temperature)tempIndex;
        UpdateTemperatureUI();
    }

    private void UpdateTemperatureUI()
    {
        bool isCold = (currentTemp == MixingResultDatabase.Temperature.Cold);
        bool isNormal = (currentTemp == MixingResultDatabase.Temperature.Normal);
        bool isHot = (currentTemp == MixingResultDatabase.Temperature.Hot);

        if (coldBtnImage != null) coldBtnImage.color = isCold ? activeColor : inactiveColor;
        if (normalBtnImage != null) normalBtnImage.color = isNormal ? activeColor : inactiveColor;
        if (hotBtnImage != null) hotBtnImage.color = isHot ? activeColor : inactiveColor;

        string tempName = "常温";//0
        if (temperatureStatusText != null)
        {
            if (isCold) tempName = "冷却";//1
            else if (isHot) tempName = "加熱";//2

            temperatureStatusText.text = $"現在の温度：{tempName}";


        }

         int currentIndex = 0;

        switch (tempName)
        {
            case "加熱":
                currentIndex = 2;
                break; 

            case "冷却":
                currentIndex = 1;
                break;

            default:
                currentIndex = 0;
                break;
        }

        if (targetImage != null)
        {
            targetImage.sprite = sprites[currentIndex];
        }

    }

    /// <summary>
    /// 調合ボタン 兼 試飲ボタンが押されたときのルーター
    /// </summary>
    private void OnActionButtonClicked()
    {
        if (currentPhase == PotionPhase.Inputting)
        {
            ExecuteMixing(); // 1段階目：調合実行（色と香り）
        }
        else if (currentPhase == PotionPhase.Mixed)
        {
            ExecuteTasting(); // 2段階目：試飲実行（味と効果、助手のドット絵連動用）
        }
    }

    /// <summary>
    /// 【第1段階】調合する：色と香りのみを表示する
    /// </summary>
    private void ExecuteMixing()
    {
        if (string.IsNullOrEmpty(selectedIngredient1) || string.IsNullOrEmpty(selectedIngredient2)) return;
        if (resultDatabase == null) return;

        // ★ 調合した瞬間の素材と温度をキャッシュにしっかりと保存する！
        cachedIngredient1 = selectedIngredient1;
        cachedIngredient2 = selectedIngredient2;
        cachedTemp = currentTemp;

        cachedResult = resultDatabase.GetResult(cachedIngredient1, cachedIngredient2, cachedTemp);
     

        // 成功判定
        cachedIsSuccess = false;
        if (cachedResult != null)
        {
            bool hasComment = !string.IsNullOrEmpty(cachedResult.tasteComment);
            bool hasValidColor = cachedResult.resultColor.a > 0f && cachedResult.resultColor != Color.clear;
            if (hasComment && hasValidColor) cachedIsSuccess = true;
        }

        if (cachedIsSuccess)
        {  
            // フェーズを「調合完了」に移行
            currentPhase = PotionPhase.Mixed;

            SoundManager.Instance.PlaySEByName("決定ボタン1");

            // 1. ポーションプレビューの色・マテリアル反映（見た目のヒント）
            if (potionPreviewImage != null)
            {
                potionPreviewImage.color = cachedIsSuccess ? cachedResult.resultColor : failColor;
            }
            CheckAndApplyMaterial(cachedResult != null ? cachedResult.colorName : "");

            // 2. テキストエリアを「香り（および観察結果）」に上書き！
            if (tasteResultText != null)
            {
                string scent = cachedResult != null ? cachedResult.scentComment : "無臭";
                tasteResultText.text = $"【観察レポート】\n{scent}";
            }
        }
        else 
        {
            currentPhase = PotionPhase.Failed;

            SoundManager.Instance.PlaySEByName("決定ボタン1");

            if (potionPreviewImage != null)
            {
                potionPreviewImage.color = cachedIsSuccess ? cachedResult.resultColor : failColor;
            }
            CheckAndApplyMaterial(cachedResult != null ? cachedResult.colorName : "");

            // 2. テキストエリアを「香り（および観察結果）」に上書き！
            if (tasteResultText != null)
            {
                string scent = cachedResult != null ? cachedResult.scentComment : "無臭";
                tasteResultText.text = $"【観察レポート】\n{scent}";
            }
        }


        

        // 素材スロット選択をクリアして試飲ボタンへ変化させる
        selectedIngredient1 = string.Empty;
        selectedIngredient2 = string.Empty;
        UpdateUI();
    }

    /// <summary>
    /// 【第2段階】試飲する：味と効果を表示し、助手ちゃんを連動させる
    /// </summary>
    private void ExecuteTasting()
    {
        currentPhase = PotionPhase.Tasted; // フェーズを「試飲完了」に移行

        SoundManager.Instance.PlaySEByName("決定ボタン1");

        if (cachedIsSuccess)
        {
            // ★ クリアされてしまった `selectedIngredient1` ではなく、
            //    保持しておいた `cachedIngredient1` などの安全なデータを使ってログに登録する！
            RecipeDataStore.AddLog(new RecipeDataStore.MixingLog
            {
                ingredientA = cachedIngredient1,
                ingredientB = cachedIngredient2,
                temp = cachedTemp,
                scentComment = cachedResult.scentComment,
                tasteComment = cachedResult.tasteComment
            });

            // テキストエリアを「味と効果のレポート」に上書き！
            if (tasteResultText != null)
            {
                tasteResultText.text = $"【試飲レポート】\n{cachedResult.tasteComment}";
            }

            // ★ ここで助手ちゃんのドット絵アニメーション/リアクションを効果に応じて呼び出せます！
            // TriggerAssistantReaction(cachedResult.tasteComment);
        }
        else
        {
            // 失敗時
            if (tasteResultText != null)
            {
                string scent = cachedResult != null ? cachedResult.scentComment : "無臭";
                tasteResultText.text = $"【観察レポート】\n{scent}";
            }
        }

        UpdateUI();
    }

    private void CheckAndApplyMaterial(string colName)
    {
        if (potionPreviewImage == null) return;

        bool isMetal = !string.IsNullOrEmpty(colName) &&
                       (colName.Contains("金") ||
                        colName.Contains("銀") ||
                        colName.Contains("銅") ||
                        colName.Contains("黒曜石") ||
                        colName.Contains("コバルト"));
        bool isRainbow = !string.IsNullOrEmpty(colName) && colName.Contains("虹");

        if (isMetal && metalMaterial != null)
        {
            potionPreviewImage.material = metalMaterial;
        }
        else if (isRainbow && rainbowMaterial != null)
        {
            potionPreviewImage.material = rainbowMaterial;
        }
        else
        {
            potionPreviewImage.material = null;
        }
    }

    public void ReuseLogSettings(RecipeDataStore.MixingLog log)
    {
        ResetSelection();

        selectedIngredient1 = log.ingredientA;
        selectedIngredient2 = log.ingredientB;
        currentTemp = log.temp;

        UpdateTemperatureUI();
        UpdateUI();
    }

    /// <summary>
    /// リセットボタン押下時
    /// </summary>
    public void ResetSelection()
    {
        selectedIngredient1 = string.Empty;
        selectedIngredient2 = string.Empty;
        currentPhase = PotionPhase.Inputting;
        cachedResult = null;
        cachedIsSuccess = false;
        cachedIngredient1 = string.Empty;
        cachedIngredient2 = string.Empty;

        SoundManager.Instance.PlaySEByName("キャンセル1");

        if (potionPreviewImage != null)
        {
            potionPreviewImage.color = Color.white;
            potionPreviewImage.material = null;
        }

        if (tasteResultText != null)
        {
            tasteResultText.text = "【観察レポート】\n";
        }

        UpdateUI();
    }

    /// <summary>
    /// 素材名から対応するSprite（アイコン）を探して返す
    /// </summary>
    private Sprite GetIngredientIcon(string ingredientName)
    {
        if (string.IsNullOrEmpty(ingredientName) || optionData == null) return null;

        foreach (var item in optionData.IngredientItems)
        {
            if (item.ingredientName == ingredientName)
            {
                return item.ingredientIcon;
            }
        }
        return null;
    }
}