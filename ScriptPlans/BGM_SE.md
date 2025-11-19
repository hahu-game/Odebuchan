# BGM・効果音 導入計画書

## 概要

本ドキュメントでは、Odebuchanゲームに導入するBGMと効果音（SE）の一覧、および導入手順を記載します。

---

## 1. BGM一覧

| No | BGM名 | 再生箇所 | ループ | 備考 |
|----|-------|----------|--------|------|
| 1 | TitleBGM | タイトル画面 | Yes | TitleScene読み込み時に再生開始 |
| 2 | GameBGM | ゲーム画面 | Yes | GameScene読み込み時に再生開始 |
| 3 | ResultBGM | リザルト画面 | No | ゲーム終了時に再生 |

---

## 2. 効果音一覧

### 2.1 共通SE

| No | SE名 | 再生タイミング | 関連スクリプト | 関連メソッド |
|----|------|----------------|----------------|--------------|
| 1 | SE_ButtonClick | 各種ボタンクリック時 | UIController.cs | 各ボタンのonClickリスナー |
| 2 | SE_ButtonHover | ボタンホバー時（任意） | - | EventTriggerで設定 |

### 2.2 タイトル画面SE

| No | SE名 | 再生タイミング | 関連スクリプト | 関連メソッド |
|----|------|----------------|----------------|--------------|
| 1 | SE_MatchingStart | マッチング開始時 | NetworkRunnerHandler.cs | StartGame() |
| 2 | SE_MatchingSuccess | マッチング成功時（2人揃った） | NetworkRunnerHandler.cs | OnPlayerJoined() |

### 2.3 ゲーム画面SE

#### フェーズ関連

| No | SE名 | 再生タイミング | 関連スクリプト | 関連メソッド |
|----|------|----------------|----------------|--------------|
| 1 | SE_PhaseStart | 各フェーズ開始時 | GameFlowManager.cs | StartPreparationPhase(), StartSelectionPhase(), StartExecutionPhase() |
| 2 | SE_DayStart | 新しい日の開始時 | GameFlowManager.cs | StartPreparationPhase() |
| 3 | SE_TimerWarning | 残り時間警告（10秒以下） | UIController.cs | Update()内のタイマー更新 |
| 4 | SE_TimeUp | 時間切れ | GameFlowManager.cs | WaitForSelectionComplete() |

#### 行動選択関連

| No | SE名 | 再生タイミング | 関連スクリプト | 関連メソッド |
|----|------|----------------|----------------|--------------|
| 1 | SE_ActionSelect | 行動ボタン選択時 | UIController.cs | OnActionButtonClicked() |
| 2 | SE_ActionConfirm | 確定ボタン押下時 | UIController.cs | OnFixButtonClicked() |
| 3 | SE_ActionClear | クリアボタン押下時 | UIController.cs | OnClearButtonClicked() |
| 4 | SE_ActionReveal | 行動公開時 | GameFlowManager.cs | RPC_RevealAllActions() |

#### 行動実行関連

| No | SE名 | 再生タイミング | 関連スクリプト | 関連メソッド |
|----|------|----------------|----------------|--------------|
| 1 | SE_Eat | たべる実行時 | TurnProcessor.cs | ExecuteEat() |
| 2 | SE_Sleep | ねむる実行時 | TurnProcessor.cs | ExecuteSleep() |
| 3 | SE_Play | あそぶ実行時 | TurnProcessor.cs | ExecutePlay() |
| 4 | SE_Clinic | つういん実行時 | TurnProcessor.cs | ExecuteClinic() |
| 5 | SE_SpecialAbility | 特殊能力実行時 | TurnProcessor.cs | ExecuteSpecialAbility() |

#### 状態変化関連

| No | SE名 | 再生タイミング | 関連スクリプト | 関連メソッド |
|----|------|----------------|----------------|--------------|
| 1 | SE_Evolution | 進化時 | GameFlowManager.cs | CheckEvolution() |
| 2 | SE_AbilityChoice | 特殊能力選択画面表示時 | GameFlowManager.cs | RPC_ShowSpecialAbilityChoice() |
| 3 | SE_AbilitySelected | 特殊能力選択完了時 | UIController.cs | OnAbilityChoicePanelButtonClicked() |
| 4 | SE_SicknessOnset | 病気発症時 | TurnProcessor.cs | CheckStatusAilments() |
| 5 | SE_InjuryOnset | ケガ発症時 | TurnProcessor.cs | CheckStatusAilments() |
| 6 | SE_AilmentCure | 状態異常回復時 | TurnProcessor.cs | ExecuteClinic() |

#### ゲーム終了関連

| No | SE名 | 再生タイミング | 関連スクリプト | 関連メソッド |
|----|------|----------------|----------------|--------------|
| 1 | SE_Victory | 勝利決定時 | GameFlowManager.cs | EndGame() |
| 2 | SE_Defeat | 敗北決定時 | GameFlowManager.cs | EndGame() |
| 3 | SE_Draw | 引き分け決定時 | GameFlowManager.cs | EndGame() |
| 4 | SE_Surrender | 降参時 | GameFlowManager.cs | RPC_Surrender() |

---

## 3. 導入手順

### 3.1 AudioManagerの作成

#### 新規スクリプト: `Assets/Scripts/Audio/AudioManager.cs`

```csharp
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
    public void PlayResultBGM() => PlayBGM(resultBGM, false);

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
```

### 3.2 ヒエラルキー設定

#### TitleSceneの設定

1. **AudioManager GameObject の作成**
   - 空のGameObjectを作成し、「AudioManager」と命名
   - AudioManager.csをアタッチ
   - 子オブジェクトとして2つのGameObjectを作成：
     - 「BGMSource」（AudioSource付き）
     - 「SESource」（AudioSource付き）
   - AudioManagerのInspectorでbgmSourceとseSourceを設定

