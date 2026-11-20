using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways] //この文は消さない（エディタ上でも確認できるようにしてるため）
public class AspectKeeperScript : MonoBehaviour
{
    public Camera mainCamera;   //カメラ

    public Vector2 aspectVector;

    void Start()
    {

    }

    void Update()
    {
        if (Screen.width <= 0 || Screen.height <= 0 || mainCamera == null) return;

        var screenAspect = Screen.width / (float)Screen.height; //画面のアスペクト比
        var targetAspect = aspectVector.x / aspectVector.y;     //目的のアスペクト比

        var magRate = targetAspect / screenAspect;  //目的アスペクト比にするための倍率

        var viewportRect = new Rect(0, 0, 1, 1);    //Viewport初期値でRectを作成

        if (magRate < 1)
        {
            viewportRect.width = magRate;                       //横幅を変更
            viewportRect.x = 0.5f - viewportRect.width * 0.5f;  //中央に寄せる
        }
        else
        {
            viewportRect.height = 1 / magRate;                  //縦幅を変更
            viewportRect.y = 0.5f - viewportRect.height * 0.5f; //中央に寄せる
        }

        mainCamera.rect = viewportRect; //カメラのViewportに適用
    }
}
