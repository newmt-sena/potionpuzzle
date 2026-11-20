using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MixingLogItemUI;

public class MixingLogItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI TemplogText;
    [SerializeField] private TextMeshProUGUI ItemAlogText;
    [SerializeField] private TextMeshProUGUI ItemBlogText;
    [SerializeField] private TextMeshProUGUI logText;

    [Serializable]
    public struct LogItemIcon
    {
        public string ingredientName; // 素材名（例：「マンドラゴラ」）
        public Sprite iconSprite;     // ログ専用の画像
    }// 切り替える画像を格納する配列

    [SerializeField] private Image targetImageA; // UIのImageを使う場合
    [SerializeField] private Image targetImageB;

    [SerializeField] private List<LogItemIcon> logItemIcons = new List<LogItemIcon>();

    private RecipeDataStore.MixingLog currentLog;

    public void Setup(RecipeDataStore.MixingLog log)
    {
        currentLog = log;

        if (logText != null)
        {
            string tempStr = log.temp switch
            {
                MixingResultDatabase.Temperature.Cold => "<color=#639bff>【冷却】</color>",
                MixingResultDatabase.Temperature.Hot => "<color=#ac3232>【加熱】</color>",
                _ => "<color=#df7126>【常温】</color>"

            };

            TemplogText.text = tempStr;
            ItemAlogText.text = log.ingredientA;
            ItemBlogText.text = log.ingredientB;
        

            logText.text = $"<color=#6B4A2B><b>観察：</b></color><indent=140>{log.scentComment}</indent>\n" +
                $"<color=#6B4A2B><b>試飲：</b></color><indent=140>{log.tasteComment}</indent>";
        }

        if (targetImageA != null)
        {
            targetImageA.sprite = GetIngredientSprite(log.ingredientA);
            targetImageA.gameObject.SetActive(targetImageA.sprite != null); // 画像がなければ非表示にするなど
        }

        if (targetImageB != null)
        {
            targetImageB.sprite = GetIngredientSprite(log.ingredientB);
            targetImageB.gameObject.SetActive(targetImageB.sprite != null);
        }

        // ★ Prefab自身についている Button コンポーネントを自動取得してクリックイベントを登録
        Button itemButton = GetComponent<Button>();
        if (itemButton != null)
        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(() =>
            {
                // シーン内の MixingManager を探して条件を復元
                MixingManager manager = FindObjectOfType<MixingManager>();
                if (manager != null)
                {
                    manager.ReuseLogSettings(currentLog);
                }

                // ログウィンドウが開いていれば閉じる
                MixingLogWindow window = GetComponentInParent<MixingLogWindow>();
                if (window != null)
                {
                    window.CloseWindow();
                }
            });
        }
    }

    public Sprite GetIngredientSprite(string name)
    {
        foreach (var item in logItemIcons)
        {
            if (item.ingredientName == name)
            {
                return item.iconSprite;
            }
        }
        return null; // 見つからない場合は null
               
    }
    
}

