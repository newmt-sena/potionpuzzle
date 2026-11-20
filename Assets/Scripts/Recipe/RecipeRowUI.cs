using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class RecipeRowUI : MonoBehaviour
{
    [Header("ヒント・メモ書きUI")]
    [SerializeField] private TextMeshProUGUI effectMemoText; // 効能メモ表示用テキスト
    [SerializeField] private Image effectMemoImage; // 効能メモ表示用テキスト

    [Header("確定後に出るメモ書きUI")]
    [SerializeField] private TextMeshProUGUI thoughtsMemoText; // 確定後メモ表示用テキスト
    [SerializeField] private TextMeshProUGUI nameText; // 【固定表示】ポーション名用テキスト
    [SerializeField] private Image stampImage;

    [Header("UIドロップダウン")]
    [SerializeField] private TMP_Dropdown ingredientADropdown;
    [SerializeField] private TMP_Dropdown ingredientBDropdown;
    [SerializeField] private TMP_Dropdown colorDropdown;
    [SerializeField] private TMP_Dropdown scentDropdown;
    [SerializeField] private TMP_Dropdown effectDropdown; // ★ 効能選択用ドロップダウン

    [Header("スプライト切り替え設定")]
    [SerializeField] private Sprite normalDropdownSprite;   // 通常時のスプライト
    [SerializeField] private Sprite confirmedDropdownSprite; // 確定時のスプライト

    // ドロップダウンが変更されたことをマネージャーに知らせるアクション
    private Action onDropdownChangedCallback;

    private bool isInitializing = false; // 初期化中の自動判定発火を防ぐフラグ

    private void Awake()
    {
        ingredientADropdown?.onValueChanged.AddListener(_ => OnDropdownChanged());
        ingredientBDropdown?.onValueChanged.AddListener(_ => OnDropdownChanged());
        colorDropdown?.onValueChanged.AddListener(_ => OnDropdownChanged());
        scentDropdown?.onValueChanged.AddListener(_ => OnDropdownChanged());
        effectDropdown?.onValueChanged.AddListener(_ => OnDropdownChanged()); // ★ 効能の変更監視
    }

    /// <summary>
    /// マネージャー側からの変更通知イベント登録
    /// </summary>
    public void SetOnDropdownChangedCallback(Action callback)
    {
        onDropdownChangedCallback = callback;
    }

    private void OnDropdownChanged()
    {
        if (isInitializing) return;
        onDropdownChangedCallback?.Invoke();
    }

    /// <summary>
    /// メモ書きテキストを表示する
    /// </summary>
    public void SetEffectMemo(string memo)
    {
        if (effectMemoText != null)
        {
            effectMemoText.text = memo;
        }
    }

    public void SetThoughtsMemo(string memo)
    {
        if (thoughtsMemoText != null)
        {
            thoughtsMemoText.text = string.IsNullOrEmpty(memo) ? "" : memo;
        }
    }

    /// <summary>
    /// ポーション名（固定見出し）を表示する
    /// </summary>
    public void SetPotionNameText(string potionName)
    {
        if (nameText != null)
        {
            nameText.text = potionName;
        }
    }

    /// <summary>
    /// OptionDataからドロップダウンの選択肢を初期化設定する（名前の代わりに「効能」を重複除外対象にする）
    /// </summary>
    public void InitializeDropdown(OptionData options, HashSet<string> usedEffects, HashSet<string> usedColors, string currentEffect, string currentColor)
    {
        if (options == null) return;

        isInitializing = true;

        // 1. 重複除外フィルタリング（効能・色）
        List<string> filteredEffects = FilterOptions(options.EffectOptions, usedEffects, currentEffect);
        List<string> filteredColors = FilterOptions(options.ColorOptions, usedColors, currentColor);

        // 2. 効能と色のオプション再設定
        SetDropdownOptions(effectDropdown, filteredEffects);
        SetDropdownOptions(colorDropdown, filteredColors);

        // 3. 材料（A/B）と香りのオプション設定（重複OKのため全リストをそのまま渡す）
        SetDropdownOptions(ingredientADropdown, options.IngredientOptions);
        SetDropdownOptions(ingredientBDropdown, options.IngredientOptions);
        SetDropdownOptions(scentDropdown, options.ScentOptions);

        isInitializing = false;
    }

    /// <summary>
    /// ハンコの表示・非表示を切り替える
    /// </summary>
    public void SetStampActive(bool isActive)
    {
        if (stampImage != null)
        {
            stampImage.gameObject.SetActive(isActive);
        }
    }

    /// <summary>
    /// メモの表示・非表示を切り替える
    /// </summary>
    public void SetMemoActive(bool isActive)
    {
        if (effectMemoImage != null)
        {
            effectMemoImage.gameObject.SetActive(isActive);
        }
    }

    /// <summary>
    /// 確定済みリストに含まれる選択肢を除外したリストを生成する内部処理
    /// </summary>
    private List<string> FilterOptions(List<string> originalList, HashSet<string> usedItems, string currentSelfValue)
    {
        List<string> result = new List<string>();
        if (originalList == null) return result;

        foreach (var item in originalList)
        {
            // 未使用のもの、または現在自分が選択中の値なら追加
            if (!usedItems.Contains(item) || item == currentSelfValue)
            {
                result.Add(item);
            }
        }
        return result;
    }

    public void SetDropdownOptions(TMP_Dropdown dropdown, List<string> optionList)
    {
        if (dropdown == null) return;

        dropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (var opt in optionList)
        {
            options.Add(new TMP_Dropdown.OptionData(opt));
        }

        dropdown.AddOptions(options);
    }

    // 各値の取得メソッド（Name の代わりに Effect を取得）
    public string GetSelectedEffect() => GetText(effectDropdown);
    public string GetSelectedIngredientA() => GetText(ingredientADropdown);
    public string GetSelectedIngredientB() => GetText(ingredientBDropdown);
    public string GetSelectedColor() => GetText(colorDropdown);
    public string GetSelectedScent() => GetText(scentDropdown);
  
    /// <summary>
    /// 現在ドロップダウンで選択されているテキストを取得
    /// </summary>
    private string GetText(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return string.Empty;
        return dropdown.options[dropdown.value].text;
    }

    /// <summary>
    /// 5つの要素がすべて未選択（？？？）以外に選ばれているかチェック
    /// </summary>
    public bool IsAllElementsSelected()
    {
        return IsValidSelection(effectDropdown) &&
               IsValidSelection(ingredientADropdown) &&
               IsValidSelection(ingredientBDropdown) &&
               IsValidSelection(colorDropdown) &&
               IsValidSelection(scentDropdown);
    }

    private bool IsValidSelection(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return false;
        string text = GetText(dropdown);
        return !string.IsNullOrEmpty(text) && text != "？？？" && text != "選択してください";
    }

    /// <summary>
    /// 指定したテキストに一致するドロップダウンを選択状態にする（ページ復元用）
    /// </summary>
    public void SetSelectedValues(string effectVal, string aVal, string bVal, string colorVal, string scentVal)
    {
        isInitializing = true;
        SetDropdownValueWithoutNotify(effectDropdown, effectVal);
        SetDropdownValueWithoutNotify(ingredientADropdown, aVal);
        SetDropdownValueWithoutNotify(ingredientBDropdown, bVal);
        SetDropdownValueWithoutNotify(colorDropdown, colorVal);
        SetDropdownValueWithoutNotify(scentDropdown, scentVal);
        isInitializing = false;
    }

    private void SetDropdownValueWithoutNotify(TMP_Dropdown dropdown, string valueText)
    {
        if (dropdown == null) return;

        int index = dropdown.options.FindIndex(option => option.text == valueText);
        if (index >= 0)
        {
            dropdown.SetValueWithoutNotify(index);
        }
        else
        {
            dropdown.SetValueWithoutNotify(0); // 見つからない場合はデフォルト
        }
    }

    //<summary>
    /// rowUI 内にあるすべてのドロップダウンの背景スプライトを一括で切り替える
    /// </summary>
    public void SetDropdownSprites(bool isConfirmed)
    {
        Sprite targetSprite = isConfirmed ? confirmedDropdownSprite : normalDropdownSprite;
        if (targetSprite == null) return;

        // rowUI 内にあるすべての Dropdown または TMP_Dropdown を探す
        var dropdowns = GetComponentsInChildren<TMP_Dropdown>(true);
        foreach (var dd in dropdowns)
        {
            Image bgImage = dd.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.sprite = targetSprite;
            }
        }

        // ※もし通常の UnityEngine.UI.Dropdown を使っている場合はこちらのコメントを外してください
        /*
        var legacyDropdowns = GetComponentsInChildren<Dropdown>(true);
        foreach (var dd in legacyDropdowns)
        {
            Image bgImage = dd.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.sprite = targetSprite;
            }
        }
        */
    }

    /// <summary>
    /// ドロップダウンの操作可否（確定後のロック用）
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (effectDropdown != null) effectDropdown.interactable = interactable;
        if (ingredientADropdown != null) ingredientADropdown.interactable = interactable;
        if (ingredientBDropdown != null) ingredientBDropdown.interactable = interactable;
        if (colorDropdown != null) colorDropdown.interactable = interactable;
        if (scentDropdown != null) scentDropdown.interactable = interactable;
    }
}