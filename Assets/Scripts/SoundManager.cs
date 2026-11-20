using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Source 割り当て")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioSource seAudioSource;

    [Header("オーディオクリップのマスターリスト（任意）")]
    [SerializeField] private AudioClip[] bgmClips;
    [SerializeField] private AudioClip[] seClips;

    private void Awake()
    {
        // シングルトンの重複防止＆シーン跨ぎ設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 保存されている音量をロードして適用
        LoadVolume();
    }

    /// <summary>
    /// BGMを再生する（クリップを直接指定する場合）
    /// </summary>
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmAudioSource == null || clip == null) return;

        if (bgmAudioSource.clip == clip && bgmAudioSource.isPlaying) return; // すでに同じ曲なら何もしない

        bgmAudioSource.clip = clip;
        bgmAudioSource.loop = loop;
        bgmAudioSource.Play();
    }

    /// <summary>
    /// SEを1回鳴らす（ワンショット）
    /// </summary>
    public void PlaySE(AudioClip clip)
    {
        if (seAudioSource == null || clip == null) return;
        seAudioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// BGMの音量を変更・保存する (0.0 ～ 1.0)
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        if (bgmAudioSource != null) bgmAudioSource.volume = volume;
        PlayerPrefs.SetFloat("BGM_Volume", volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// SEの音量を変更・保存する (0.0 ～ 1.0)
    /// </summary>
    public void SetSEVolume(float volume)
    {
        if (seAudioSource != null) seAudioSource.volume = volume;
        PlayerPrefs.SetFloat("SE_Volume", volume);
        PlayerPrefs.Save();
    }

    private void LoadVolume()
    {
        float bgmVol = PlayerPrefs.GetFloat("BGM_Volume", 1f);
        float seVol = PlayerPrefs.GetFloat("SE_Volume", 1f);

        if (bgmAudioSource != null) bgmAudioSource.volume = bgmVol;
        if (seAudioSource != null) seAudioSource.volume = seVol;
    }

    /// <summary>
    /// BGMをクリップ名（ファイル名）で指定して再生する
    /// </summary>
    public void PlayBGMByName(string clipName, bool loop = true)
    {
        AudioClip clip = System.Array.Find(bgmClips, c => c != null && c.name == clipName);
        if (clip != null)
        {
            PlayBGM(clip, loop);
        }
        else
        {
            Debug.LogWarning($"指定されたBGMが見つかりません: {clipName}");
        }
    }

    /// <summary>
    /// SEをクリップ名（ファイル名）で指定して再生する
    /// </summary>
    public void PlaySEByName(string clipName)
    {
        AudioClip clip = System.Array.Find(seClips, c => c != null && c.name == clipName);
        if (clip != null)
        {
            PlaySE(clip);
        }
        else
        {
            Debug.LogWarning($"指定されたSEが見つかりません: {clipName}");
        }
    }
}