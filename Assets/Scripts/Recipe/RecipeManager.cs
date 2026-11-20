using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI.Table;

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
    [SerializeField] private GameObject tocPanel; // 目次パネル（オブジェクト全体）
    [SerializeField] private Transform tocButtonContainer; // 目次のボタンを並べる親（Content等）
    [SerializeField] private GameObject tocButtonPrefab;   // 目次の1項目分のボタンプレハブ

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
    [SerializeField] private TextMeshProUGUI pageText; // 「1 / 15」などのページ表示用テキスト
    [SerializeField] private GameObject prevButton; // 「前へ」ボタンのGameObject
    [SerializeField] private GameObject nextButton; // 「次へ」ボタンのGameObject

    [Header("全レシピのデータリスト")]
    [SerializeField] private List<RecipeSlot> recipePages = new List<RecipeSlot>();

    private int currentPageIndex = 0;

    // 確定済みの「効能」と「色」を記録する集合（重複なし）
    private HashSet<string> confirmedEffects = new HashSet<string>();
    private HashSet<string> confirmedColors = new HashSet<string>();

    private void Start()
    {
        UpdateProgressDisplay();

        // 参照エラー防止ガード
        if (rowUI == null || optionData == null)
        {
            Debug.LogError("RecipeManager: rowUI または optionData がインスペクターでセットされていません！");
            return;
        }

        // データの復元処理（既存のままでOK）
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

        // ★ ゲーム起動時は必ず「目次」を開いた状態にする
        OpenTableOfContents();

        // コールバック接続
        rowUI.SetOnDropdownChangedCallback(OnDropdownValueChanged);
    }

    /// <summary>
    /// ドロップダウン変更時にコールバックされるメイン処理
    /// </summary>
    private void OnDropdownValueChanged()
    {
        // UI更新中なら何もしない（無限ループ防止）
        if (isUpdatingUI) return;

        SaveCurrentInput();

        RecipeSlot currentPage = recipePages[currentPageIndex];

        // データの保存（inputName の代わりに inputEffect を保存）
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

        // 5つの選択肢がすべて選ばれているかチェックして自動判定
        if (rowUI.IsAllElementsSelected())
        {
            CheckCurrentAnswerAuto();
        }
    }

    /// <summary>
    /// 現在の入力値を保存してから、指定したページを表示に反映する
    /// </summary>
    private void UpdatePageDisplay()
    {
        if (recipePages.Count == 0 || rowUI == null || optionData == null) return;

        // UI更新開始
        isUpdatingUI = true;

        try
        {
            RecipeSlot currentPage = recipePages[currentPageIndex];

            CleanUpConfirmedInputs(currentPage);

            // ★ ポーション名（固定見出し）をUIにセット
            if (currentPage.targetRecipe != null)
            {
                rowUI.SetPotionNameText(currentPage.targetRecipe.PotionName);
                rowUI.SetEffectMemo(currentPage.targetRecipe.EffectMemo);
            }
            else
            {
                rowUI.SetPotionNameText("不明なポーション");
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

            // ★ ここでハンコの表示・非表示を切り替える！
            rowUI.SetStampActive(currentPage.isConfirmed);
            rowUI.SetMemoActive(currentPage.isConfirmed);

            RefreshConfirmedLists();

            // ドロップダウンの初期化（ConfirmedNames の代わりに confirmedEffects を渡す）
            rowUI.InitializeDropdown(
                optionData,
                confirmedEffects,
                confirmedColors,
                currentPage.inputEffect,
                currentPage.inputColor
            );

            // 選択値の反映
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
            // 全UIの更新が終わってからフラグ解除
            isUpdatingUI = false;
        }
    }

    /// <summary>
    /// 最初のページなら「前へ」を非表示、最終ページなら「次へ」を非表示にする
    /// </summary>
    private void UpdateNavigationButtons()
    {
        if (prevButton != null)
        {
            prevButton.SetActive(currentPageIndex > 0);
        }

        if (nextButton != null)
        {
            nextButton.SetActive(currentPageIndex < recipePages.Count - 1);
        }
    }

    /// <summary>
    /// 達成度（解明済み数 / 全レシピ数）を計算してテキストに反映する
    /// </summary>
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

    /// <summary>
    /// 各ページの確定状態をチェックし、確定済みの「効能」と「色」のリストを更新する
    /// </summary>
    private void RefreshConfirmedLists()
    {
        confirmedEffects.Clear();
        confirmedColors.Clear();

        foreach (var page in recipePages)
        {
            if (page.isConfirmed && page.targetRecipe != null)
            {
                // 正解の「効能」と「色」を確定済みリストに追加
                confirmedEffects.Add(page.targetRecipe.PotionEffectt); // ※RecipeData側のプロパティ名に合わせる
                confirmedColors.Add(page.targetRecipe.PotionColor);
            }
        }
    }

    /// <summary>
    /// 現在の画面上のドロップダウン入力値をデータ構造に保存する
    /// </summary>
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

    #region ページ送りUIイベント

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
    }

    #endregion

    /// <summary>
    /// 外部から直接指定ページにジャンプする
    /// </summary>
    public void JumpToPage(int pageIndex)
    {
        currentPageIndex = Mathf.Clamp(pageIndex, 0, recipePages.Count - 1);

        // ★ 1. 目次パネルを非表示にする
        if (tocPanel != null)
        {
            tocPanel.SetActive(false);
        }

        // ★ 2. 詳細画面（rowUI）のゲームオブジェクトを確実に表示する
        if (rowUI != null)
        {
            rowUI.gameObject.SetActive(true);
        }

        // ページの表示内容を更新
        UpdatePageDisplay();
    }

    /// <summary>
    /// 目次用：全レシピスロットのリストを取得する
    /// </summary>
    public List<RecipeSlot> GetRecipePages()
    {
        return recipePages;
    }

    /// <summary>
    /// 目次のボタン一覧を生成する
    /// </summary>
    private void InitializeTableOfContents()
    {
        if (tocButtonContainer == null || tocButtonPrefab == null) return;

        // 既存のボタンを削除
        foreach (Transform child in tocButtonContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < recipePages.Count; i++)
        {
            int index = i; // クロージャ（ローカル変数保持）対策
            GameObject btnObj = Instantiate(tocButtonPrefab, tocButtonContainer);

            // テキストの反映
            var textComp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null && recipePages[i].targetRecipe != null)
            {
                string potionName = recipePages[i].targetRecipe.PotionName;
                string potionId = recipePages[i].targetRecipe.recipeId;
               
                textComp.text = $"{potionId}.{potionName} ";
            }

            bool isConfirmed = recipePages[i].isConfirmed;
            Transform checkIconTrans = btnObj.transform.Find("CheckIcon"); // または GetComponentInChildren を使用
            if (checkIconTrans != null)
            {
                checkIconTrans.gameObject.SetActive(isConfirmed);
            }

            // ボタンのクリックイベント設定
            var button = btnObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners(); // 二重登録防止
                button.onClick.AddListener(() =>
                {
                    JumpToPage(index); // ページ切り替え＆目次非表示を実行
                });
            }
        }
    }


    /// <summary>
    /// 目次を開く（どの画面からでも呼べる）
    /// </summary>
    public void OpenTableOfContents()
    {
        SaveCurrentInput(); // 現在の入力を保存

        // ★ 1. 詳細画面を非表示にする
        if (rowUI != null)
        {
            rowUI.gameObject.SetActive(false);
        }

        // ★ 2. 目次パネルを表示する
        if (tocPanel != null)
        {
            tocPanel.SetActive(true);
            tocPanel.transform.SetAsLastSibling(); // Canvasの一番手前に持ってきて確実に表示
        }

        if (sceneChangeButtonRect != null)
        {
            sceneChangeButtonRect.SetAsLastSibling();
        }

        
        if (optionButtonRect != null)
        {
            optionButtonRect.SetAsLastSibling();
        }
        // 目次ボタンの一覧を最新化（確定状況の反映など）
        InitializeTableOfContents();
    }

    /// <summary>
    /// 自動判定処理（5要素が揃った瞬間に自動呼び出し）
    /// </summary>
    private void CheckCurrentAnswerAuto()
    {
        RecipeSlot currentPage = recipePages[currentPageIndex];

        if (currentPage.targetRecipe == null) return;

        // 正解判定（recipeChecker を使用：引数を効能に変更）
        if (currentPage.targetRecipe.recipeChecker(
            currentPage.inputA,
            currentPage.inputB,
            currentPage.inputColor,
            currentPage.inputScent,
            currentPage.inputEffect))
        {
            // 正解処理
            currentPage.isConfirmed = true;
            rowUI.SetDropdownSprites(true);

            foreach (var p in recipePages)
            {
                CleanUpConfirmedInputs(p);
            }

            // データの保存
            RecipeDataStore.SavePageData(
                currentPageIndex,
                currentPage.isConfirmed,
                currentPage.inputEffect,
                currentPage.inputA,
                currentPage.inputB,
                currentPage.inputColor,
                currentPage.inputScent
            );

            // 画面の更新
            UpdatePageDisplay();

            Debug.Log($"【確定！】レシピ {currentPageIndex + 1} の効能が一致したためロックされました！");

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

        // 正解チェック
        if (currentPage.targetRecipe.recipeChecker(
            currentPage.inputA,
            currentPage.inputB,
            currentPage.inputColor,
            currentPage.inputScent,
            currentPage.inputEffect))
        {
            currentPage.isConfirmed = true;
            rowUI.SetInteractable(false); // UIをロック
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
        else
        {
            Debug.Log($"【不正解】レシピ {currentPageIndex + 1} はまだ一致していません。");

            // デバッグ出力
            RecipeData.DebugResult result = currentPage.targetRecipe.GetDebugResult(
                currentPage.inputA,
                currentPage.inputB,
                currentPage.inputColor,
                currentPage.inputScent,
                currentPage.inputEffect);

            Debug.Log($"--- レシピ[{currentPageIndex + 1}] 判定結果 ---");
            Debug.Log($"効能（{currentPage.inputEffect}）: {(result.isEffectCorrect ? "正解" : "不正解")}");
            Debug.Log($"材料（{currentPage.inputA} + {currentPage.inputB}）: {(result.isIngredients ? "正解" : "不正解")}");
            Debug.Log($"色（{currentPage.inputColor}）: {(result.isColorCorrect ? "正解" : "不正解")}");
            Debug.Log($"香り（{currentPage.inputScent}）: {(result.isScentCorrect ? "正解" : "不正解")}");
        }
    }

    /// <summary>
    /// すでに他のページで確定している正解の要素が、
    /// 未確定ページの入力欄に残っていれば「？？？」にリセットする
    /// </summary>
    private void CleanUpConfirmedInputs(RecipeSlot page)
    {
        // 確定済みのページは変更しない
        if (page.isConfirmed || page.targetRecipe == null) return;

        // 全ページの確定データから「すでに使われている確定要素」を集める
                
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

        // 入力値が他の場所で確定済みなら "？？？" にリセット
        if (confirmedEffectsSet.Contains(page.inputEffect)) page.inputEffect = "？？？";
        if (confirmedColorsSet.Contains(page.inputColor)) page.inputColor = "？？？";
        
    }

    /// <summary>
    /// シーン遷移ボタンなどを押した際に明示的に呼び出す保存処理
    /// </summary>
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
        foreach (var page in recipePages)
        {
            if (!page.isConfirmed)
            {
                return;
            }
        }

        OnGameClear();
    }

    private void OnGameClear()
    {
        Debug.Log("【祝！全レシピ解明！】全てのポーションレシピが完全に特定されました！");

        // 念のため全ページの確定状態を保存しておく
        SaveAllPagesDataForSceneChange();

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartEndingStory();
        }
    }

    /// <summary>
    /// 現在の確定済みレシピ数を返す
    /// </summary>
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

    /// <summary>
    /// レシピ確定後の進行チェック（全クリア → 幕間の順で判定）
    /// </summary>
    private void CheckProgressionAfterConfirm()
    {
        // すべて埋まっているなら全クリア処理へ
        if (IsAllRecipesCleared())
        {
            OnGameClear();
            return;
        }

        // 一定数に達していて未視聴なら幕間会話へ遷移
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

    /// <summary>
    /// rowUI 内にあるすべてのドロップダウンから「Arrow（矢印）」を探して非表示にする
    /// </summary>
    private void HideDropdownArrows(bool isActive)
    {
        if (rowUI == null) return;

        // rowUI の中（子孫要素すべて）から "Arrow" という名前のTransformをすべて探す
        Transform[] allChildren = rowUI.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.name == "Arrow")
            {
                child.gameObject.SetActive(isActive);
            }
        }
    }
}