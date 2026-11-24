#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MixingResultDatabase))]
public class MixingResultDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MixingResultDatabase db = (MixingResultDatabase)target;

        serializedObject.Update();

        // OptionData のアタッチ欄を表示
        EditorGUILayout.PropertyField(serializedObject.FindProperty("optionData"));

        if (db.optionData == null)
        {
            EditorGUILayout.HelpBox("OptionData を割り当ててください。素材の選択肢ドロップダウンが表示されます。", MessageType.Warning);
            EditorGUILayout.Space(10);
            DrawDefaultInspectorExceptOptionData();
            serializedObject.ApplyModifiedProperties();
            return;
        }

        OptionData options = db.optionData;
        SerializedProperty recipesProp = serializedObject.FindProperty("recipes");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("【調合結果データベース（素材選択式）】", EditorStyles.boldLabel);

        // リストのサイズ調整フィールド
        int newSize = EditorGUILayout.DelayedIntField("レシピ数", recipesProp.arraySize);
        if (newSize != recipesProp.arraySize)
        {
            recipesProp.arraySize = Mathf.Max(0, newSize);
        }

        EditorGUILayout.Space(5);

        // リスト要素ごとに描画
        for (int i = 0; i < recipesProp.arraySize; i++)
        {
            SerializedProperty element = recipesProp.GetArrayElementAtIndex(i);
            SerializedProperty ingAProp = element.FindPropertyRelative("ingredientA");
            SerializedProperty ingBProp = element.FindPropertyRelative("ingredientB");
            SerializedProperty tempProp = element.FindPropertyRelative("targetTemp");

            // 成果物プロパティの取得
            SerializedProperty colorNameProp = element.FindPropertyRelative("colorName");     // ★ 追加：カラー名（文字列）
            SerializedProperty colorProp = element.FindPropertyRelative("resultColor");       // （既存：Color型）
            SerializedProperty scentProp = element.FindPropertyRelative("scentComment");
            SerializedProperty tasteProp = element.FindPropertyRelative("tasteComment");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"組合せ #{i + 1}", EditorStyles.boldLabel);

            // 1. 素材Aと素材Bのドロップダウン選択
            int indexIngA = GetIndex(options.IngredientOptions, ingAProp.stringValue);
            int indexIngB = GetIndex(options.IngredientOptions, ingBProp.stringValue);

            EditorGUI.BeginChangeCheck();

            indexIngA = EditorGUILayout.Popup("素材 1", indexIngA, options.IngredientOptions.ToArray());
            indexIngB = EditorGUILayout.Popup("素材 2", indexIngB, options.IngredientOptions.ToArray());

            if (EditorGUI.EndChangeCheck())
            {
                ingAProp.stringValue = GetValue(options.IngredientOptions, indexIngA);
                ingBProp.stringValue = GetValue(options.IngredientOptions, indexIngB);
            }

            // 2. 指定温度
            if (tempProp != null)
            {
                EditorGUILayout.PropertyField(tempProp, new GUIContent("指定温度"));
            }

            // 3. 結果の色情報
            EditorGUILayout.PropertyField(colorNameProp, new GUIContent("カラー名")); // ★ 追加：ここに「金」や「銀」と入力する
            EditorGUILayout.PropertyField(colorProp, new GUIContent("結果の色"));

            // 4. 各種コメント類
            EditorGUILayout.PropertyField(scentProp, new GUIContent("香りと見た目の文言"));
            EditorGUILayout.PropertyField(tasteProp, new GUIContent("レポートの文言"));

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDefaultInspectorExceptOptionData()
    {
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "m_Script" || prop.name == "optionData") continue;
            EditorGUILayout.PropertyField(prop, true);
        }
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