# 選択フェーズタイマー UI実装計画書

## 概要

選択フェーズの制限時間（60秒）を視覚的に表示するUI要素を実装します。プレイヤーが残り時間を直感的に把握できるよう、シンプルなテキスト表示で残り時間を示します。

---

## 1. 現在の実装状況

### 1.1 タイマーロジック（既存）

| 項目 | 内容 | 実装箇所 |
|------|------|----------|
| タイマー値 | `SelectionTimeRemaining` (Networked int) | GameFlowManager.cs |
| 初期値 | 60秒 | GameParameters.SelectionPhaseTimeLimit |
| 更新頻度 | 60 ticks = 1秒 | GameFlowManager.FixedUpdateNetwork() |
| 警告タイミング | 残り10秒 | GameFlowManager.FixedUpdateNetwork() |
| タイムアウト処理 | 未選択の行動を「ねむる」に設定 | GameFlowManager.WaitForSelectionComplete() |

### 1.2 既存の通知方法

- ログエリアに「行動を選択してください（制限時間: 60秒）」と表示
- 残り10秒でログエリアに「残り10秒です！」と警告
- 音声（SE_TimerWarning、SE_TimeUp）は定義済みだが未実装

---

## 2. UI表示仕様

### 2.1 表示要素

タイマーUIは以下の要素で構成します：

| 要素名 | 種類 | 内容 | 備考 |
|--------|------|------|------|
| タイマーテキスト | TextMeshProUGUI | 「残り XX 秒」 | 数値表示 |
| 背景パネル | UI Image | タイマーの背景 | 視認性向上（optional） |

### 2.2 配置位置

**推奨配置**: 画面上部中央

```
┌──────────────────────────────────────┐
│           残り 45 秒                  │ <- タイマーUI
├──────────────────────────────────────┤
│                                      │
│   [プレイヤー情報]  [相手情報]        │
│                                      │
│        [行動ボタン]                  │
│                                      │
└──────────────────────────────────────┘
```

**代替案**: 行動ボタンパネルの上部

### 2.3 視覚効果

#### 色変化（残り時間に応じて）

