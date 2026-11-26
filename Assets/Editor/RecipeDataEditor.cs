#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(RecipeData))]
public class RecipeDataEditor : Editor
{
    private SerializedProperty recipeIdProp;
    private SerializedProperty optionDataProp;
    private SerializedProperty potionNameProp;
    private SerializedProperty effectMemoProp;
    private SerializedProperty trueEffectMemoProp;
    private SerializedProperty thoughtsMemoProp;
    private SerializedProperty clearThoughtsMemoProp;

    private void OnEnable()
    {
        recipeIdProp = serializedObject.FindProperty("recipeId");
        optionDataProp = serializedObject.FindProperty("optionData");
        potionNameProp = serializedObject.FindProperty("potionName");
        effectMemoProp = serializedObject.FindProperty("effectMemo");
        trueEffectMemoProp = serializedObject.FindProperty("trueEffectMemo");
        thoughtsMemoProp = serializedObject.FindProperty("thoughtsMemo");
        clearThoughtsMemoProp = serializedObject.FindProperty("clearThoughtsMemo");
    }

    public override void OnInspectorGUI()
    {
        RecipeData recipe = (RecipeData)target;

        serializedObject.Update();

        // 基本情報・名称
        EditorGUILayout.PropertyField(recipeIdProp);
        EditorGUILayout.PropertyField(optionDataProp);

        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(potionNameProp, new GUIContent("ポーションの名称"));

        // メモ欄（虫食いあり／なし・確定後／オマケ用）
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("【メモ・解説テキスト】", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(effectMemoProp, new GUIContent("効能・ヒントメモ（虫食いあり）"));
        EditorGUILayout.Space(3);
        EditorGUILayout.PropertyField(trueEffectMemoProp, new GUIContent("効能・ヒントメモ（虫食いなし・完全版）"));

        EditorGUILayout.Space(6);
        EditorGUILayout.PropertyField(thoughtsMemoProp, new GUIContent("確定後メモ"));
        EditorGUILayout.Space(3);
        EditorGUILayout.PropertyField(clearThoughtsMemoProp, new GUIContent("オマケ用メモ"));


        if (recipe.optionData == null)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("OptionData を割り当ててください。選択肢ドロップダウンが表示されます。", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        // 正解データ（ドロップダウン選択式）
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("【正解データ（選択式）】", EditorStyles.boldLabel);

        OptionData options = recipe.optionData;

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