2. **BGM自動再生の設定**
   - TitleScreenManagerのStart()またはAwake()でBGM再生を呼び出す

#### GameSceneの設定

- AudioManagerはDontDestroyOnLoadで引き継がれるため、追加設定不要
- GameFlowManagerのSpawned()でGameBGMに切り替え

### 3.3 各スクリプトへの実装例

#### TitleScreenManager.cs への追加

```csharp
private void Start()
{
    // タイトルBGM再生
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.PlayTitleBGM();
    }
}

public void OnPlayButtonClicked()
{
    // 効果音再生
    AudioManager.Instance?.PlayButtonClickSE();

    // 既存の処理...
}
```

#### NetworkRunnerHandler.cs への追加

```csharp
public async Task StartGame(GameMode mode, string sessionName)
{
    // マッチング開始SE
    AudioManager.Instance?.PlayMatchingStartSE();

    // 既存の処理...
}

public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
{
    // 2人揃ったらマッチング成功SE
    if (runner.SessionInfo.PlayerCount == 2)
    {
        AudioManager.Instance?.PlayMatchingSuccessSE();
    }

    // 既存の処理...
}
```

#### GameFlowManager.cs への追加

```csharp
public override void Spawned()
{
    // ゲームBGMに切り替え
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.FadeOutBGM(0.5f);
        StartCoroutine(PlayGameBGMDelayed(0.5f));
    }

    // 既存の処理...
}

private IEnumerator PlayGameBGMDelayed(float delay)
{
    yield return new WaitForSeconds(delay);
    AudioManager.Instance?.PlayGameBGM();
}

private async UniTask StartPreparationPhase()
{
    // 日の開始SE
    AudioManager.Instance?.PlayDayStartSE();

    // 既存の処理...
}

private async UniTask CheckEvolution()
{
    // 進化SE
    AudioManager.Instance?.PlayEvolutionSE();

    // 既存の処理...
}

private void EndGame(PlayerRef winner)
{
    // BGMを停止してリザルトBGM再生
    AudioManager.Instance?.StopBGM();
    AudioManager.Instance?.PlayResultBGM();

    var runner = Object.Runner;
    if (winner == runner.LocalPlayer)
    {
        AudioManager.Instance?.PlayVictorySE();
    }
    else
    {
        AudioManager.Instance?.PlayDefeatSE();
    }

    // 既存の処理...
}
```

#### UIController.cs への追加

```csharp
private void OnActionButtonClicked(ActionType actionType)
{
    // 行動選択SE
    AudioManager.Instance?.PlayActionSelectSE();

    // 既存の処理...
}

public void OnFixButtonClicked()
{
    // 確定SE
    AudioManager.Instance?.PlayActionConfirmSE();

    // 既存の処理...
}

public void OnClearButtonClicked()
{
    // クリアSE
    AudioManager.Instance?.PlayActionClearSE();

    // 既存の処理...
}
```

### 3.4 音声ファイルの配置

```
Assets/
└── Audio/
    ├── BGM/
    │   ├── TitleBGM.mp3
    │   ├── GameBGM.mp3
    │   └── ResultBGM.mp3
    └── SE/
        ├── Common/
        │   └── ButtonClick.wav
        ├── Title/
        │   ├── MatchingStart.wav
        │   └── MatchingSuccess.wav
        ├── Game/
        │   ├── PhaseStart.wav
        │   ├── DayStart.wav
        │   ├── TimerWarning.wav
        │   ├── TimeUp.wav
        │   ├── ActionSelect.wav
        │   ├── ActionConfirm.wav
        │   ├── ActionClear.wav
        │   ├── ActionReveal.wav
        │   ├── Eat.wav
        │   ├── Sleep.wav
        │   ├── Play.wav
        │   ├── Clinic.wav
        │   ├── SpecialAbility.wav
        │   ├── Evolution.wav
        │   ├── AbilityChoice.wav
        │   ├── AbilitySelected.wav
        │   ├── SicknessOnset.wav
        │   ├── InjuryOnset.wav
        │   └── AilmentCure.wav
        └── Result/
            ├── Victory.wav
            ├── Defeat.wav
            ├── Draw.wav
            └── Surrender.wav
```

### 3.5 音声ファイルのImport設定

#### BGMファイル（.mp3/.ogg）
- Load Type: Streaming（メモリ節約）
- Compression Format: Vorbis
- Quality: 70-100%

#### SEファイル（.wav/.ogg）
- Load Type: Decompress On Load（低遅延）
- Compression Format: PCM or ADPCM
- Force To Mono: Yes（ステレオ不要の場合）

---

## 4. 優先度

### 高優先度（必須）
- タイトルBGM
- ゲームBGM
- リザルトBGM
- ボタンクリックSE
- 行動選択/確定/クリアSE
- 勝利/敗北/引き分けSE

### 中優先度
- マッチング成功SE
- 進化SE
- 状態異常発症SE
- フェーズ開始SE
- 特殊能力選択SE

### 低優先度（任意）
- 個別行動実行SE（たべる、ねむる等）
- タイマー警告SE
- ホバーSE

---

## 5. 注意事項

1. **ネットワーク同期**: SEは各クライアントでローカルに再生する。RPCで同期する必要はない。

2. **WebGL対応**: WebGLではユーザー操作なしに音声を自動再生できない。タイトル画面でのクリック後にBGMを開始する。

3. **音量バランス**: BGMとSEの音量バランスをテストで調整する。

4. **重複再生防止**: 同じSEが短時間に連続再生されないよう、必要に応じてクールダウンを設ける。

5. **Null Check**: AudioManager.Instanceのnullチェックを必ず行う（シングルトン未初期化対策）。
