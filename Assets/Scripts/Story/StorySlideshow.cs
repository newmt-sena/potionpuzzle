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
    [SerializeField] private TextMeshProUGUI storyText;

    [Header("オープニング用スライド")]
    [SerializeField] private SlideEntry[] openingSlides;

    [Header("エンディング用スライド")]
    [SerializeField] private SlideEntry[] endingSlides;

    [Header("文字送り設定")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float charInterval = 0.05f;
    [SerializeField] private bool playVoiceSE = false;
    [SerializeField] private SoundManager.SEType voiceSEType = SoundManager.SEType.UI_Click; // プロジェクトのSETypeに合わせて変更

    [Header("進行設定")]
    [SerializeField] private bool autoAdvance = false;
    [SerializeField] private float autoInterval = 2.0f;

    private SlideEntry[] activeSlides;
    private int currentSlideIndex = 0;
    private int currentLineIndex = 0;
    private string currentFullText = string.Empty;
    private bool isTyping = false;
    private bool isFinished = false;

    private Coroutine typingCoroutine;
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
    }

    private void Update()
    {
        if (isFinished) return;

        // 画面クリック／タップで次へ進む（または文字送り全文表示）
        if (Input.GetMouseButtonDown(0))
        {
            Advance();
        }
    }

    /// <summary>
    /// 文字送り中なら全文表示、完了済みなら次のテキスト／次のスチルへ進める
    /// </summary>
    public void Advance()
    {
        if (isFinished) return;

        // 文字送り中ならスキップして全文を即表示
        if (isTyping)
        {
            CompleteCurrentLineImmediately();
            return;
        }

        StopAutoRoutine();

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
            }
        }
    }

    private void ApplyCurrentSlideAndLine()
    {
        if (activeSlides == null || currentSlideIndex >= activeSlides.Length) return;

        StopTypingRoutine();
        StopAutoRoutine();

        SlideEntry slide = activeSlides[currentSlideIndex];

        if (stillImage != null)
        {
            stillImage.sprite = slide.sprite;
        }

        string rawLine = string.Empty;
        if (slide.lines != null && slide.lines.Length > 0 && currentLineIndex < slide.lines.Length)
        {
            rawLine = slide.lines[currentLineIndex] ?? string.Empty;
        }

        currentFullText = rawLine.Replace("\\n", "\n");

        if (storyText == null)
        {
            StartAutoRoutineIfNeeded();
            return;
        }

        storyText.gameObject.SetActive(true);

        if (useTypewriter && !string.IsNullOrEmpty(currentFullText))
        {
            typingCoroutine = StartCoroutine(TypeLineRoutine(currentFullText));
        }
        else
        {
            storyText.text = currentFullText;
            isTyping = false;
            StartAutoRoutineIfNeeded();
        }
    }

    private IEnumerator TypeLineRoutine(string fullText)
    {
        isTyping = true;
        storyText.text = string.Empty;

        int charCount = 0;
        foreach (char c in fullText)
        {
            storyText.text += c;
            charCount++;

            if (playVoiceSE && !char.IsWhiteSpace(c) && charCount % 2 == 0)
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySE(voiceSEType);
                }
            }

            yield return new WaitForSeconds(charInterval);
        }

        isTyping = false;
        typingCoroutine = null;
        StartAutoRoutineIfNeeded();
    }

    private void CompleteCurrentLineImmediately()
    {
        StopTypingRoutine();

        if (storyText != null)
        {
            storyText.text = currentFullText;
        }

        isTyping = false;
        StartAutoRoutineIfNeeded();
    }

    private void StartAutoRoutineIfNeeded()
    {
        if (!autoAdvance || isFinished || isTyping) return;

        StopAutoRoutine();
        autoCoroutine = StartCoroutine(AutoAdvanceRoutine());
    }

    private IEnumerator AutoAdvanceRoutine()
    {
        yield return new WaitForSeconds(autoInterval);
        Advance();
    }

    private void StopTypingRoutine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = false;
    }

    private void StopAutoRoutine()
    {
        if (autoCoroutine != null)
        {
            StopCoroutine(autoCoroutine);
            autoCoroutine = null;
        }
    }

    private void FinishStory()
    {
        if (isFinished) return;
        isFinished = true;

        StopTypingRoutine();
        StopAutoRoutine();

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OnStoryFinished();
        }
    }
}