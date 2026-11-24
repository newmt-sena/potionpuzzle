using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeDatabase", menuName = "Potion/RecipeDatabase")]
public class RecipeDatabase : ScriptableObject
{
    [Header("参照する選択肢データ（タイポ防止用）")]
    public OptionData optionData;

    [Header("全レシピ一覧")]
    public List<RecipeEntry> recipes = new List<RecipeEntry>();

    [Serializable]
    public class RecipeEntry
    {
        public string recipeId;

        [Header("正解データ")]
        [SerializeField] private string potionName;        // 確定後に表示する名前
        [SerializeField] private string potionIngredientA; // 正解の材料1
        [SerializeField] private string potionIngredientB; // 正解の材料2
        [SerializeField] private string potionColor;       // 正解の色
        [SerializeField] private string potionScent;       // 正解の香り
        [SerializeField] private string potionEffect;      // 正解の効能

        [Header("レシピに残っているヒントメモ（虫食い仕様）")]
        [TextArea(2, 4)]
        [SerializeField] private string effectMemo;

        [Header("確定後に出るメモ")]
        [TextArea(2, 4)]
        [SerializeField] private string thoughtsMemo;

        [Header("全クリア報酬（オマケ用）")]
        [TextArea(2, 4)]
        [SerializeField] private string effectMemoFull; // 虫食いナシ全文
        [TextArea(2, 4)]
        [SerializeField] private string arcanaComment;   // アルカナの解説
        [TextArea(2, 4)]
        [SerializeField] private string etherComment;    // エーテルの解説

        public string PotionName => potionName;
        public string PotionIngredientA => potionIngredientA;
        public string PotionIngredientB => potionIngredientB;
        public string PotionColor => potionColor;
        public string PotionScent => potionScent;
        public string PotionEffectt => potionEffect;

        public string EffectMemo => effectMemo;
        public string ThoughtsMemo => thoughtsMemo;
        public string EffectMemoFull => effectMemoFull;
        public string ArcanaComment => arcanaComment;
        public string EtherComment => etherComment;

        public void SetValues(
            string name, string ingA, string ingB,
            string color, string scent, string effect,
            string effectmemo = "", string thoughts = "",
            string fullMemo = "", string arcana = "", string ether = "")
        {
            potionName = name;
            potionIngredientA = ingA;
            potionIngredientB = ingB;
            potionColor = color;
            potionScent = scent;
            potionEffect = effect;

            if (!string.IsNullOrEmpty(effectmemo)) effectMemo = effectmemo;
            if (!string.IsNullOrEmpty(thoughts)) thoughtsMemo = thoughts;
            if (!string.IsNullOrEmpty(fullMemo)) effectMemoFull = fullMemo;
            if (!string.IsNullOrEmpty(arcana)) arcanaComment = arcana;
            if (!string.IsNullOrEmpty(ether)) etherComment = ether;
        }

        public struct DebugResult
        {
            public bool isNameCorrect;
            public bool isIngredients;
            public bool isColorCorrect;
            public bool isScentCorrect;
            public bool isEffectCorrect;

            public bool IsAllCorrect =>
                isNameCorrect && isIngredients && isColorCorrect && isScentCorrect && isEffectCorrect;
        }

        public DebugResult GetDebugResult(
            string inputA, string inputB,
            string inputColor, string inputScent, string inputEffect)
        {
            return new DebugResult
            {
                isIngredients = MatchesIngredients(inputA, inputB),
                isColorCorrect = (potionColor == inputColor),
                isScentCorrect = (potionScent == inputScent),
                isEffectCorrect = (potionEffect == inputEffect)
            };
        }

        public bool recipeChecker(
            string inputA, string inputB,
            string inputColor, string inputScent, string inputEffect)
        {
            bool isIngredients = MatchesIngredients(inputA, inputB);
            bool isColor = (inputColor == potionColor);
            bool isScent = (inputScent == potionScent);
            bool isEffect = (inputEffect == potionEffect);

            return isIngredients && isColor && isScent && isEffect;
        }

        private bool MatchesIngredients(string inputA, string inputB)
        {
            var targetList = new List<string> { potionIngredientA, potionIngredientB };
            var inputList = new List<string> { inputA, inputB };

            targetList.Sort();
            inputList.Sort();

            return (targetList[0] == inputList[0]) && (targetList[1] == inputList[1]);
        }
    }
}