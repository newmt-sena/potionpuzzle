using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorySlideshow : MonoBehaviour
{
    [Serializable]
    public struct SlideEntry
    {
       public Sprite sprite;

        [TextArea(2, 6)]
        public string[] lines; // このスチルで順番に表示するテキスト群
    }

    [Header("表示先UI")]
    [SerializeField] private Image stillImage;
    [SerializeField] private TextMeshProUGUI storyText; // 標準UIのTextを使う場合は UnityEngine.UI.Text に変更

    [Header("オープニング用スライド")]
    [SerializeField] private SlideEntry[] openingSlides;

    [Header("エンディング用スライド")]
    [SerializeField] private SlideEntry[] endingSlides;

    [Header("進行設定")]
    [SerializeField] private bool autoAdvance = false;
    [SerializeField] private float autoInterval = 2.0f;

    private SlideEntry[] activeSlides;
    private int currentSlideIndex = 0;
    private int currentLineIndex = 0;
    private bool isFinished = false;
    private Coroutine autoCoroutine;

    private void Start()
    {
        if (GameFlowManager.Instance != null &&
            GameFlowManager.Instance.CurrentStoryMode == GameFlowManager.StoryMode.Ending)
        {
            activeSlides = endingSlides;
        }
        else
        {
            activeSlides = openingSlides;
        }

        if (activeSlides == null || activeSlides.Length == 0)
        {
            FinishStory();
            return;
        }

        currentSlideIndex = 0;
        currentLineIndex = 0;
        ApplyCurrentSlideAndLine();

        if (autoAdvance)
        {
            autoCoroutine = StartCoroutine(AutoAdvanceRoutine());
        }
    }

    private void Update()
    {
        if (isFinished) return;

        // 画面クリック／タップで次へ進む
        if (Input.GetMouseButtonDown(0))
        {
            Advance();
        }
    }

    /// <summary>
    /// 次のテキスト、または次のスチルへ進める
    /// </summary>
    public void Advance()
    {
        if (isFinished) return;

        if (autoCoroutine != null)
        {
            StopCoroutine(autoCoroutine);
            autoCoroutine = null;
        }

        SlideEntry currentSlide = activeSlides[currentSlideIndex];
        int lineCount = currentSlide.lines != null ? currentSlide.lines.Length : 0;

        // 現在のスチル内にまだ次のテキストがあればそちらへ進める
        if (currentLineIndex + 1 < lineCount)
        {
            currentLineIndex++;
            ApplyCurrentSlideAndLine();
        }
        else
        {
            // 次のスチルへ進む
            currentSlideIndex++;
            currentLineIndex = 0;

            if (currentSlideIndex < activeSlides.Length)
            {
                ApplyCurrentSlideAndLine();
            }
            else
            {
                FinishStory();
                return;
            }
        }

        if (autoAdvance)
        {
            autoCoroutine = StartCoroutine(AutoAdvanceRoutine());
        }
    }

    private IEnumerator AutoAdvanceRoutine()
    {
        yield return new WaitForSeconds(autoInterval);
        Advance();
    }

    private void ApplyCurrentSlideAndLine()
    {
        if (activeSlides == null || currentSlideIndex >= activeSlides.Length) return;

        SlideEntry slide = activeSlides[currentSlideIndex];

        if (stillImage != null)
        {
            stillImage.sprite = slide.sprite;
        }

        if (storyText != null)
        {
            if (slide.lines != null && slide.lines.Length > 0 && currentLineIndex < slide.lines.Length)
            {
                storyText.gameObject.SetActive(true);
                storyText.text = slide.lines[currentLineIndex];
            }
            else
            {
                // テキストを設定していないスチルの場合は空にする（必要なら非表示にもできます）
                storyText.text = string.Empty;
            }
        }
    }

    private void FinishStory()
    {
        if (isFinished) return;
        isFinished = true;

        if (autoCoroutine != null)
        {
            StopCoroutine(autoCoroutine);
            autoCoroutine = null;
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OnStoryFinished();
        }
    }
}