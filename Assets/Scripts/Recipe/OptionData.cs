using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewOptionData", menuName = "Potion/OptionData")]
public class OptionData : ScriptableObject
{
    [System.Serializable]
    public class IngredientItem
    {
        public string ingredientName; // 素材名 (例: "赤ハーブ")
        public Sprite ingredientIcon; // アイコン画像
    }


    [Header("効能の選択肢リスト")]
    [SerializeField]
    private List<string> effectOptions = new List<string>
    {"？？？", "体力回復", "魔力回復", "疲労回復", "解毒"}; // ※ゲームの仕様に合わせて項目は書き換えてください

    [Header("材料の選択肢リスト（画像付き）")]
    [SerializeField]
    private List<IngredientItem> ingredientItems = new List<IngredientItem>
    {
        // ★ ここに最初から「？？？」の項目が入っているか確認！
        new IngredientItem { ingredientName = "？？？", ingredientIcon = null },
        new IngredientItem { ingredientName = "赤ハーブ", ingredientIcon = null },
        new IngredientItem { ingredientName = "青ハーブ", ingredientIcon  = null}
    };

    [Header("色の選択肢リスト")]
    [SerializeField]
    private List<string> colorOptions = new List<string>
    {"？？？","赤色","青色","緑色","黄色"};

    [Header("香りの選択肢リスト")]
    [SerializeField]
    private List<string> scentOptions = new List<string>
    {"？？？","甘い香り","刺激臭","無臭","薬草臭"};

    // 読み取り専用プロパティ
    public List<string> EffectOptions => effectOptions;
    public List<IngredientItem> IngredientItems => ingredientItems;
    public List<string> ColorOptions => colorOptions;
    public List<string> ScentOptions => scentOptions;

    // 互換性維持用（ほかのスクリプトで IngredientOptions を string のリストとして参照している場合の救済）
    public List<string> IngredientOptions
    {
        get
        {
            List<string> list = new List<string>();
            foreach (var item in ingredientItems)
            {
                list.Add(item.ingredientName);
            }
            return list;
        }
    }

    /// <summary>
    /// 指定した素材名に対応するアイコンを取得する
    /// </summary>
    public Sprite GetIngredientIcon(string name)
    {
        var found = ingredientItems.Find(x => x.ingredientName == name);
        return found != null ? found.ingredientIcon : null;
    }
}