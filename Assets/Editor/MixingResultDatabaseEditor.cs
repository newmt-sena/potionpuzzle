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
        if (optionDataProp == null || recipesProp == null)
        {
            DrawDefaultInspector();
            return;
        }

        MixingResultDatabase db = (MixingResultDatabase)target;

        serializedObject.Update();

        // OptionData のアタッチ欄
        EditorGUILayout.PropertyField(optionDataProp, new GUIContent("選択肢データ (OptionData)"));

        if (db.optionData == null)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("OptionData を割り当ててください。素材の選択肢ドロップダウンが表示されます。", MessageType.Warning);
            EditorGUILayout.Space(8);
            DrawDefaultInspectorExcept(new HashSet<string> { "m_Script", "optionData" });
            serializedObject.ApplyModifiedProperties();
            return;
        }

        OptionData options = db.optionData;
        string[] ingredientArray = options.IngredientOptions != null
            ? options.IngredientOptions.ToArray()
            : new string[0];

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("【調合結果データベース（素材選択式）】", EditorStyles.boldLabel);

        // 上部ツールバー：レシピ数・一括開閉・追加
        DrawHeaderToolbar();

        EditorGUILayout.Space(6);

        // レシピ一覧の描画
        for (int i = 0; i < recipesProp.arraySize; i++)
        {
            SerializedProperty element = recipesProp.GetArrayElementAtIndex(i);
            if (DrawRecipeElement(i, element, ingredientArray))
            {
                // 要素の削除や並び替えが発生した場合は、そのフレームの描画を安全に打ち切る
                break;
            }
        }

        EditorGUILayout.Space(6);
        if (GUILayout.Button("＋ レシピを追加", GUILayout.Height(26)))
        {
            int newIndex = recipesProp.arraySize;
            recipesProp.InsertArrayElementAtIndex(newIndex);
            SerializedProperty added = recipesProp.GetArrayElementAtIndex(newIndex);
            added.isExpanded = true;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeaderToolbar()
    {
        EditorGUILayout.BeginHorizontal();

        int newSize = EditorGUILayout.DelayedIntField("レシピ数", recipesProp.arraySize);
        if (newSize != recipesProp.arraySize)
        {
            recipesProp.arraySize = Mathf.Max(0, newSize);
        }

        if (GUILayout.Button("全展開", GUILayout.Width(56)))
        {
            SetAllExpanded(true);
        }

        if (GUILayout.Button("全収納", GUILayout.Width(56)))
        {
            SetAllExpanded(false);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void SetAllExpanded(bool expanded)
    {
        for (int i = 0; i < recipesProp.arraySize; i++)
        {
            recipesProp.GetArrayElementAtIndex(i).isExpanded = expanded;
        }
    }

    /// <summary>
    /// 1件分のレシピ要素を描画する。配列構造が変わったら true を返す。
    /// </summary>
    private bool DrawRecipeElement(int index, SerializedProperty element, string[] ingredientArray)
    {
        SerializedProperty ingAProp = element.FindPropertyRelative("ingredientA");
        SerializedProperty ingBProp = element.FindPropertyRelative("ingredientB");
        SerializedProperty tempProp = element.FindPropertyRelative("targetTemp");
        SerializedProperty colorNameProp = element.FindPropertyRelative("colorName");
        SerializedProperty colorProp = element.FindPropertyRelative("resultColor");
        SerializedProperty scentProp = element.FindPropertyRelative("scentComment");
        SerializedProperty tasteProp = element.FindPropertyRelative("tasteComment");

        string ingA = ingAProp != null ? ingAProp.stringValue : "";
        string ingB = ingBProp != null ? ingBProp.stringValue : "";
        string tempLabel = (tempProp != null && tempProp.enumDisplayNames != null &&
                            tempProp.enumValueIndex >= 0 && tempProp.enumValueIndex < tempProp.enumDisplayNames.Length)
            ? tempProp.enumDisplayNames[tempProp.enumValueIndex]
            : "";

        string headerTitle = $"#{index + 1}  {FormatIngredientLabel(ingA)} ＋ {FormatIngredientLabel(ingB)}";
        if (!string.IsNullOrEmpty(tempLabel))
        {
            headerTitle += $"  [{tempLabel}]";
        }

        EditorGUILayout.BeginVertical("box");

        // ヘッダー行（Foldout + 並び替え・削除ボタン）
        EditorGUILayout.BeginHorizontal();
        element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, headerTitle, true, EditorStyles.foldoutHeader);

        GUI.enabled = index > 0;
        if (GUILayout.Button("↑", GUILayout.Width(24)))
        {
            recipesProp.MoveArrayElement(index, index - 1);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return true;
        }

        GUI.enabled = index < recipesProp.arraySize - 1;
        if (GUILayout.Button("↓", GUILayout.Width(24)))
        {
            recipesProp.MoveArrayElement(index, index + 1);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return true;
        }
        GUI.enabled = true;

        Color prevBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
        if (GUILayout.Button("×", GUILayout.Width(24)))
        {
            recipesProp.DeleteArrayElementAtIndex(index);
            GUI.backgroundColor = prevBg;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return true;
        }
        GUI.backgroundColor = prevBg;

        EditorGUILayout.EndHorizontal();

        // 折り畳み展開時のみ詳細フィールドを描画
        if (element.isExpanded)
        {
            EditorGUILayout.Space(4);

            // 1. 素材1・素材2のドロップダウン選択
            if (ingAProp != null && ingBProp != null && ingredientArray.Length > 0)
            {
                int indexIngA = GetIndex(ingredientArray, ingAProp.stringValue);
                int indexIngB = GetIndex(ingredientArray, ingBProp.stringValue);

                EditorGUI.BeginChangeCheck();
                int nextIngA = EditorGUILayout.Popup("素材 1", indexIngA, ingredientArray);
                int nextIngB = EditorGUILayout.Popup("素材 2", indexIngB, ingredientArray);

                if (EditorGUI.EndChangeCheck())
                {
                    ingAProp.stringValue = GetValue(ingredientArray, nextIngA);
                    ingBProp.stringValue = GetValue(ingredientArray, nextIngB);
                }
            }

            // 2. 指定温度
            if (tempProp != null)
            {
                EditorGUILayout.PropertyField(tempProp, new GUIContent("指定温度"));
            }

            // 3. 結果の色情報
            EditorGUILayout.Space(4);
            if (colorNameProp != null)
            {
                EditorGUILayout.PropertyField(colorNameProp, new GUIContent("カラー名（例：金・銀）"));
            }
            if (colorProp != null)
            {
                EditorGUILayout.PropertyField(colorProp, new GUIContent("結果の色"));
            }

            // 4. 各種コメント類
            EditorGUILayout.Space(4);
            if (scentProp != null)
            {
                EditorGUILayout.PropertyField(scentProp, new GUIContent("香りと見た目の文言"));
            }
            if (tasteProp != null)
            {
                EditorGUILayout.PropertyField(tasteProp, new GUIContent("レポートの文言"));
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);

        return false;
    }

    private void DrawDefaultInspectorExcept(HashSet<string> excludedNames)
    {
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (excludedNames.Contains(prop.name)) continue;
            EditorGUILayout.PropertyField(prop, true);
        }
    }

    private string FormatIngredientLabel(string value)
    {
        return string.IsNullOrEmpty(value) ? "未選択" : value;
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