#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MixingResultDatabase))]
public class MixingResultDatabaseEditor : Editor
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
        MixingResultDatabase db = (MixingResultDatabase)target;

        serializedObject.Update();

        if (optionDataProp != null)
        {
            EditorGUILayout.PropertyField(optionDataProp, new GUIContent("共通選択肢データ (OptionData)"));
        }

        OptionData options = db.optionData;
        if (options == null)
        {
            EditorGUILayout.HelpBox(
                "OptionData を割り当ててください。素材の選択肢ドロップダウンが表示されます。",
                MessageType.Warning);
            EditorGUILayout.Space(8);
            DrawDefaultInspectorExceptOptionData();
            serializedObject.ApplyModifiedProperties();
            return;
        }

        string[] ingredientArray = (options.IngredientOptions != null && options.IngredientOptions.Count > 0)
            ? options.IngredientOptions.ToArray()
            : null;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("【調合結果データベース（素材選択式）】", EditorStyles.boldLabel);

        if (recipesProp != null)
        {
            for (int i = 0; i < recipesProp.arraySize; i++)
            {
                SerializedProperty element = recipesProp.GetArrayElementAtIndex(i);
                DrawRecipeElement(element, i, ingredientArray);
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.Space(6);
            if (GUILayout.Button("＋ 組合せを追加"))
            {
                recipesProp.InsertArrayElementAtIndex(recipesProp.arraySize);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRecipeElement(SerializedProperty element, int index, string[] ingredientArray)
    {
        SerializedProperty ingAProp = element.FindPropertyRelative("ingredientA");
        SerializedProperty ingBProp = element.FindPropertyRelative("ingredientB");
        SerializedProperty tempProp = element.FindPropertyRelative("targetTemp");
        SerializedProperty colorNameProp = element.FindPropertyRelative("colorName");
        SerializedProperty colorProp = element.FindPropertyRelative("resultColor");
        SerializedProperty scentProp = element.FindPropertyRelative("scentComment");
        SerializedProperty tasteProp = element.FindPropertyRelative("tasteComment");

        string aText = (ingAProp != null && !string.IsNullOrEmpty(ingAProp.stringValue)) ? ingAProp.stringValue : "未設定";
        string bText = (ingBProp != null && !string.IsNullOrEmpty(ingBProp.stringValue)) ? ingBProp.stringValue : "未設定";
        string colorNameText = (colorNameProp != null && !string.IsNullOrEmpty(colorNameProp.stringValue))
            ? $" / {colorNameProp.stringValue}"
            : "";

        string headerTitle = $"組合せ #{index + 1}：{aText} ＋ {bText}{colorNameText}";

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, headerTitle, true);

        if (GUILayout.Button("削除", GUILayout.Width(48)))
        {
            recipesProp.DeleteArrayElementAtIndex(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        EditorGUILayout.EndHorizontal();

        if (element.isExpanded)
        {
            EditorGUI.indentLevel++;

            // 1. 素材Aと素材Bのドロップダウン選択
            if (ingAProp != null && ingBProp != null)
            {
                if (ingredientArray != null)
                {
                    int indexIngA = GetIndex(ingredientArray, ingAProp.stringValue);
                    int indexIngB = GetIndex(ingredientArray, ingBProp.stringValue);

                    EditorGUI.BeginChangeCheck();
                    indexIngA = EditorGUILayout.Popup("素材 1", indexIngA, ingredientArray);
                    indexIngB = EditorGUILayout.Popup("素材 2", indexIngB, ingredientArray);

                    if (EditorGUI.EndChangeCheck())
                    {
                        ingAProp.stringValue = GetValue(ingredientArray, indexIngA);
                        ingBProp.stringValue = GetValue(ingredientArray, indexIngB);
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(ingAProp, new GUIContent("素材 1"));
                    EditorGUILayout.PropertyField(ingBProp, new GUIContent("素材 2"));
                }
            }

            // 2. 指定温度
            if (tempProp != null)
            {
                EditorGUILayout.PropertyField(tempProp, new GUIContent("指定温度"));
            }

            // 3. 結果の色情報
            if (colorNameProp != null)
            {
                EditorGUILayout.PropertyField(colorNameProp, new GUIContent("カラー名"));
            }
            if (colorProp != null)
            {
                EditorGUILayout.PropertyField(colorProp, new GUIContent("結果の色"));
            }

            // 4. 各種コメント類
            if (scentProp != null)
            {
                EditorGUILayout.PropertyField(scentProp, new GUIContent("香りと見た目の文言"));
            }
            if (tasteProp != null)
            {
                EditorGUILayout.PropertyField(tasteProp, new GUIContent("レポートの文言"));
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
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

    private int GetIndex(string[] array, string currentVal)
    {
        if (array == null || array.Length == 0 || string.IsNullOrEmpty(currentVal)) return 0;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == currentVal) return i;
        }
        return 0;
    }

    private string GetValue(string[] array, int index)
    {
        if (array == null || index < 0 || index >= array.Length) return "";
        return array[index];
    }
}
#endif