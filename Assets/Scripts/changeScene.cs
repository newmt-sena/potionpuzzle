using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class changeScene : MonoBehaviour
{
    [SerializeField] private string targetSceneName; // 遷移先のシーン名

    /// <summary>
    /// ボタンの OnClick イベントに登録する関数
    /// </summary>
    public void OnClickChangeScene()
    {
        // シーン内にある RecipeManager を自動で探す
        RecipeManager recipeManager = FindFirstObjectByType<RecipeManager>();

        if (recipeManager != null)
        {
            // 遷移直前に全入力状態（ドロップダウンの選択）を保存
            recipeManager.SaveAllPagesDataForSceneChange();
            Debug.Log("RecipeManager を自動検出して保存処理を実行しました。");
        }
        else
        {
            // 調合画面など RecipeManager が存在しないシーンから戻る場合はここが通る（問題なし）
            Debug.Log("RecipeManager が見つからないため、保存処理をスキップしてシーン遷移します。");
        }

        // シーン遷移を実行
        SceneManager.LoadScene(targetSceneName);
    }
}
