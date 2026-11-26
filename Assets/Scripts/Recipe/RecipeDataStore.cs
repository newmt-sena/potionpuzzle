using System.Collections.Generic;
using UnityEngine;

public static class RecipeDataStore
{
    private const string ClearedAllKey = "RecipeDataStore_HasClearedAllRecipes";

    public class SavedRecipeData
    {
        public bool isConfirmed;
        public string inputEffect;
        public string inputA;
        public string inputB;
        public string inputColor;
        public string inputScent;
    }

    public static Dictionary<int, SavedRecipeData> SavedPages = new Dictionary<int, SavedRecipeData>();

    /// <summary>
    /// 一度でも全レシピをクリアしたか（永続化）
    /// </summary>
    public static bool HasClearedAllRecipes
    {
        get => PlayerPrefs.GetInt(ClearedAllKey, 0) == 1;
        private set
        {
            PlayerPrefs.SetInt(ClearedAllKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 全クリア状態を更新・保存する
    /// </summary>
    public static void SetClearedAllRecipes(bool cleared = true)
    {
        HasClearedAllRecipes = cleared;
    }

    /// <summary>
    /// ページの入力・確定データを保存（未確定でも呼び出す）
    /// </summary>
    public static void SavePageData(int index, bool isConfirmed, string effect, string ingA, string ingB, string color, string scent)
    {
        SavedPages[index] = new SavedRecipeData
        {
            isConfirmed = isConfirmed,
            inputEffect = effect,
            inputA = ingA,
            inputB = ingB,
            inputColor = color,
            inputScent = scent
        };
    }

    public static SavedRecipeData GetPageData(int index)
    {
        return SavedPages.TryGetValue(index, out var data) ? data : null;
    }

    public static void Clear()
    {
        SavedPages.Clear();
    }


    // ★ 実験ログの1件分データ
    public class MixingLog
    {
        public string ingredientA;
        public string ingredientB;
        public MixingResultDatabase.Temperature temp;
        public string resultColor;
        public string scentComment;
        public string tasteComment;
    }

    // ログ保持用のリスト
    public static List<MixingLog> MixingLogs = new List<MixingLog>();

    public static void AddLog(MixingLog log)
    {
        // リストの先頭（または末尾）に追加
        MixingLogs.Add(log);

        // ★ 5件を超えたら、一番古いログ（最初に入ったもの）を削除する
        if (MixingLogs.Count > 5)
        {
            MixingLogs.RemoveAt(0); // 0番目（最古）を削除
        }
    }

    /// <summary>
    /// すべてのレシピ進捗・調合ログを初期化する（全クリア実績は保持）
    /// </summary>
    public static void ResetAll()
    {
        SavedPages.Clear();
        MixingLogs.Clear();
    }

    /// <summary>
    /// 保存済みの進捗データ（または調合ログ）があるか判定する
    /// </summary>
    public static bool HasSavedProgress()
    {
        return SavedPages.Count > 0 || MixingLogs.Count > 0;
    }


    public static void ClearLogs()
    {
        MixingLogs.Clear();
    }
}