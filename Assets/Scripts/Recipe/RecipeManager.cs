using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeManager : MonoBehaviour
{
    // ★ UI更新中かどうかを判定するフラグ
    private bool isUpdatingUI = false;

    [System.Serializable]
    public class RecipeSlot
    {
        public RecipeData targetRecipe;       // 正解データ（ScriptableObject）
        [HideInInspector] public bool isConfirmed = false; // 確定フラグ

        // プレイヤーが入力中の選択値を保持する変数（名前の代わりに「効能」を保持）
        [HideInInspector] public string inputEffect;
        [HideInInspector] public string inputA;
        [HideInInspector] public string inputB;
        [HideInInspector] public string inputColor;
        [HideInInspector] public string inputScent;
    }

    [Header("目次（インデックス）UI参照")]
    [SerializeField] private GameObject tocPanel;              // 目次パネル（オブジェクト全体）
    [SerializeField] private Transform tocButtonContainer;    // 目次のボタンを並べる親（Content等）
    [SerializeField] private GameObject tocButtonPrefab;       // 目次の1項目分のボタンプレハブ

    [SerializeField] private int tocItemsPerPage = 10; // 1見開き（2行×5列や左右5+5など）で表示する数
    [SerializeField] private TextMeshProUGUI tocPageText; // 例：「目次 1 / 3」
    [SerializeField] private GameObject tocPrevButton;    // 目次「前へ」ボタン
    [SerializeField] private GameObject tocNextButton;    // 目次「次へ」ボタン
    private int currentTocPageIndex = 0;

    [Header("材料解説ページUI参照")]
    [SerializeField] private GameObject ingredientGuidePanel;  // 目次の次に開く材料解説パネル

    [Header("画面固定UI参照")]
    [SerializeField] private RectTransform sceneChangeButtonRect; // シーンチェンジボタン（または共通ヘッダー）
    [SerializeField] private RectTransform optionButtonRect;

    [Header("共通選択肢データ")]
    [SerializeField] private OptionData optionData;

    [Header("1行分のUI制御（単体使い回し）")]
    [SerializeField] private RecipeRowUI rowUI;

    [Header("UI参照 - 進捗表示")]
    [SerializeField] private TextMeshProUGUI contentsprogressText;  // 例：「解明済み：3 / 15」を表示するテキスト

    [Header("ページ送りUI参照")]
    [SerializeField] private TextMeshProUGUI pageText; // 「レシピ 1 / 15」などのページ表示用テキスト
    [SerializeField] private GameObject prevButton;  // 「前へ」ボタンのGameObject
    [SerializeField] private GameObject nextButton;  // 「次へ」ボタンのGameObject

    [Header("全レシピのデータリスト")]
    [SerializeField] private List<RecipeSlot> recipePages = new List<RecipeSlot>();

    private int currentPageIndex = 0;

    // 確定済みの「効能」と「色」を記録する集合（重複なし）
    private HashSet<string> confirmedEffects = new HashSet<string>();
    private HashSet<string> confirmedColors = new HashSet<string>();

    private void Start()
    {
        UpdateProgressDisplay();

        if (rowUI == null || optionData == null)
        {
            Debug.LogError("RecipeManager: rowUI または optionData がインスペクターでセットされていません！");
            return;
        }

        // データの復元処理
        for (int i = 0; i < recipePages.Count; i++)
        {
            var saved = RecipeDataStore.GetPageData(i);
            if (saved != null)
            {
                recipePages[i].isConfirmed = saved.isConfirmed;
                recipePages[i].inputEffect = saved.inputEffect;
                recipePages[i].inputA = saved.inputA;
                recipePages[i].inputB = saved.inputB;
                recipePages[i].inputColor = saved.inputColor;
                recipePages[i].inputScent = saved.inputScent;
            }
            else
            {
                if (string.IsNullOrEmpty(recipePages[i].inputEffect)) recipePages[i].inputEffect = "？？？";
                if (string.IsNullOrEmpty(recipePages[i].inputA)) recipePages[i].inputA = "？？？";
                if (string.IsNullOrEmpty(recipePages[i].inputB)) recipePages[i].inputB = "？？？";
                if (string.IsNullOrEmpty(recipePages[i].inputColor)) recipePages[i].inputColor = "？？？";
                if (string.IsNullOrEmpty(recipePages[i].inputScent)) recipePages[i].inputScent = "？？？";
            }
        }

        // 目次ボタンの動的生成
        InitializeTableOfContents();

        // ゲーム起動時は必ず「目次」を開いた状態にする
        OpenTableOfContents();

        // コールバック接続
        rowUI.SetOnDropdownChangedCallback(OnDropdownValueChanged);
    }

    /// <summary>
    /// 未確定時は「No.1」、確定後はポーション名を返す
    /// </summary>
    private string GetDisplayPotionName(int index, RecipeSlot slot)
    {
        string fallbackNumber = $"No.{index + 1}";

        if (slot == null || slot.targetRecipe == null)
        {
            return fallbackNumber;
        }

        if (slot.isConfirmed && !string.IsNullOrEmpty(slot.targetRecipe.PotionName))
        {
            return $"{fallbackNumber}.{slot.targetRecipe.PotionName}";
        }

        return fallbackNumber;
    }

    /// <summary>
    /// 効能メモの表示テキストを取得する（全クリア後などに虫食いなしへ切り替える場合はここで制御）
    /// </summary>
    private string GetDisplayEffectMemo(RecipeData recipe, bool useFullVersion = false)
    {
        if (recipe == null) return "";

        if (useFullVersion && !string.IsNullOrEmpty(recipe.TrueEffectMemo))
        {
            return recipe.TrueEffectMemo;
        }

        return recipe.EffectMemo;
    }

    /// <summary>
    /// ドロップダウン変更時にコールバックされるメイン処理
    /// </summary>
    private void OnDropdownValueChanged()
    {
        if (isUpdatingUI) return;

        SaveCurrentInput();

        RecipeSlot currentPage = recipePages[currentPageIndex];

        RecipeDataStore.SavePageData(
            currentPageIndex,
            currentPage.isConfirmed,
            currentPage.inputEffect,
            currentPage.inputA,
            currentPage.inputB,
            currentPage.inputColor,
            currentPage.inputScent
        );

        if (currentPage.isConfirmed) return;

        if (rowUI.IsAllElementsSelected())
        {
            CheckCurrentAnswerAuto();
        }
    }

    /// <summary>
    /// 指定したレシピページを表示に反映する
    /// </summary>
    private void UpdatePageDisplay()
    {
        if (recipePages.Count == 0 || rowUI == null || optionData == null) return;

        isUpdatingUI = true;

        try
        {
            RecipeSlot currentPage = recipePages[currentPageIndex];

            CleanUpConfirmedInputs(currentPage);

            // 未確定なら No.1 / No.2、確定後ならポーション名を表示
            string displayName = GetDisplayPotionName(currentPageIndex, currentPage);
            rowUI.SetPotionNameText(displayName);

            if (currentPage.targetRecipe != null)
            {
                rowUI.SetEffectMemo(GetDisplayEffectMemo(currentPage.targetRecipe, useFullVersion: false));
            }
            else
            {
                rowUI.SetEffectMemo("");
            }

            if (currentPage.targetRecipe != null && currentPage.isConfirmed)
            {
                rowUI.SetThoughtsMemo(currentPage.targetRecipe.ThoughtsMemo);
            }
            else
            {
                rowUI.SetThoughtsMemo("");
            }

            // ハンコ・メモの表示切り替え
            rowUI.SetStampActive(currentPage.isConfirmed);
            rowUI.SetMemoActive(currentPage.isConfirmed);

            RefreshConfirmedLists();

            rowUI.InitializeDropdown(
                optionData,
                confirmedEffects,
                confirmedColors,
                currentPage.inputEffect,
                currentPage.inputColor
            );

            rowUI.SetSelectedValues(
                currentPage.inputEffect,
                currentPage.inputA,
                currentPage.inputB,
                currentPage.inputColor,
                currentPage.inputScent
            );

            rowUI.SetInteractable(!currentPage.isConfirmed);
            HideDropdownArrows(!currentPage.isConfirmed);
            rowUI.SetDropdownSprites(currentPage.isConfirmed);

            UpdateProgressDisplay();

            if (pageText != null)
            {
                pageText.text = $"レシピ {currentPageIndex + 1} / {recipePages.Count}";
            }

            UpdateNavigationButtons();
        }
        finally
        {
            isUpdatingUI = false;
        }
    }

    private void UpdateNavigationButtons()
    {
        if (prevButton != null)
        {
            // 材料解説パネルがあるなら No.1（index 0）でも「前へ」を表示
            bool canGoPrev = ingredientGuidePanel != null ? (currentPageIndex >= 0) : (currentPageIndex > 0);
            prevButton.SetActive(canGoPrev);
        }

        if (nextButton != null)
        {
            nextButton.SetActive(currentPageIndex < recipePages.Count - 1);
        }
    }

    private void UpdateProgressDisplay()
    {
        if (contentsprogressText == null || recipePages == null || recipePages.Count == 0) return;

        int confirmedCount = 0;
        foreach (var page in recipePages)
        {
            if (page.isConfirmed)
            {
                confirmedCount++;
            }
        }

        contentsprogressText.text = $"解明済み：{confirmedCount} / {recipePages.Count}";
    }

    private void RefreshConfirmedLists()
    {
        confirmedEffects.Clear();
        confirmedColors.Clear();

        foreach (var page in recipePages)
        {
            if (page.isConfirmed && page.targetRecipe != null)
            {
                confirmedEffects.Add(page.targetRecipe.PotionEffectt);
                confirmedColors.Add(page.targetRecipe.PotionColor);
            }
        }
    }

    private void SaveCurrentInput()
    {
        if (recipePages.Count == 0 || rowUI == null) return;

        RecipeSlot currentPage = recipePages[currentPageIndex];

        if (currentPage.isConfirmed) return;

        currentPage.inputEffect = rowUI.GetSelectedEffect();
        currentPage.inputA = rowUI.GetSelectedIngredientA();
        currentPage.inputB = rowUI.GetSelectedIngredientB();
        currentPage.inputColor = rowUI.GetSelectedColor();
        currentPage.inputScent = rowUI.GetSelectedScent();
    }

    #region ページ送り・目次／材料解説遷移

    public void NextPage()
    {
        SaveCurrentInput();

        if (currentPageIndex < recipePages.Count - 1)
        {
            currentPageIndex++;
            UpdatePageDisplay();
        }
    }

    public void PrevPage()
    {
        SaveCurrentInput();

        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            UpdatePageDisplay();
        }
        else if (ingredientGuidePanel != null)
        {
            OpenIngredientGuide();
        }
    }

    /// <summary>
    /// 目次の上部「次へ」ボタンから呼ぶ：材料解説ページを開く
    /// </summary>
    public void OpenIngredientGuide()
    {
        Debug.Log($"[TOC] OpenIngredientGuide called. tocPanel={(tocPanel != null)}, guide={(ingredientGuidePanel != null)}");
        SaveCurrentInput();

        if (tocPanel != null)
        {
            tocPanel.SetActive(false);
        }

        if (rowUI != null)
        {
            rowUI.gameObject.SetActive(false);
        }

        if (ingredientGuidePanel != null)
        {
            ingredientGuidePanel.SetActive(true);
            ingredientGuidePanel.transform.SetAsLastSibling();
            Debug.Log($"[TOC] Guide activeSelf={ingredientGuidePanel.activeSelf}, activeInHierarchy={ingredientGuidePanel.activeInHierarchy}");
        }

        BringFixedButtonsToFront();
    }

    /// <summary>
    /// 材料解説ページの「次へ」ボタンから呼ぶ：No.1（インデックス0）のレシピページへ進む
    /// </summary>
    public void StartRecipesFromFirst()
    {
        if (ingredientGuidePanel != null)
        {
            ingredientGuidePanel.SetActive(false);
        }

        JumpToPage(0);
    }

    #endregion

    /// <summary>
    /// 外部や目次ボタンから直接指定ページにジャンプする
    /// </summary>
    public void JumpToPage(int pageIndex)
    {
        currentPageIndex = Mathf.Clamp(pageIndex, 0, recipePages.Count - 1);

        if (tocPanel != null)
        {
            tocPanel.SetActive(false);
        }

        if (ingredientGuidePanel != null)
        {
            ingredientGuidePanel.SetActive(false);
        }

        if (rowUI != null)
        {
            rowUI.gameObject.SetActive(true);
        }

        UpdatePageDisplay();
    }

    public List<RecipeSlot> GetRecipePages()
    {
        return recipePages;
    }

    private int GetTotalTocPages()
    {
        if (recipePages == null || recipePages.Count == 0) return 1;
        int perPage = Mathf.Max(1, tocItemsPerPage);
        return Mathf.CeilToInt((float)recipePages.Count / perPage);
    }

    /// <summary>
    /// 目次のボタン一覧を生成する（未確定は No.1, No.2 / 確定後はポーション名）
    /// </summary>
    private void InitializeTableOfContents()
    {
        if (tocButtonContainer == null || tocButtonPrefab == null) return;

        foreach (Transform child in tocButtonContainer)
        {
            Destroy(child.gameObject);
        }

        int perPage = Mathf.Max(1, tocItemsPerPage);
        int totalPages = GetTotalTocPages();
        currentTocPageIndex = Mathf.Clamp(currentTocPageIndex, 0, totalPages - 1);

        int startIndex = currentTocPageIndex * perPage;
        int endIndex = Mathf.Min(startIndex + perPage, recipePages.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            int recipeIndex = i;
            GameObject btnObj = Instantiate(tocButtonPrefab, tocButtonContainer);

            var textComp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = GetDisplayPotionName(i, recipePages[i]);
            }

            bool isConfirmed = recipePages[i].isConfirmed;
            Transform checkIconTrans = btnObj.transform.Find("CheckIcon");
            if (checkIconTrans != null)
            {
                checkIconTrans.gameObject.SetActive(isConfirmed);
            }

            var button = btnObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    JumpToPage(recipeIndex);
                });
            }
        }

        UpdateTocNavigationUI(totalPages);
    }


    /// <summary>
    /// 目次ページ送り用のメソッド
    /// </summary>

    private bool IsLastTocPage()
    {
        return currentTocPageIndex >= GetTotalTocPages() - 1;
    }

    public void NextTocPage()
    {
        int totalPages = GetTotalTocPages();
        Debug.Log($"[TOC] NextTocPage called: current={currentTocPageIndex}, total={totalPages}");

        if (currentTocPageIndex < totalPages - 1)
        {
            currentTocPageIndex++;
            Debug.Log($"[TOC] Incremented currentTocPageIndex to {currentTocPageIndex}");
            InitializeTableOfContents();
        }
        else
        {
            Debug.Log("[TOC] Reached last TOC page -> calling OpenIngredientGuide()");
            OpenIngredientGuide();
        }
    }

    public void PrevTocPage()
    {
        int totalPages = GetTotalTocPages();
        Debug.Log($"[TOC] PrevTocPage before: current={currentTocPageIndex}, total={totalPages}");

        if (currentTocPageIndex > 0)
        {
            currentTocPageIndex--;
            Debug.Log($"[TOC] Decremented currentTocPageIndex to {currentTocPageIndex}");
            InitializeTableOfContents();
        }
    }

    private void UpdateTocNavigationUI(int totalPages)
    {
        if (tocPageText != null)
        {
            tocPageText.text = $"目次 {currentTocPageIndex + 1} / {totalPages}";
        }

        if (tocPrevButton != null)
        {
            tocPrevButton.SetActive(currentTocPageIndex > 0);
        }

        // 最終目次ページでも「次へ（＝材料ガイドへ）」を押せるよう表示を維持する
        if (tocNextButton != null)
        {
            tocNextButton.SetActive(true);
        }
    }

    /// <summary>
    /// 目次を開く（どの画面からでも呼べる）
    /// </summary>
    public void OpenTableOfContents()
    {
        Debug.Log($"[TOC] OpenTableOfContents called (currentTocPageIndex={currentTocPageIndex})");
        SaveCurrentInput();

        if (rowUI != null)
        {
            rowUI.gameObject.SetActive(false);
        }

        if (ingredientGuidePanel != null)
        {
            ingredientGuidePanel.SetActive(false);
        }

        if (tocPanel != null)
        {
            tocPanel.SetActive(true);
            tocPanel.transform.SetAsLastSibling();
        }

        BringFixedButtonsToFront();
        InitializeTableOfContents();
    }


    private void BringFixedButtonsToFront()
    {
        if (sceneChangeButtonRect != null)
        {
            sceneChangeButtonRect.SetAsLastSibling();
        }

        if (optionButtonRect != null)
        {
            optionButtonRect.SetAsLastSibling();
        }
    }

    private void CheckCurrentAnswerAuto()
    {
        RecipeSlot currentPage = recipePages[currentPageIndex];

        if (currentPage.targetRecipe == null) return;

        if (currentPage.targetRecipe.recipeChecker(
            currentPage.inputA,
            currentPage.inputB,
            currentPage.inputColor,
            currentPage.inputScent,
            currentPage.inputEffect))
        {
            currentPage.isConfirmed = true;
            rowUI.SetDropdownSprites(true);

            foreach (var p in recipePages)
            {
                CleanUpConfirmedInputs(p);
            }

            RecipeDataStore.SavePageData(
                currentPageIndex,
                currentPage.isConfirmed,
                currentPage.inputEffect,
                currentPage.inputA,
                currentPage.inputB,
                currentPage.inputColor,
                currentPage.inputScent
            );

            UpdatePageDisplay();

            Debug.Log($"【確定！】レシピ {currentPageIndex + 1} が確定しました！");

            CheckProgressionAfterConfirm();
        }
    }

    public void CheckAnswer()
    {
        if (recipePages.Count == 0 || rowUI == null) return;

        RecipeSlot currentPage = recipePages[currentPageIndex];

        if (currentPage.isConfirmed)
        {
            Debug.Log($"レシピ[{currentPageIndex + 1}] はすでに確定済みです。");
            return;
        }

        SaveCurrentInput();

        if (currentPage.targetRecipe.recipeChecker(
            currentPage.inputA,
            currentPage.inputB,
            currentPage.inputColor,
            currentPage.inputScent,
            currentPage.inputEffect))
        {
            currentPage.isConfirmed = true;
            rowUI.SetInteractable(false);
            HideDropdownArrows(false);
            rowUI.SetDropdownSprites(true);

            foreach (var p in recipePages)
            {
                CleanUpConfirmedInputs(p);
            }

            SaveAllPagesDataForSceneChange();
            UpdatePageDisplay();

            Debug.Log($"【確定！】レシピ {currentPageIndex + 1} が正解と完全一致しました！");

            CheckProgressionAfterConfirm();
        }
    }

    private void CleanUpConfirmedInputs(RecipeSlot page)
    {
        if (page.isConfirmed || page.targetRecipe == null) return;

        HashSet<string> confirmedEffectsSet = new HashSet<string>();
        HashSet<string> confirmedColorsSet = new HashSet<string>();

        foreach (var p in recipePages)
        {
            if (p.isConfirmed && p.targetRecipe != null)
            {
                confirmedEffectsSet.Add(p.targetRecipe.PotionEffectt);
                confirmedColorsSet.Add(p.targetRecipe.PotionColor);
            }
        }

        if (confirmedEffectsSet.Contains(page.inputEffect)) page.inputEffect = "？？？";
        if (confirmedColorsSet.Contains(page.inputColor)) page.inputColor = "？？？";
    }

    public void SaveAllPagesDataForSceneChange()
    {
        SaveCurrentInput();

        for (int i = 0; i < recipePages.Count; i++)
        {
            RecipeSlot slot = recipePages[i];
            RecipeDataStore.SavePageData(
                i,
                slot.isConfirmed,
                slot.inputEffect,
                slot.inputA,
                slot.inputB,
                slot.inputColor,
                slot.inputScent
            );
        }
    }

    private void CheckAllRecipesCleared()
    {
        if (!IsAllRecipesCleared()) return;
        OnGameClear();
    }

    private void OnGameClear()
    {
        Debug.Log("【祝！全レシピ解明！】全てのポーションレシピが完全に特定されました！");

        RecipeDataStore.SetClearedAllRecipes(true);
        SaveAllPagesDataForSceneChange();

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartEndingStory();
        }
    }

    public int GetConfirmedCount()
    {
        int count = 0;
        if (recipePages == null) return count;

        foreach (var page in recipePages)
        {
            if (page.isConfirmed)
            {
                count++;
            }
        }
        return count;
    }

    private void CheckProgressionAfterConfirm()
    {
        if (IsAllRecipesCleared())
        {
            OnGameClear();
            return;
        }

        if (GameFlowManager.Instance != null)
        {
            int confirmedCount = GetConfirmedCount();
            if (GameFlowManager.Instance.TryStartInterludeByCount(confirmedCount))
            {
                SaveAllPagesDataForSceneChange();
            }
        }
    }

    private bool IsAllRecipesCleared()
    {
        foreach (var page in recipePages)
        {
            if (!page.isConfirmed)
            {
                return false;
            }
        }
        return recipePages.Count > 0;
    }

    private void HideDropdownArrows(bool isActive)
    {
        if (rowUI == null) return;

        Transform[] allChildren = rowUI.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.name == "Arrow")
            {
                child.gameObject.SetActive(isActive);
            }
        }
    }








    #region デバッグ用：正解一括入力・確定

    /// <summary>
    /// 現在開いている1ページ分に正解を入力して確定させる（デバッグ用）
    /// </summary>
    public void DebugFillCurrentRecipeAnswer()
    {
        if (recipePages == null || recipePages.Count == 0) return;

        RecipeSlot slot = recipePages[currentPageIndex];
        if (slot == null || slot.targetRecipe == null) return;

        ApplyCorrectAnswerToSlot(slot);

        foreach (var p in recipePages)
        {
            CleanUpConfirmedInputs(p);
        }

        SaveAllPagesDataForSceneChange();

        if (rowUI != null && rowUI.gameObject.activeSelf)
        {
            UpdatePageDisplay();
        }
        else if (tocPanel != null && tocPanel.activeSelf)
        {
            InitializeTableOfContents();
        }

        Debug.Log($"[Debug] レシピ {currentPageIndex + 1}（{slot.targetRecipe.PotionName}）に正解を入力して確定しました。");
        CheckProgressionAfterConfirm();
    }

    /// <summary>
    /// 全レシピページに正解を入力して確定させる（デバッグ用）
    /// </summary>
    public void DebugFillAllRecipeAnswers()
    {
        if (recipePages == null || recipePages.Count == 0) return;

        foreach (var slot in recipePages)
        {
            if (slot == null || slot.targetRecipe == null) continue;
            ApplyCorrectAnswerToSlot(slot);
        }

        foreach (var p in recipePages)
        {
            CleanUpConfirmedInputs(p);
        }

        SaveAllPagesDataForSceneChange();
        UpdateProgressDisplay();

        if (rowUI != null && rowUI.gameObject.activeSelf)
        {
            UpdatePageDisplay();
        }
        else if (tocPanel != null && tocPanel.activeSelf)
        {
            InitializeTableOfContents();
        }

        Debug.Log($"[Debug] 全 {recipePages.Count} 件のレシピに正解を入力して確定しました。");
        CheckProgressionAfterConfirm();
    }

    private void ApplyCorrectAnswerToSlot(RecipeSlot slot)
    {
        RecipeData data = slot.targetRecipe;
        slot.inputA = data.PotionIngredientA;
        slot.inputB = data.PotionIngredientB;
        slot.inputColor = data.PotionColor;
        slot.inputScent = data.PotionScent;
        slot.inputEffect = data.PotionEffectt;
        slot.isConfirmed = true;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        // 必要に応じてショートカットキーからも呼べるようにしておく（例：F9で現在ページ、F10で全ページ）
        if (Input.GetKeyDown(KeyCode.F9))
        {
            DebugFillCurrentRecipeAnswer();
        }
        else if (Input.GetKeyDown(KeyCode.F10))
        {
            DebugFillAllRecipeAnswers();
        }
    }
#endif

    #endregion
}