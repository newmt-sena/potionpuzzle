using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MixingLogWindow : MonoBehaviour
{
    [SerializeField] private Transform contentContainer;
    [SerializeField] private GameObject logItemPrefab;

    [SerializeField] private TextMeshProUGUI buttonText;


    public void RefreshLogList()
    {
        if (contentContainer == null || logItemPrefab == null) return;

        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        var logs = RecipeDataStore.MixingLogs;
        for (int i = logs.Count - 1; i >= 0; i--)
        {
            var log = logs[i];
            GameObject itemObj = Instantiate(logItemPrefab, contentContainer);
            var itemUI = itemObj.GetComponent<MixingLogItemUI>();
            if (itemUI != null)
            {
                itemUI.Setup(log); // シンプルに log だけを渡す！
            }
        }
    }

    public void OpenWindow()
    {
        gameObject.SetActive(true);
        RefreshLogList();

        if (buttonText != null)
        {
            buttonText.text = "ログを閉じる"; // または 「閉じる」など
        }
    }

    public void CloseWindow()
    {
        gameObject.SetActive(false);

        if (buttonText != null)
        {
            buttonText.text = "実験ログ"; // または 「ログを開く」など
        }
    }

    public void ToggleWindow()
    {
        if (gameObject.activeSelf) CloseWindow();
        else OpenWindow();
    }
}