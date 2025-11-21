using UnityEngine;
using System.Collections;

/// <summary>
/// BGMと効果音を管理するシングルトンクラス
/// シーンをまたいで存続する
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;

    [Header("BGM Clips")]
    public AudioClip titleBGM;
    public AudioClip gameBGM;
    public AudioClip resultBGM;

    [Header("SE Clips - Common")]
    public AudioClip seButtonClick;

    [Header("SE Clips - Title")]
    public AudioClip seMatchingStart;
    public AudioClip seMatchingSuccess;

    [Header("SE Clips - Game Phase")]
    public AudioClip sePhaseStart;
    public AudioClip seDayStart;
    public AudioClip seTimerWarning;
    public AudioClip seTimeUp;

    [Header("SE Clips - Action Selection")]
    public AudioClip seActionSelect;
    public AudioClip seActionConfirm;
    public AudioClip seActionClear;
    public AudioClip seActionReveal;

    [Header("SE Clips - Action Execution")]
    public AudioClip seEat;
    public AudioClip seSleep;
    public AudioClip sePlay;
    public AudioClip seClinic;
    public AudioClip seSpecialAbility;

    [Header("SE Clips - Status Change")]
    public AudioClip seEvolution;
    public AudioClip seAbilityChoice;
    public AudioClip seAbilitySelected;
    public AudioClip seSicknessOnset;
    public AudioClip seInjuryOnset;
    public AudioClip seAilmentCure;

    [Header("SE Clips - Game End")]
    public AudioClip seVictory;
    public AudioClip seDefeat;
    public AudioClip seDraw;
    public AudioClip seSurrender;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float seVolume = 0.7f;

    private void Awake()
    {
        // シングルトンパターン + DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // PlayerPrefsから音量設定を読み込む
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        seVolume = PlayerPrefs.GetFloat("SEVolume", 0.7f);

        // AudioSourceの初期設定
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
        }
        if (seSource != null)
        {
            seSource.volume = seVolume;
        }
    }

    #region BGM Methods

    /// <summary>
    /// BGMを再生する
    /// </summary>
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    /// <summary>
    /// BGMを停止する
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    /// <summary>
    /// BGMをフェードアウトして停止する
    /// </summary>
    public void FadeOutBGM(float duration = 1f)
    {
        StartCoroutine(FadeOutBGMCoroutine(duration));
    }

    private IEnumerator FadeOutBGMCoroutine(float duration)
    {
        float startVolume = bgmSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = bgmVolume;
    }

    // 便利メソッド
    public void PlayTitleBGM() => PlayBGM(titleBGM);
    public void PlayGameBGM() => PlayBGM(gameBGM);
    public void PlayResultBGM() => PlayBGM(resultBGM);

    #endregion

    #region SE Methods

    /// <summary>
    /// 効果音を再生する
    /// </summary>
    public void PlaySE(AudioClip clip)
    {
        if (seSource == null || clip == null) return;
        seSource.PlayOneShot(clip, seVolume);
    }

    // 便利メソッド - Common
    public void PlayButtonClickSE() => PlaySE(seButtonClick);

    // 便利メソッド - Title
    public void PlayMatchingStartSE() => PlaySE(seMatchingStart);
    public void PlayMatchingSuccessSE() => PlaySE(seMatchingSuccess);

    // 便利メソッド - Phase
    public void PlayPhaseStartSE() => PlaySE(sePhaseStart);
    public void PlayDayStartSE() => PlaySE(seDayStart);
    public void PlayTimerWarningSE() => PlaySE(seTimerWarning);
    public void PlayTimeUpSE() => PlaySE(seTimeUp);

    // 便利メソッド - Action Selection
    public void PlayActionSelectSE() => PlaySE(seActionSelect);
    public void PlayActionConfirmSE() => PlaySE(seActionConfirm);
    public void PlayActionClearSE() => PlaySE(seActionClear);
    public void PlayActionRevealSE() => PlaySE(seActionReveal);

    // 便利メソッド - Action Execution
    public void PlayEatSE() => PlaySE(seEat);
    public void PlaySleepSE() => PlaySE(seSleep);
    public void PlayPlaySE() => PlaySE(sePlay);
    public void PlayClinicSE() => PlaySE(seClinic);
    public void PlaySpecialAbilitySE() => PlaySE(seSpecialAbility);

    // 便利メソッド - Status Change
    public void PlayEvolutionSE() => PlaySE(seEvolution);
    public void PlayAbilityChoiceSE() => PlaySE(seAbilityChoice);
    public void PlayAbilitySelectedSE() => PlaySE(seAbilitySelected);
    public void PlaySicknessOnsetSE() => PlaySE(seSicknessOnset);
    public void PlayInjuryOnsetSE() => PlaySE(seInjuryOnset);
    public void PlayAilmentCureSE() => PlaySE(seAilmentCure);

    // 便利メソッド - Game End
    public void PlayVictorySE() => PlaySE(seVictory);
    public void PlayDefeatSE() => PlaySE(seDefeat);
    public void PlayDrawSE() => PlaySE(seDraw);
    public void PlaySurrenderSE() => PlaySE(seSurrender);

    #endregion

    #region Volume Control

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
    }

    public void SetSEVolume(float volume)
    {
        seVolume = Mathf.Clamp01(volume);
    }

    #endregion
}
