#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RecipeManager.RecipeSlot))]
public class RecipeSlotDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // リスト内のインデックス（0始まり）を取得して No1. 形式にする
        int index = GetIndexFromPropertyPath(property.propertyPath);
        string title = index >= 0 ? $"No{index + 1}." : label.text;

        // targetRecipe が設定されていれば名前も添える（未設定時は No1. のみ）
        SerializedProperty targetRecipeProp = property.FindPropertyRelative("targetRecipe");
        if (targetRecipeProp != null && targetRecipeProp.objectReferenceValue is RecipeData recipeData)
        {
            if (!string.IsNullOrEmpty(recipeData.PotionName))
            {
                title = index >= 0
                    ? $"No{index + 1}.（{recipeData.PotionName}）"
                    : recipeData.PotionName;
            }
        }

        EditorGUI.PropertyField(position, property, new GUIContent(title), true);
    }

    private int GetIndexFromPropertyPath(string path)
    {
        // 例: "recipePages.Array.data[0]" から 0 を取り出す
        int start = path.LastIndexOf('[');
        int end = path.LastIndexOf(']');
        if (start >= 0 && end > start)
        {
            if (int.TryParse(path.Substring(start + 1, end - start - 1), out int idx))
            {
                return idx;
            }
        }
        return -1;
    }
}
#endif