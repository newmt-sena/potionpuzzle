using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BonusSceneController : MonoBehaviour
{
    [Header("シーン遷移設定")]
    [SerializeField] private string titleSceneName = "TitleScene";

    [Header("ポーションページ上に重ねるパネル")]
    [SerializeField] private GameObject tocPanel;
    [SerializeField] private Transform tocButtonContainer;
    [SerializeField] private GameObject tocButtonPrefab;

    [Header("見開き詳細表示（左メモ＆右5項目）")]
    [SerializeField] private TextMeshProUGUI pageText;
    [SerializeField] private TextMeshProUGUI potionNameText;

    // 左ページ：虫食いなし完全版メモ＆アルカナ・エーテル解説メモ
    [SerializeField] private TextMeshProUGUI trueEffectMemoText;
    [SerializeField] private TextMeshProUGUI clearThoughtsMemoText;

    // 右ページ：ドロップダウンの代わりに表示する5項目
    [SerializeField] private TextMeshProUGUI ingredientAText;
    [SerializeField] private TextMeshProUGUI ingredientBText;
    [SerializeField] private TextMeshProUGUI colorText;
    [SerializeField] private TextMeshProUGUI scentText;
    [SerializeField] private TextMeshProUGUI effectText;

    [Header("ページ送りボタン")]
    [SerializeField] private GameObject prevButton;
    [SerializeField] private GameObject nextButton;

    [Header("全レシピデータ（21種）")]
    [SerializeField] private List<RecipeData> recipes = new List<RecipeData>();

    private int currentRecipeIndex = 0;

    private void Start()
    {
        Time.timeScale = 1f;
        BuildTableOfContents();
        OpenTableOfContents();
    }

    /// <summary>
    /// ポーションページの上に目次パネルを重ねて表示
    /// </summary>
    public void OpenTableOfContents()
    {
        if (tocPanel != null)
        {
            tocPanel.SetActive(true);
            tocPanel.transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// 目次パネルを閉じて指定のポーションページを表示
    /// </summary>
    public void JumpToRecipe(int index)
    {
        if (recipes == null || recipes.Count == 0) return;

        currentRecipeIndex = Mathf.Clamp(index, 0, recipes.Count - 1);

        if (tocPanel != null)
        {
            tocPanel.SetActive(false);
        }

        UpdateRecipeDetailUI();
    }

    public void NextPage()
    {
        if (recipes == null || recipes.Count == 0) return;

        if (currentRecipeIndex < recipes.Count - 1)
        {
            currentRecipeIndex++;
            UpdateRecipeDetailUI();
        }
    }

    public void PrevPage()
    {
        if (recipes == null || recipes.Count == 0) return;

        if (currentRecipeIndex > 0)
        {
            currentRecipeIndex--;
            UpdateRecipeDetailUI();
        }
        else
        {
            // No.1 から「前へ」を押したら目次へ戻る
            OpenTableOfContents();
        }
    }

    private void UpdateRecipeDetailUI()
    {
        RecipeData data = recipes[currentRecipeIndex];
        if (data == null) return;

        if (potionNameText != null)
        {
            potionNameText.text = $"No.{currentRecipeIndex + 1}. {data.PotionName}";
        }

        // 左ページ：虫食いなし完全版メモ
        if (trueEffectMemoText != null)
        {
            trueEffectMemoText.text = string.IsNullOrEmpty(data.TrueEffectMemo)
                ? data.EffectMemo
                : data.TrueEffectMemo;
        }

        // 左ページ：アルカナ・エーテル解説メモ
        if (clearThoughtsMemoText != null)
        {
            clearThoughtsMemoText.text = string.IsNullOrEmpty(data.ClearThoughtsMemo)
                ? data.ThoughtsMemo
                : data.ClearThoughtsMemo;
        }

        // 右ページ：材料1・材料2・色・香り・効能
        if (ingredientAText != null) ingredientAText.text = data.PotionIngredientA;
        if (ingredientBText != null) ingredientBText.text = data.PotionIngredientB;
        if (colorText != null) colorText.text = data.PotionColor;
        if (scentText != null) scentText.text = data.PotionScent;
        if (effectText != null) effectText.text = data.PotionEffectt;

        if (pageText != null)
        {
            pageText.text = $"{currentRecipeIndex + 1} / {recipes.Count}";
        }

        if (prevButton != null) prevButton.SetActive(true);
        if (nextButton != null) nextButton.SetActive(currentRecipeIndex < recipes.Count - 1);
    }

    private void BuildTableOfContents()
    {
        if (tocButtonContainer == null || tocButtonPrefab == null || recipes == null) return;

        foreach (Transform child in tocButtonContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < recipes.Count; i++)
        {
            int capturedIndex = i;
            GameObject btnObj = Instantiate(tocButtonPrefab, tocButtonContainer);
            var textComp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null && recipes[i] != null)
            {
                textComp.text = $"No.{i + 1}. {recipes[i].PotionName}";
            }

            var btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => JumpToRecipe(capturedIndex));
            }
        }
    }

    public void ReturnToTitle()
    {
        Time.timeScale = 1f;
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ReturnToTitle();
        }
        else
        {
            SceneManager.LoadScene(titleSceneName);
        }
    }
}