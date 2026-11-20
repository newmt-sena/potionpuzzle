using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngredientButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;

    public string IngredientName { get; private set; }
    private Action<string> onClickCallback;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
    }

    public void Setup(string name, Sprite icon, Action<string> onClick)
    {
        IngredientName = name;
        onClickCallback = onClick;

        if (nameText != null)
        {
            nameText.text = name;
        }

        // ★ アイコン画像の設定
        if (iconImage != null)
        {
            if (icon != null)
            {
                iconImage.gameObject.SetActive(true);
                iconImage.sprite = icon;
            }
            else
            {
                // アイコンが設定されていない場合は非表示にする（またはデフォルト画像にする）
                iconImage.gameObject.SetActive(false);
            }
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback?.Invoke(IngredientName));

        //SetSelectedHighlight(false);
    }

    /// <summary>
    /// 選択中のハイライト演出切り替え
    /// </summary>
//    public void SetSelectedHighlight(bool isSelected, int selectedCount = 1)
//    {
//        if (highlightImage != null)
//        {
//            highlightImage.enabled = isSelected;
//        }

//        // ※もしボタン上に「x2」などのカウントバッジをつけたい場合は、以下のようにText等に反映できます
//        // if (countText != null) countText.text = selectedCount > 1 ? $"x{selectedCount}" : "";
//    }
}