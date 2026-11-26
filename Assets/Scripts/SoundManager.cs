using System;
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
        Confirm,
        Cancel,
        Select,
        MixSuccess,
        MixFail,
        Taste
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

    [Header("Audio Source äÑÇËìñÇƒ")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioSource seAudioSource;

    [Header("BGM / SE àÍóó")]
    [SerializeField] private BGMEntry[] bgmList;
    [SerializeField] private SEEntry[] seList;

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

        // Resources/SoundManager.prefab Ç™Ç†ÇÍÇŒÇªÇ±Ç©ÇÁê∂ê¨
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

    public void PlayBGM(BGMType type, bool loop = true)
    {
        if (type == BGMType.None)
        {
            StopBGM();
            return;
        }

        if (currentBGMType == type && bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            return;
        }

        BGMEntry entry = Array.Find(bgmList, b => b.type == type);
        if (entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] BGMÇ™ñ¢ê›íËÇ≈Ç∑: {type}");
            return;
        }

        SetupDefaultAudioSources();
        currentBGMType = type;
        bgmAudioSource.clip = entry.clip;
        bgmAudioSource.loop = loop;
        bgmAudioSource.Play();
    }

    public void StopBGM()
    {
        currentBGMType = BGMType.None;
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
        }
    }

    public void PlaySE(SEType type)
    {
        SEEntry entry = Array.Find(seList, s => s.type == type);
        if (entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] SEÇ™ñ¢ê›íËÇ≈Ç∑: {type}");
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