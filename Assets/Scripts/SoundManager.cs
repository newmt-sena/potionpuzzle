using System;
using System.Collections;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public enum BGMType
    {
        None,
        Title,
        Talk,
        Mixing,
        Note,
        Ending
    }

    public enum SEType
    {
        None,

        // UI操作系
        UI_Click,       // 汎用ボタン決定・確定
        UI_Cancel,      // キャンセル・戻る・リセット
        UI_Select,      // 選択・切り替え（素材選択やタブ/ページ送りなど）

        // 文字送り（キャラ別）
        Voice_Ether, // エーテル文字送り
        Voice_Arcana, // アルカナ文字送り

        // 調合・アクション系
        Mix_Brew,       // 調合するボタンを押したとき
        Mix_Taste,      // 試飲するボタンを押したとき
        Mix_Reset,      // リセットボタンを押したとき

        // 結果・演出系
        Result_Success, // 調合成功・レシピ確定など
        Result_Fail,    // 調合失敗など
        Stamp_Confirm   // ノート確定（ハンコ）など
    }

    [Serializable]
    public struct BGMEntry
    {
        public BGMType type;
        public AudioClip clip;
    }

    [Serializable]
    public struct SEEntry
    {
        public SEType type;
        public AudioClip clip;
    }

    private static SoundManager instance;
    public static SoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                EnsureExists();
            }
            return instance;
        }
    }

    [Header("Audio Source 割り当て")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioSource seAudioSource;

    [Header("BGM / SE 一覧")]
    [SerializeField] private BGMEntry[] bgmList;
    [SerializeField] private SEEntry[] seList;

    private Coroutine bgmFadeCoroutine;




    private BGMType currentBGMType = BGMType.None;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreateOnLoad()
    {
        EnsureExists();
    }

    private static void EnsureExists()
    {
        if (instance != null) return;

        SoundManager existing = FindFirstObjectByType<SoundManager>();
        if (existing != null)
        {
            instance = existing;
            DontDestroyOnLoad(instance.gameObject);
            instance.LoadVolume();
            return;
        }

        // Resources/SoundManager.prefab があればそこから生成
        SoundManager prefab = Resources.Load<SoundManager>("SoundManager");
        if (prefab != null)
        {
            instance = Instantiate(prefab);
            instance.name = "SoundManager";
        }
        else
        {
            GameObject obj = new GameObject("SoundManager");
            instance = obj.AddComponent<SoundManager>();
            instance.SetupDefaultAudioSources();
        }

        DontDestroyOnLoad(instance.gameObject);
        instance.LoadVolume();
    }

    private void SetupDefaultAudioSources()
    {
        if (bgmAudioSource == null)
        {
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
            bgmAudioSource.loop = true;
            bgmAudioSource.playOnAwake = false;
        }

        if (seAudioSource == null)
        {
            seAudioSource = gameObject.AddComponent<AudioSource>();
            seAudioSource.loop = false;
            seAudioSource.playOnAwake = false;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SetupDefaultAudioSources();
            LoadVolume();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void PlayBGM(BGMType type, bool loop = true, float fadeDuration = 0.35f)
    {
        if (type == BGMType.None)
        {
            StopBGM(fadeDuration);
            return;
        }

        // すでに同じBGMが再生中なら何もしない（途切れ防止）
        if (currentBGMType == type && bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.loop = loop;
            return;
        }

        BGMEntry entry = Array.Find(bgmList, b => b.type == type);
        if (entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] BGMが未設定です: {type}");
            return;
        }

        SetupDefaultAudioSources();

        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }

        float targetVolume = PlayerPrefs.GetFloat("BGM_Volume", 1f);

        if (fadeDuration > 0f && bgmAudioSource.isPlaying)
        {
            bgmFadeCoroutine = StartCoroutine(SwitchBGMWithFade(type, entry.clip, loop, targetVolume, fadeDuration));
        }
        else
        {
            bgmAudioSource.Stop();
            currentBGMType = type;
            bgmAudioSource.clip = entry.clip;
            bgmAudioSource.loop = loop;
            bgmAudioSource.volume = targetVolume;
            bgmAudioSource.Play();
        }
    }

    public void StopBGM(float fadeDuration = 0f)
    {
        if (bgmAudioSource == null) return;

        if (bgmFadeCoroutine != null)
        {
            StopCoroutine(bgmFadeCoroutine);
        }

        if (fadeDuration > 0f && bgmAudioSource.isPlaying)
        {
            bgmFadeCoroutine = StartCoroutine(FadeOutAndStop(fadeDuration));
        }
        else
        {
            currentBGMType = BGMType.None;
            bgmAudioSource.Stop();
            bgmAudioSource.clip = null;
        }
    }

    private IEnumerator SwitchBGMWithFade(BGMType nextType, AudioClip nextClip, bool loop, float targetVolume, float duration)
    {
        float halfDuration = duration * 0.5f;
        float startVolume = bgmAudioSource.volume;

        // 1. 現在のBGMをフェードアウトして停止
        float t = 0f;
        while (t < halfDuration)
        {
            t += Time.unscaledDeltaTime;
            bgmAudioSource.volume = Mathf.Lerp(startVolume, 0f, t / halfDuration);
            yield return null;
        }

        bgmAudioSource.Stop();
        currentBGMType = nextType;
        bgmAudioSource.clip = nextClip;
        bgmAudioSource.loop = loop;
        bgmAudioSource.Play();

        // 2. 新しいBGMをフェードイン
        t = 0f;
        while (t < halfDuration)
        {
            t += Time.unscaledDeltaTime;
            bgmAudioSource.volume = Mathf.Lerp(0f, targetVolume, t / halfDuration);
            yield return null;
        }

        bgmAudioSource.volume = targetVolume;
        bgmFadeCoroutine = null;
    }

    private IEnumerator FadeOutAndStop(float duration)
    {
        float startVolume = bgmAudioSource.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            bgmAudioSource.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }

        bgmAudioSource.Stop();
        bgmAudioSource.clip = null;
        bgmAudioSource.volume = startVolume;
        currentBGMType = BGMType.None;
        bgmFadeCoroutine = null;
    }

    public void PlaySE(SEType type)
    {
        SEEntry entry = Array.Find(seList, s => s.type == type);
        if (entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] SEが未設定です: {type}");
            return;
        }

        SetupDefaultAudioSources();
        seAudioSource.PlayOneShot(entry.clip);
    }

    public void PlaySE(AudioClip clip)
    {
        if (clip == null) return;
        SetupDefaultAudioSources();
        seAudioSource.PlayOneShot(clip);
    }

    public void SetBGMVolume(float volume)
    {
        SetupDefaultAudioSources();
        bgmAudioSource.volume = volume;
        PlayerPrefs.SetFloat("BGM_Volume", volume);
        PlayerPrefs.Save();
    }

    public void SetSEVolume(float volume)
    {
        SetupDefaultAudioSources();
        seAudioSource.volume = volume;
        PlayerPrefs.SetFloat("SE_Volume", volume);
        PlayerPrefs.Save();
    }

    private void LoadVolume()
    {
        SetupDefaultAudioSources();
        bgmAudioSource.volume = PlayerPrefs.GetFloat("BGM_Volume", 1f);
        seAudioSource.volume = PlayerPrefs.GetFloat("SE_Volume", 1f);
    }
}