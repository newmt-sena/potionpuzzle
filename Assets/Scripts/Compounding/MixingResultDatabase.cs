using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMixingResultDatabase", menuName = "Potion/MixingResultDatabase")]
public class MixingResultDatabase : ScriptableObject
{
    //タイポ防止用：インスペクターで OptionData をセットできるようにする
    public OptionData optionData;

    // ★ 温度の定義
    public enum Temperature
    {
        Cold,   // 冷却
        Normal, // 常温
        Hot     // 加熱
    }

    [System.Serializable]
    public class MixRecipe
    {
        public string ingredientA;
        public string ingredientB;
        public Temperature targetTemp = Temperature.Normal; // ★ 指定温度（デフォルトは常温）

        [Header("生成される結果データ")]
        public string colorName;
        public Color resultColor;       // 例: "鮮やかな赤色"

        [TextArea(2, 3)]
        public string scentComment;

        [TextArea(2, 3)]
        public string tasteComment;     // 例: "助手「舌がピリピリと麻痺するような味がします！」"
    }

    [SerializeField] private List<MixRecipe> recipes = new List<MixRecipe>();

    /// <summary>
    /// 2つの素材から調合結果を取得する（順不同）
    /// </summary>
    public MixRecipe GetResult(string inputA, string inputB, Temperature temp)
    {
        foreach (var recipe in recipes)
        {
            if (Matches(recipe.ingredientA, recipe.ingredientB, inputA, inputB) && recipe.targetTemp == temp)
            {
                return recipe;
            }
           
        }

        // 該当なし（失敗時）のメッセージも温度に応じて変えると面白い
        //string defaultScent = temp == Temperature.Hot ? "助手「焦げ臭いにおいがします…失敗ですね。」" : "助手「特に変化は見られません。」";

        // 定義されていない組み合わせの場合のデフォルトレスポンス
        return new MixRecipe
        {
            ingredientA = inputA,
            ingredientB = inputB,
            targetTemp = temp,
            resultColor = Color.gray,
            scentComment = "『特に変化が見られませんね…\nもしかしてこれ失敗じゃないですか？』"
        };
    }

    private bool Matches(string a1, string b1, string a2, string b2)
    {
        return (a1 == a2 && b1 == b2) || (a1 == b2 && b1 == a2);
    }
}