using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueLogItemUI : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI speakerNameText; // 名前タグ用のText
    [SerializeField] private TextMeshProUGUI messageText;     // セリフ用のText
    [SerializeField] private Image faceIconImage;           // 顔グラ用のImage
    [SerializeField] private Image NameTagImage;           // 名前タグ用のImage

    [Header("キャラごとのログ用顔グラ")]
    [SerializeField] private Sprite leftCharIcon;  // 左キャラ（主人公側）のログ用アイコン
    [SerializeField] private Sprite rightCharIcon; // 右キャラ側のログ用アイコン

    [SerializeField] private Sprite leftTag;  
    [SerializeField] private Sprite rightTag; 

    public void Setup(DialogueManager.LogData log)
    {
        if (speakerNameText != null)
        {
            speakerNameText.text = log.speakerName;
        }

        if (messageText != null)
        {
            messageText.text = log.message;
        }

        if (faceIconImage != null)
        {
            Sprite targetSprite = log.isLeftChar ? leftCharIcon : rightCharIcon;
            faceIconImage.sprite = targetSprite;
            faceIconImage.gameObject.SetActive(targetSprite != null);
        }

        if(NameTagImage != null)
        {
            Sprite targetSprite = log.isLeftChar ? leftTag : rightTag;
            NameTagImage.sprite = targetSprite;
            NameTagImage.gameObject.SetActive(targetSprite != null);
        }
    }
}