| 残り時間 | テキスト色 |
|---------|-----------|
| 60-31秒 | 白色 (#FFFFFF) |
| 30-11秒 | 黄色 (#FFEB3B) |
| 10-1秒 | 赤色 (#F44336) |
| 0秒 | 灰色 |

#### アニメーション効果

| 残り時間 | アニメーション | 実装方法 |
|---------|--------------|---------|
| 60-31秒 | なし | - |
| 30-11秒 | テキスト拡大縮小（スケール 1.0 ↔ 1.1） | DOTween / Animation |
| 10-1秒 | 点滅（0.5秒間隔） + 拡大縮小 | DOTween / Animation |
| 0秒 | 「時間切れ！」表示 | テキスト変更 |

---

## 3. 実装方法

### 3.1 UI構造（Hierarchy）

```
Canvas
└── GameCanvas
    └── TimerPanel (新規作成)
        ├── BackgroundImage (背景、optional)
        └── TimerText (残り時間テキスト)
```

### 3.2 UIコンポーネント設定

#### TimerPanel
- **RectTransform**: Anchor = Top Center, Pivot = (0.5, 1.0)
- **Position**: (0, -50, 0) - 画面上部から50px下
- **Size**: (300, 60)

#### TimerText
- **Font**: TextMeshPro
- **Font Size**: 32
- **Alignment**: Center
- **Format**: "残り {time} 秒"

### 3.3 スクリプト実装

#### 新規スクリプト: `Assets/Scripts/UI/TimerDisplay.cs`

```csharp
using UnityEngine;
using TMPro;
using DG.Tweening; // optional: DOTween使用時

/// <summary>
/// 選択フェーズのタイマーを表示するUIコンポーネント（シンプルテキスト版）
/// </summary>
public class TimerDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Color Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.92f, 0.23f); // 黄
    [SerializeField] private Color dangerColor = new Color(0.96f, 0.26f, 0.21f); // 赤

    [Header("Animation Settings")]
    [SerializeField] private bool enablePulseAnimation = true;
    [SerializeField] private bool enableBlinkAnimation = true;

    private int _lastDisplayedTime = -1;
    private Tween _pulseTween;
    private Tween _blinkTween;
    private bool _warningPlayed = false;

    /// <summary>
    /// タイマー表示を更新
    /// </summary>
    /// <param name="remainingTime">残り時間（秒）</param>
    /// <param name="maxTime">最大時間（秒）- 未使用だが互換性のため保持</param>
    public void UpdateTimer(int remainingTime, int maxTime = 60)
    {
        if (remainingTime < 0) remainingTime = 0;

        // テキスト更新
        if (remainingTime > 0)
        {
            timerText.text = $"残り {remainingTime} 秒";
        }
        else
        {
            timerText.text = "時間切れ！";
        }

        // 色とアニメーション更新（1秒に1回のみ）
        if (_lastDisplayedTime != remainingTime)
        {
            _lastDisplayedTime = remainingTime;
            UpdateVisuals(remainingTime);
        }
    }

    /// <summary>
    /// 色とアニメーションを更新
    /// </summary>
    private void UpdateVisuals(int remainingTime)
    {
        // アニメーション停止
        StopAnimations();

        // 色変更
        if (remainingTime > 30)
        {
            // 通常（白）
            timerText.color = normalColor;
        }
        else if (remainingTime > 10)
        {
            // 警告（黄）
            timerText.color = warningColor;

            // パルスアニメーション開始
            if (enablePulseAnimation)
            {
                StartPulseAnimation();
            }
        }
        else if (remainingTime > 0)
        {
            // 危険（赤）
            timerText.color = dangerColor;

            // 点滅 + パルスアニメーション開始
            if (enableBlinkAnimation)
            {
                StartBlinkAnimation();
            }

            // 警告音再生（1回のみ）
            if (!_warningPlayed)
            {
                AudioManager.Instance?.PlayTimerWarningSE();
                _warningPlayed = true;
            }
        }
        else
        {
            // 時間切れ（灰色）
            timerText.color = Color.gray;

            // タイムアップ音再生
            AudioManager.Instance?.PlayTimeUpSE();
        }
    }

    /// <summary>
    /// パルスアニメーション（拡大縮小）
    /// </summary>
    private void StartPulseAnimation()
    {
        #if DOTWEEN_ENABLED
        _pulseTween = timerText.transform
            .DOScale(1.1f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
        #else
        // DOTweenなしの場合はAnimation Componentで代替
        #endif
    }

    /// <summary>
    /// 点滅アニメーション
    /// </summary>
    private void StartBlinkAnimation()
    {
        #if DOTWEEN_ENABLED
        _blinkTween = timerText
            .DOFade(0.3f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        _pulseTween = timerText.transform
            .DOScale(1.15f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
        #endif
    }

    /// <summary>
    /// アニメーション停止
    /// </summary>
    private void StopAnimations()
    {
        #if DOTWEEN_ENABLED
        _pulseTween?.Kill();
        _blinkTween?.Kill();
        #endif

        timerText.transform.localScale = Vector3.one;
        timerText.alpha = 1f;
    }

    /// <summary>
    /// タイマーパネルを表示/非表示
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        _warningPlayed = false;
        _lastDisplayedTime = -1;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        StopAnimations();
    }

    private void OnDestroy()
    {
        StopAnimations();
    }
}
```

### 3.4 UIControllerへの統合

#### UIController.cs への追加

```csharp
[Header("Timer UI")]
[SerializeField] private TimerDisplay timerDisplay;

/// <summary>
/// タイマー表示を更新
/// GameFlowManagerから呼び出される（RPCまたはRender）
/// </summary>
public void UpdateTimerDisplay(int remainingTime, int maxTime)
{
    if (timerDisplay != null)
    {
        timerDisplay.UpdateTimer(remainingTime, maxTime);
    }
}

/// <summary>
/// タイマーパネルを表示
/// </summary>
public void ShowTimer()
{
    if (timerDisplay != null)
    {
        timerDisplay.Show();
    }
}

/// <summary>
/// タイマーパネルを非表示
/// </summary>
public void HideTimer()
{
    if (timerDisplay != null)
    {
        timerDisplay.Hide();
    }
}
```

### 3.5 GameFlowManagerへの統合

#### GameFlowManager.cs への追加

```csharp
// 選択フェーズ開始時
private async UniTask StartSelectionPhase()
{
    // ...既存の処理...

    // タイマー表示を開始（全クライアント）
    RPC_ShowTimer();

    // ...既存の処理...
}

// 選択フェーズ終了時
private async UniTask StartExecutionPhase()
{
    // タイマー非表示（全クライアント）
    RPC_HideTimer();

    // ...既存の処理...
}

// タイマー表示RPC
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
private void RPC_ShowTimer()
{
    UIController.Instance?.ShowTimer();
}

// タイマー非表示RPC
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
private void RPC_HideTimer()
{
    UIController.Instance?.HideTimer();
}

// タイマー更新RPC（毎秒呼び出し）
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
private void RPC_UpdateTimer(int remainingTime)
{
    UIController.Instance?.UpdateTimerDisplay(remainingTime, gameParams.SelectionPhaseTimeLimit);
}

// FixedUpdateNetwork内のタイマー処理に追加
public override void FixedUpdateNetwork()
{
    // ...既存の処理...

    // 選択フェーズ中のタイマー処理
    if (CurrentPhase == GamePhase.Selection && _selectionPhaseActive)
    {
        _selectionPhaseTickCounter++;

        if (_selectionPhaseTickCounter >= 60)
        {
            _selectionPhaseTickCounter = 0;
            SelectionTimeRemaining--;

            // タイマーUI更新（全クライアント）
            RPC_UpdateTimer(SelectionTimeRemaining);

            // 残り時間が10秒になったら警告ログ
            if (SelectionTimeRemaining == 10)
            {
                RPC_AddLog("残り10秒です！");
            }
        }
    }
}
```

### 3.6 代替実装（RPCなし、Render使用）

RPCの代わりにRender()でタイマーを更新する方法：

```csharp
// GameFlowManager.cs

public override void Render()
{
    // 選択フェーズ中のみタイマーを更新
    if (CurrentPhase == GamePhase.Selection && _selectionPhaseActive)
    {
        UIController.Instance?.UpdateTimerDisplay(
            SelectionTimeRemaining,
            gameParams.SelectionPhaseTimeLimit
        );
    }
}
```

**メリット**: RPCコール削減、リアルタイム性向上
**デメリット**: 毎フレーム実行されるため、前回値との比較が必要

---

## 4. 実装手順

### Phase 1: UI作成
1. Unity EditorでTimerPanelのGameObject構造を作成
2. TextMeshProコンポーネントを設定
3. レイアウト調整（Anchor、Pivot、Position）

### Phase 2: スクリプト実装
1. `TimerDisplay.cs` を作成
2. UIController.csにタイマー関連メソッドを追加
3. GameFlowManager.csにRPCまたはRender処理を追加

### Phase 3: 接続と動作確認
1. TimerPanelにTimerDisplay.csをアタッチ
2. InspectorでUI参照を設定
3. UIController.timerDisplayにTimerDisplayを設定
4. テストプレイで動作確認

### Phase 4: ビジュアル調整
1. 色設定の調整
2. アニメーション速度の調整
3. 音声との連携確認

---

## 5. 技術仕様

### 5.1 依存関係

| 依存先 | 用途 | 必須/任意 |
|--------|------|----------|
| TextMeshPro | テキスト表示 | 必須 |
| DOTween | アニメーション | 任意（Animationで代替可） |
| AudioManager | 警告音再生 | 任意 |

### 5.2 ネットワーク同期

- **SelectionTimeRemaining**: [Networked]プロパティで自動同期
- **タイマーUI更新**: RPCまたはRender()で全クライアントに反映
- **音声再生**: 各クライアントでローカルに再生（同期不要）

### 5.3 パフォーマンス

| 項目 | 値 | 備考 |
|------|-----|------|
| 更新頻度 | 1秒に1回 | FixedUpdateNetworkの60 ticks |
| RPC頻度 | 1秒に1回 | または Render() で毎フレーム |
| UI再描画 | 1秒に1回 | SetDirty不要（自動） |

---

## 6. 拡張案

### 6.1 オプション機能

1. **サウンドオプション**
   - カウントダウン音（残り3秒で「3, 2, 1」の音声）
   - タイマー警告音の種類選択

2. **ビジュアルオプション**
   - パーティクル効果（残り時間が少ない時）
   - 画面全体の色変化（Vignette効果）

3. **アクセシビリティ**
   - タイマー表示のON/OFF設定
   - 点滅アニメーションの無効化オプション
   - 音声読み上げ（残り時間をナレーション）

### 6.2 採用デザイン

シンプルテキスト表示を採用します：

```
残り 45 秒
```

- 残り時間に応じて色が変化（白→黄→赤）
- 30秒以下でパルスアニメーション
- 10秒以下で点滅アニメーション
- 視認性を重視したシンプルなデザイン

---

## 7. 注意事項

### 7.1 WebGL対応
- アニメーションはWebGLでもパフォーマンス影響が少ない方法を選択
- DOTweenを使用する場合はWebGL Buildで動作確認

### 7.2 ネットワーク遅延
- SelectionTimeRemainingは[Networked]なのでホストとクライアントで若干ずれる可能性
- 視覚的に1秒程度のずれは許容範囲

### 7.3 UI重複防止
- タイマーパネルは選択フェーズのみ表示
- 他のフェーズでは非表示にする

### 7.4 アクセシビリティ
- 点滅アニメーションは視覚過敏のユーザーに配慮し、無効化オプションを検討
- 色だけでなく、テキストやアイコンでも状態を表現

---

## 8. テストケース

| No | テスト項目 | 期待結果 |
|----|-----------|---------|
| 1 | 選択フェーズ開始時 | タイマーが「残り 60 秒」で表示される |
| 2 | 1秒経過 | 「残り 59 秒」に更新される |
| 3 | 30秒経過 | テキストが黄色に変わる、パルスアニメーション開始 |
| 4 | 10秒経過 | テキストが赤色に変わる、点滅開始、警告音再生 |
| 5 | 0秒到達 | 「時間切れ！」表示、タイムアップ音再生 |
| 6 | 選択フェーズ終了 | タイマーパネルが非表示になる |
| 7 | ネットワーク同期 | ホストとクライアントで同じ時間が表示される |
| 8 | 複数ラウンド | 2日目以降も正常に60秒から開始される |

---

## 9. リリース判定基準

### 必須要件
- [ ] 残り時間が数値で表示される
- [ ] 残り時間に応じて色が変化する
- [ ] タイマーがネットワーク同期される
- [ ] 選択フェーズ以外では非表示になる

### 推奨要件
- [ ] アニメーション効果（パルス、点滅）
- [ ] 警告音・タイムアップ音の再生
- [ ] レスポンシブデザイン（画面サイズに対応）

### オプション要件
- [ ] パーティクル効果
- [ ] 設定でタイマー表示のON/OFF
- [ ] アクセシビリティ対応（点滅無効化など）
