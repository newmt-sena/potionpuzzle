#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(RecipeData))]
public class RecipeDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        RecipeData recipe = (RecipeData)target;

        serializedObject.Update();

        // ID、OptionData、メモ書きを表示
        EditorGUILayout.PropertyField(serializedObject.FindProperty("recipeId"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("optionData"));

        // 固定表示するポーション名（インスペクターで直接テキスト入力）
        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("potionName"), new GUIContent("ポーションの名称"));

        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("effectMemo"), new GUIContent("効能・ヒントメモ"));

        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("thoughtsMemo"), new GUIContent("確定後メモ"));

        if (recipe.optionData == null)
        {
            EditorGUILayout.HelpBox("OptionData を割り当ててください。選択肢ドロップダウンが表示されます。", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("【正解データ（選択式）】", EditorStyles.boldLabel);

        OptionData options = recipe.optionData;

        // EffectOptions を取得・ポップアップ表示する（※プロパティ名は RecipeData.cs の定義に合わせて調整してください）
        int indexEffect = GetIndex(options.EffectOptions, recipe.PotionEffectt);
        int indexIngA = GetIndex(options.IngredientOptions, recipe.PotionIngredientA);
        int indexIngB = GetIndex(options.IngredientOptions, recipe.PotionIngredientB);
        int indexColor = GetIndex(options.ColorOptions, recipe.PotionColor);
        int indexScent = GetIndex(options.ScentOptions, recipe.PotionScent);

        EditorGUI.BeginChangeCheck();

        indexIngA = EditorGUILayout.Popup("正解の材料1", indexIngA, options.IngredientOptions.ToArray());
        indexIngB = EditorGUILayout.Popup("正解の材料2", indexIngB, options.IngredientOptions.ToArray());
        indexScent = EditorGUILayout.Popup("正解の香り", indexScent, options.ScentOptions.ToArray());
        indexColor = EditorGUILayout.Popup("正解の色", indexColor, options.ColorOptions.ToArray());
        indexEffect = EditorGUILayout.Popup("正解の効能", indexEffect, options.EffectOptions.ToArray());

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(recipe, "Change Recipe Data");

            string selectedEffect = GetValue(options.EffectOptions, indexEffect);
            string selectedIngA = GetValue(options.IngredientOptions, indexIngA);
            string selectedIngB = GetValue(options.IngredientOptions, indexIngB);
            string selectedColor = GetValue(options.ColorOptions, indexColor);
            string selectedScent = GetValue(options.ScentOptions, indexScent);

            // SetValues の定義順（name, ingA, ingB, color, scent, effect）に合致させる
            recipe.SetValues(
                recipe.potionName,
                selectedIngA,
                selectedIngB,
                selectedColor,
                selectedScent,
                selectedEffect
            );

            EditorUtility.SetDirty(recipe);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private int GetIndex(List<string> list, string currentVal)
    {
        if (list == null || string.IsNullOrEmpty(currentVal)) return 0;
        int idx = list.IndexOf(currentVal);
        return idx >= 0 ? idx : 0;
    }

    private string GetValue(List<string> list, int index)
    {
        if (list == null || index < 0 || index >= list.Count) return "";
        return list[index];
    }
}
#endif