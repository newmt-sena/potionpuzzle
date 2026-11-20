using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StorySlideshow : MonoBehaviour
{
    [Header("表示先UI")]
    [SerializeField] private Image stillImage;

    [Header("オープニング用スチル")]
    [SerializeField] private Sprite[] openingSprites;

    [Header("エンディング用スチル")]
    [SerializeField] private Sprite[] endingSprites;

    [Header("進行設定")]
    [SerializeField] private bool autoAdvance = false;
    [SerializeField] private float autoInterval = 2.0f;

    private Sprite[] activeSprites;
    private int currentIndex = 0;
    private bool isFinished = false;
    private Coroutine autoCoroutine;

    private void Start()
    {
        if (GameFlowManager.Instance != null &&
            GameFlowManager.Instance.CurrentStoryMode == GameFlowManager.StoryMode.Ending)
        {
            activeSprites = endingSprites;
        }
        else
        {
            activeSprites = openingSprites;
        }

        if (activeSprites == null || activeSprites.Length == 0)
        {
            FinishStory();
            return;
        }

        currentIndex = 0;
        ApplySprite();

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
            AdvanceSlide();
        }
    }

    public void AdvanceSlide()
    {
        if (isFinished) return;

        if (autoCoroutine != null)
        {
            StopCoroutine(autoCoroutine);
            autoCoroutine = null;
        }

        currentIndex++;
        if (currentIndex < activeSprites.Length)
        {
            ApplySprite();
            if (autoAdvance)
            {
                autoCoroutine = StartCoroutine(AutoAdvanceRoutine());
            }
        }
        else
        {
            FinishStory();
        }
    }

    private IEnumerator AutoAdvanceRoutine()
    {
        yield return new WaitForSeconds(autoInterval);
        AdvanceSlide();
    }

    private void ApplySprite()
    {
        if (stillImage != null && currentIndex < activeSprites.Length)
        {
            stillImage.sprite = activeSprites[currentIndex];
        }
    }

    private void FinishStory()
    {
        if (isFinished) return;
        isFinished = true;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OnStoryFinished();
        }
    }
}