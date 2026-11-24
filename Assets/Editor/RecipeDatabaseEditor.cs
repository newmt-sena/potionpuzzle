#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RecipeDatabase))]
public class RecipeDatabaseEditor : Editor
{
    private SerializedProperty optionDataProp;
    private SerializedProperty recipesProp;

    private void OnEnable()
    {
        optionDataProp = serializedObject.FindProperty("optionData");
        recipesProp = serializedObject.FindProperty("recipes");
    }

    public override void OnInspectorGUI()
    {
        RecipeDatabase database = (RecipeDatabase)target;

        serializedObject.Update();

        // データベース共通の OptionData
        EditorGUILayout.PropertyField(optionDataProp, new GUIContent("共通選択肢データ (OptionData)"));

        OptionData options = database.optionData;
        if (options == null)
        {
            EditorGUILayout.HelpBox(
                "OptionData を割り当ててください。各レシピの正解データをプルダウンから選択できるようになります。",
                MessageType.Warning);
        }

        EditorGUILayout.Space(8);

        // レシピリスト本体（要素の追加・削除・並び替えが可能）
        if (recipesProp != null)
        {
            for (int i = 0; i < recipesProp.arraySize; i++)
            {
                SerializedProperty entryProp = recipesProp.GetArrayElementAtIndex(i);
                DrawRecipeEntry(entryProp, i, options);
            }

            EditorGUILayout.Space(6);
            if (GUILayout.Button("＋ レシピを追加"))
            {
                recipesProp.InsertArrayElementAtIndex(recipesProp.arraySize);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRecipeEntry(SerializedProperty entryProp, int index, OptionData options)
    {
        SerializedProperty recipeIdProp = entryProp.FindPropertyRelative("recipeId");
        SerializedProperty potionNameProp = entryProp.FindPropertyRelative("potionName");
        SerializedProperty ingredientAProp = entryProp.FindPropertyRelative("potionIngredientA");
        SerializedProperty ingredientBProp = entryProp.FindPropertyRelative("potionIngredientB");
        SerializedProperty colorProp = entryProp.FindPropertyRelative("potionColor");
        SerializedProperty scentProp = entryProp.FindPropertyRelative("potionScent");
        SerializedProperty effectProp = entryProp.FindPropertyRelative("potionEffect");

        SerializedProperty effectMemoProp = entryProp.FindPropertyRelative("effectMemo");
        SerializedProperty thoughtsMemoProp = entryProp.FindPropertyRelative("thoughtsMemo");
        SerializedProperty effectMemoFullProp = entryProp.FindPropertyRelative("effectMemoFull");
        SerializedProperty arcanaCommentProp = entryProp.FindPropertyRelative("arcanaComment");
        SerializedProperty etherCommentProp = entryProp.FindPropertyRelative("etherComment");

        string headerTitle = string.IsNullOrEmpty(potionNameProp.stringValue)
            ? $"レシピ #{index + 1}（未命名）"
            : $"レシピ #{index + 1}：{potionNameProp.stringValue}";

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        entryProp.isExpanded = EditorGUILayout.Foldout(entryProp.isExpanded, headerTitle, true);

        if (GUILayout.Button("削除", GUILayout.Width(48)))
        {
            recipesProp.DeleteArrayElementAtIndex(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (entryProp.isExpanded)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(recipeIdProp, new GUIContent("レシピID"));
            EditorGUILayout.PropertyField(potionNameProp, new GUIContent("ポーションの名称"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("【メモ・オマケ文章】", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(effectMemoProp, new GUIContent("効能・ヒントメモ（虫食い）"));
            EditorGUILayout.PropertyField(thoughtsMemoProp, new GUIContent("確定後メモ"));
            EditorGUILayout.PropertyField(effectMemoFullProp, new GUIContent("虫食いナシ全文（オマケ）"));
            EditorGUILayout.PropertyField(arcanaCommentProp, new GUIContent("アルカナ解説（オマケ）"));
            EditorGUILayout.PropertyField(etherCommentProp, new GUIContent("エーテル解説（オマケ）"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("【正解データ】", EditorStyles.boldLabel);

            if (options != null)
            {
                DrawPopupField(ingredientAProp, "正解の材料1", options.IngredientOptions);
                DrawPopupField(ingredientBProp, "正解の材料2", options.IngredientOptions);
                DrawPopupField(scentProp, "正解の香り", options.ScentOptions);
                DrawPopupField(colorProp, "正解の色", options.ColorOptions);
                DrawPopupField(effectProp, "正解の効能", options.EffectOptions);
            }
            else
            {
                EditorGUILayout.PropertyField(ingredientAProp, new GUIContent("正解の材料1"));
                EditorGUILayout.PropertyField(ingredientBProp, new GUIContent("正解の材料2"));
                EditorGUILayout.PropertyField(scentProp, new GUIContent("正解の香り"));
                EditorGUILayout.PropertyField(colorProp, new GUIContent("正解の色"));
                EditorGUILayout.PropertyField(effectProp, new GUIContent("正解の効能"));
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPopupField(SerializedProperty stringProp, string label, List<string> optionList)
    {
        if (optionList == null || optionList.Count == 0)
        {
            EditorGUILayout.PropertyField(stringProp, new GUIContent(label));
            return;
        }

        int currentIndex = 0;
        if (!string.IsNullOrEmpty(stringProp.stringValue))
        {
            int found = optionList.IndexOf(stringProp.stringValue);
            currentIndex = found >= 0 ? found : 0;
        }

        EditorGUI.BeginChangeCheck();
        int nextIndex = EditorGUILayout.Popup(label, currentIndex, optionList.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            if (nextIndex >= 0 && nextIndex < optionList.Count)
            {
                stringProp.stringValue = optionList[nextIndex];
            }
        }
    }
}
#endif