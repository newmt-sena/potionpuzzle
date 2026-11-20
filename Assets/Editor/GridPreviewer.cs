#if UNITY_EDITOR
using UnityEngine;

[ExecuteAlways]
public class GridPreviewer : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private GameObject buttonPrefab; // 生成するボタンのプレハブ
    [SerializeField] private int previewCount = 10;     // エディタ上で表示したい数

    // エディタ上で値が変更された時や描画時に実行
    private void OnValidate()
    {
        // プレイ中は何もしない
        if (Application.isPlaying) return;

        // 遅延実行（エディタ上の安全なタイミングで生成する）
        UnityEditor.EditorApplication.delayCall += RefreshPreview;
    }

    private void RefreshPreview()
    {
        if (this == null || buttonPrefab == null) return;

        // 子要素（既存のプレビュー用ボタン）を一旦全削除
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        // 指定した数だけプレビュー用ボタンを生成
        for (int i = 0; i < previewCount; i++)
        {
            GameObject btn = Instantiate(buttonPrefab, transform);
            btn.name = $"Preview_Button_{i}";
        }
    }
}
#endif