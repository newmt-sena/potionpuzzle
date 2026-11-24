using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

[CreateAssetMenu(fileName = "NewRecipeData", menuName = "Potion/RecipeData")]
public class RecipeData : ScriptableObject
{
    [Header("参照する選択肢データ（タイポ防止用）")]
    public OptionData optionData;

    //名前、材料1、材料2、色、香り
    public string recipeId;

    [Header("正解データ")]
    [SerializeField] public string potionName;//正解の名前
    [SerializeField] private string potionIngredientA;//正解の材料1
    [SerializeField] private string potionIngredientB;//正解の材料2
    [SerializeField] public string potionColor;//正解の色
    [SerializeField] private string potionScent;//正解の香り
    [SerializeField] private string potionEffect;//正解の効能

    [Header("虫食いヒントメモ")]
    [TextArea(2, 4)]
    [SerializeField] private string effectMemo;

    [Header("虫食いなし、完全版メモ")]
    [TextArea(2, 4)]
    [SerializeField] private string trueEffectMemo;

    [Header("確定後に出るアルカナメモ")]
    [TextArea(2, 4)]
    [SerializeField] private string thoughtsMemo;

    [Header("確定後に出るアルカナ・エーテルメモ(オマケ用)")]
    [TextArea(2, 4)]
    [SerializeField] private string clearThoughtsMemo;

    //↓読み取り専用にしてる、こうすると外から名前を変えられない
    public string PotionName => potionName;
    public string PotionIngredientA => potionIngredientA;
    public string PotionIngredientB => potionIngredientB;
    public string PotionColor => potionColor;
    public string PotionScent => potionScent;
    public string PotionEffectt => potionEffect;


    public string EffectMemo => effectMemo;

    public string TrueEffectMemo => trueEffectMemo;

    public string ThoughtsMemo => thoughtsMemo;

    public string ClearThoughtsMemo => clearThoughtsMemo;

    // Editor拡張から値を安全に書き換えるためのメソッド
    public void SetValues(
        string name, string ingA, string ingB,
        string color, string scent, string effect,
        string effectmemo = "", string thoughts = "",
        string trueEffectmemo = "", string clearThoughts = "")
    {
        potionName = name;
        potionIngredientA = ingA;
        potionIngredientB = ingB;
        potionColor = color;
        potionScent = scent;
        potionEffect = effect;
        if (!string.IsNullOrEmpty(effectmemo)) effectMemo = effectmemo;
        if (!string.IsNullOrEmpty(thoughts)) thoughtsMemo = thoughts;
        if (!string.IsNullOrEmpty(trueEffectmemo)) trueEffectMemo = trueEffectmemo;
        if (!string.IsNullOrEmpty(clearThoughts)) clearThoughtsMemo = clearThoughts;
    }

    public struct DebugResult
    {
        public bool isNameCorrect;       // 名前
        public bool isIngredients; 　　　// 材料ペア（順不同）
        public bool isColorCorrect;      // 色
        public bool isScentCorrect;      // 香り
        public bool isEffectCorrect;     // 効果

        public bool IsAllCorrect => isNameCorrect && isIngredients &&  isColorCorrect && isScentCorrect;
    }

    /// <summary>
    /// 各要素の正誤状況を詳しく取得する（デバッグ用）
    /// </summary>
    public DebugResult GetDebugResult(string inputA, string inputB, string inputColor, string inputScent, string inputEffect)
    {
        DebugResult result = new DebugResult
        {
            isIngredients = MatchesIngredients(inputA, inputB),
            isColorCorrect = (potionColor == inputColor),
            isScentCorrect = (potionScent == inputScent),
            isEffectCorrect = (potionEffect == inputEffect) // ★ 効能の判定を追加
        };

        // IsAllCorrect の判定に isEffectCorrect を含めるように DebugResult 側のプロパティも要調整
        return result;
    }



    
    public bool recipeChecker(
         string inputA, string inputB,
         string inputColor, string inputScent, string inputEffect)
    {
        bool isIngredients = MatchesIngredients(inputA, inputB);
        bool isColor = (inputColor == potionColor);
        bool isScent = (inputScent == potionScent);
        bool isEffect = (inputEffect == potionEffect); // ★ 名前の代わりに効能をチェック

        return isIngredients && isColor && isScent && isEffect;
    }

    /// <summary>
    /// 材料のペアが順不同で一致しているか判定する内部処理
    /// </summary>
    private bool MatchesIngredients(string inputA, string inputB)
    {
        var targetList = new List<string> { potionIngredientA, potionIngredientB };
        var inputList = new List<string> { inputA, inputB };

        // アルファベット/文字順に並び替えて比較する
        targetList.Sort();
        inputList.Sort();

        return (targetList[0] == inputList[0]) && (targetList[1] == inputList[1]);
    }

}
