# AI_REQUIREMENTS.md

このファイルは、REQUIREMENTS.mdの内容をAI（Claude Code）が実装しやすいように整理した技術仕様書です。

## 1. ゲームシステム概要

### 1.1 プロジェクト構成
- **Unity Version**: 6000.2.7f2
- **Networking**: Photon Fusion 2.0.7 (Shared Mode)
- **Build Target**: WebGL (Unity Room向け)
- **Players**: 1 vs 1
- **Game Type**: ターン制ボードゲーム風の育成対戦ゲーム

### 1.2 ゲーム目的
自分の「うーぴょん（Uyopyon）」を相手より早く目標の重さ（700kg）まで育てることで勝利する。

### 1.3 ゲームの流れ
```
ゲーム開始
  ↓
準備フェーズ（1日目開始、進化判定）
  ↓
選択フェーズ（60秒制限、午前・午後の行動を選択）
  ↓
実行フェーズ（午前→午後の順で処理）
  ├─ ジャンケン判定と効果適用
  ├─ 行動実行とアニメーション
  └─ 勝利判定
  ↓
次の日へ（準備フェーズに戻る）
  または
ゲーム終了（勝利条件達成）
```

### 1.4 勝利条件
実行フェーズの午前または午後の処理後に、いずれかのプレイヤーの重さが**700kg**に到達した時点で、そのプレイヤーの勝利。

---

## 2. データ構造設計

### 2.1 必要なクラス・コンポーネント

#### 2.1.1 ゲーム管理系
- **GameFlowManager** (NetworkBehaviour)
  - ゲーム全体のフェーズ管理（準備→選択→実行のステートマシン）
  - `[Networked] int CurrentDay` - 現在の日数
  - `[Networked] GamePhase CurrentPhase` - 現在のフェーズ
  - ゲーム開始・終了処理

- **TurnProcessor** (NetworkBehaviour)
  - 1日の処理を管理
  - 午前・午後の行動実行ロジック
  - ジャンケン判定と効果適用

#### 2.1.2 プレイヤーデータ系
- **UyopyonState** (NetworkBehaviour) - 既存のクラスを拡張
  - `[Networked] int Weight` - 重さ（初期値: 1kg）
  - `[Networked] int Energy` - 元気（初期値: 100HP）
  - `[Networked] NetworkArray<byte> StatusAilments` - 状態異常フラグ
  - `[Networked] int PlayBuffWeight` - あそぶによる重さバフ（初期値: 0）
  - `[Networked] int PlayBuffEnergy` - あそぶによる元気バフ（初期値: 0）
  - `[Networked] int StudyCombo` - べんきょう連続回数（初期値: 0）
  - `[Networked] NetworkString<_16> SpecialAbilityName` - 選択した特殊能力
  - `[Networked] bool HasEvolved` - 進化済みフラグ
  - `[Networked] NetworkString<_16> VisualType` - "UyopyonBaby" or "UyopyonChild"

- **ActionData** (構造体)
  - `ActionType Type` - 行動の種類（Eat, Sleep, Play, Clinic, SpecialAbility）
  - `Genre Genre` - ジャンル（Rock, Scissors, Paper, None）
  - プレイヤーの選択した午前・午後の行動を保持

#### 2.1.3 列挙型
```csharp
public enum GamePhase { Preparation, Selection, Execution, GameEnd }
public enum ActionType { Eat, Sleep, Play, Clinic, SpecialAbility }
public enum Genre { Rock, Scissors, Paper, None }
public enum SpecialAbilityType { Gaishoku, Kintre, Gamushara, Benkyou, Jukusui, Dokagui }
public enum StatusAilment { SleepApnea, Diabetes, BackPain, Heatstroke }
```

### 2.2 ScriptableObject設計

#### GameParameters (ScriptableObject)
全てのゲームパラメーターを1つのScriptableObjectで管理し、Inspector編集可能にする。

```csharp
[CreateAssetMenu(fileName = "GameParameters", menuName = "Odebuchan/GameParameters")]
public class GameParameters : ScriptableObject
{
    [Header("基本設定")]
    public int InitialWeight = 1;
    public int InitialEnergy = 100;
    public int EvolutionWeightThreshold = 200;
    public int VictoryWeightThreshold = 700;

    [Header("行動パラメーター")]
    public int EatEnergyChange = -15;
    public int EatWeightChange = 50;
    public int SleepEnergyChange = 50;
    public int PlayEnergyChange = -30;
    public int ClinicEnergyChange = -20;
    public int PlayWeightBuffIncrement = 10;
    public int PlayEnergyBuffIncrement = 10; // ※修正：元は「初期値」だったが「増加量」が正しい

    [Header("状態異常")]
    public float EatSicknessProbabilityPerWeight = 0.1f; // 重さ÷10 = %
    public float PlayInjuryProbabilityPerWeight = 0.1f; // 重さ÷10 = %
    public float SicknessSleepApneaProbability = 0.5f;
    public float SicknessDiabetesProbability = 0.5f;
    public float InjuryBackPainProbability = 0.5f;
    public float InjuryHeatstrokeProbability = 0.5f;

    [Header("ジャンケン効果")]
    public int JankenWinEnergyBase = 10;
    public int JankenLoseEnergyBase = -10;
    public int JankenDayIncrement = 5; // 1日ごとに±5増減

    [Header("特殊能力")]
    public int GamushuraEnergyCost = -40;
    public float GamushuraWeightMultiplier = 1.2f;
    public float GamushuraEnergyMultiplier = 1.2f;
    public int GamusharaMixWeightBuff = 15;
    public int GamusharaMixEnergyBuff = 15;
    // ... 他の特殊能力パラメーター

    [Header("タイミング設定")]
    public float BlackoutDuration = 1f;
    public float ActionWaitDuration = 2f;
    public int SelectionPhaseTimeLimit = 60;
}
```

---

## 3. フェーズ管理システム

### 3.1 GamePhaseの状態遷移

```
Preparation (準備フェーズ)
├─ BlackoutPanel表示「〇日目」
├─ 前日の午後の行動を表示エリアに反映
├─ 進化判定・処理（200kg到達時）
└─ 糖尿病の朝処理（重さ-20, 元気-20）
  ↓
Selection (選択フェーズ)
├─ 操作ロック解除
├─ 午前・午後の行動選択（60秒制限）
├─ 両プレイヤーが確定ボタン押下で次へ
└─ タイムアウト時は選択中の行動で確定（未選択なら「ねむる」）
  ↓
Execution (実行フェーズ)
├─ 「行動開始！」表示
├─ 相手の選択を表示（2秒待機）
├─ 午前のジャンケン判定・効果適用
├─ 午前の行動実行（2秒待機）
├─ 勝利判定
├─ 午後のジャンケン判定・効果適用
├─ 午後の行動実行（2秒待機）
└─ 勝利判定
  ↓ （勝利条件未達成）
次の日の準備フェーズへ
  または
  ↓ （勝利条件達成）
GameEnd (ゲーム終了)
├─ 勝利/敗北アニメーション（4秒）
└─ ResultPanel表示
```

### 3.2 フェーズ管理の実装方針

**GameFlowManager**がホスト側でフェーズを管理し、`[Networked] GamePhase CurrentPhase`で全クライアントに同期する。各フェーズの処理は`FixedUpdateNetwork()`または`RPC`を使用して同期的に実行する。

---

## 4. 行動システム

### 4.1 基本行動（4種類）

#### 4.1.1 たべる (Eat)
- **ジャンル**: Rock（グー）
- **効果**:
  - 元気: `-15 - (連続使用ペナルティ)`
  - 重さ: `+50 + PlayBuffWeight`
  - 病気発症確率: `(現在の重さ ÷ 10)%`
    - 50%で「睡眠時無呼吸症候群」
    - 50%で「糖尿病」

#### 4.1.2 ねむる (Sleep)
- **ジャンル**: Paper（パー）
- **効果**:
  - 元気: `+50 + PlayBuffEnergy + (睡眠時無呼吸症候群なら-20) - (連続使用ペナルティ)`
  - 重さ: 変化なし
- **特殊ルール**:
  - 他の行動を選択していても、元気が足りなければ自動的に「ねむる」に変更される

#### 4.1.3 あそぶ (Play)
- **ジャンル**: Scissors（チョキ）
- **効果**:
  - 元気: `-30 - (連続使用ペナルティ)`
  - 重さ: 変化なし
  - **永久バフ**: `PlayBuffWeight += 10`, `PlayBuffEnergy += 10`
  - ケガ発症確率: `(現在の重さ ÷ 10)%`
    - 50%で「腰痛」
    - 50%で「熱中症」

#### 4.1.4 つういん (Clinic)
- **ジャンル**: Scissors（チョキ）
- **効果**:
  - 元気: `-20 - (連続使用ペナルティ)`
  - 重さ: 変化なし
  - **状態異常解除**: 全ての状態異常をクリア

#### 連続使用ペナルティ
前日の午後と今日の午前が同じ行動、または今日の午前と今日の午後が同じ行動の場合、元気が`-20`追加で減少する。

### 4.2 特殊能力（6種類から3種類がランダム選出）

#### 4.2.1 がいしょく (Gaishoku)
- **ジャンル**: Paper（パー）
- **効果**: 「たべる」と同じだが、ジャンルがパー

#### 4.2.2 きんとれ (Kintre)
- **ジャンル**: Scissors（チョキ）
- **効果**:
  - 元気: `-120`
  - 重さ: `現在の重さ × 0.5`（半分になる）
  - **永久バフ倍率アップ**: 今後の重さ・元気増加量が1.5倍になる

#### 4.2.3 がむしゃら (Gamushara)
- **ジャンル**: None（なし）
- **効果**: 元気`-40`消費して、以下の4パターンから25%ずつでランダム選択
  1. **重さ増加**: たべるの重さ増加 × 1.2倍
  2. **元気増加**: ねむるの元気増加 × 1.2倍
  3. **効果量増加**: PlayBuffWeight += 15, PlayBuffEnergy += 15
  4. **MIX**: 上記1～3の効果を全て同時発動（倍率: 重さ×0, 元気×0.5, バフ各+15）
     - ※MIXは「たべる、ねむる、あそぶの効果を弱めて全部発動」という意味
     - 重さ増加なし、元気は0.5倍（約+25）、バフは+15ずつ

#### 4.2.4 べんきょう (Benkyou)
- **ジャンル**: Scissors（チョキ）
- **効果**:
  - 元気: `-25`
  - **永久バフ増加**: `PlayBuffWeight += 10`（※あそぶと同じバフを増加）
  - **連続ボーナス**: `StudyCombo += 1`, 次回べんきょう時に`PlayBuffWeight += (StudyCombo × 10)`
  - べんきょう以外の行動をすると`StudyCombo = 0`にリセット

#### 4.2.5 じゅくすい (Jukusui)
- **ジャンル**: Paper（パー）
- **選択条件**: 自分と相手の朝時点の重さ合計が**奇数**
- **効果**:
  - 元気: `+50 + 20` = +70（ねむるの効果 + ボーナス）

#### 4.2.6 どかぐい (Dokagui)
- **ジャンル**: Rock（グー）
- **選択条件**: 自分と相手の朝時点の重さ合計が**偶数**
- **効果**:
  - 元気: `-15`
  - 重さ: `+50 + 30` = +80（たべるの効果 + ボーナス）

---

## 5. ジャンケンシステム

### 5.1 勝敗判定
午前と午後それぞれで、両プレイヤーの行動のジャンルでジャンケン判定を行う。

- **Paper（パー） > Rock（グー）**
- **Rock（グー） > Scissors（チョキ）**
- **Scissors（チョキ） > Paper（パー）**
- **None（なし）**が含まれる場合、ジャンケン判定なし
- **あいこ**の場合、ジャンケン効果なし

### 5.2 ジャンケン効果

#### 5.2.1 元気の増減
- **勝者**: `+10 + (CurrentDay - 1) × 5`
- **敗者**: `-10 - (CurrentDay - 1) × 5`

例: 3日目の場合、勝者+20、敗者-20

#### 5.2.2 バフ・デバフ効果

| 勝ちパターン | 勝者の効果 | 敗者の効果 |
|------------|----------|----------|
| Paper > Rock | 元気 +10 | 重さ -10 |
| Rock > Scissors | 重さ +10 | PlayBuffWeight -10, PlayBuffEnergy -10 |
| Scissors > Paper | PlayBuffWeight +10, PlayBuffEnergy +10 | 元気 -10 |

#### 5.2.3 ログ表示例
- **グー > チョキ**: 「{勝者}のうーぴょんは独り占めしてたくさん食べた！重さ+10。{敗者}のうーぴょんの好きな食べ物を取られて悲しい！たべる時の重さ-10、ねむる時の元気-10」
- **チョキ > パー**: 「{勝者}のうーぴょんはのびのびと遊んだ！たべる時の重さ+10、ねむる時の元気+10。{敗者}のうーぴょんは騒音で眠りが浅かった！元気-10」
- **パー > グー**: 「{勝者}のうーぴょんはぐっすり眠った！元気+10。{敗者}のうーぴょんはぼっち飯で少し寂しい！重さ-10」

### 5.3 処理順序
1. ジャンケン判定
2. エフェクト・効果音再生（2秒待機）
3. 元気・重さ・バフの値を反映
4. UI更新
5. 行動の実行へ

---

## 6. 状態異常システム

### 6.1 状態異常の種類と効果

#### 6.1.1 睡眠時無呼吸症候群 (SleepApnea)
- **発症条件**: たべる実行時に病気判定で50%
- **効果**: ねむる実行時の元気回復量 `-20`

#### 6.1.2 糖尿病 (Diabetes)
- **発症条件**: たべる実行時に病気判定で50%
- **効果**: 毎朝（準備フェーズ開始時）に重さ`-20`、元気`-20`

#### 6.1.3 腰痛 (BackPain)
- **発症条件**: あそぶ実行時にケガ判定で50%
- **効果**: 選択した行動が30%の確率でキャンセルされ、自動的に「ねむる」になる

#### 6.1.4 熱中症 (Heatstroke)
- **発症条件**: あそぶ実行時にケガ判定で50%
- **効果**:
  - 午前に発症した場合: 今日の午後が強制的に「つういん」になる
  - 午後に発症した場合: 翌日の準備フェーズ時に午前が自動的に「つういん」にセットされ、変更不可

### 6.2 状態異常の重複ルール
- **同じ状態異常は重複しない**
- **異なる状態異常は同時にかかる**

例: すでに睡眠時無呼吸症候群の状態で再度病気になった場合、必ず糖尿病になる。

### 6.3 実装方法
`NetworkArray<byte> StatusAilments`（サイズ4）で各状態異常のフラグ管理。
```csharp
StatusAilments[0] = SleepApnea ? 1 : 0;
StatusAilments[1] = Diabetes ? 1 : 0;
StatusAilments[2] = BackPain ? 1 : 0;
StatusAilments[3] = Heatstroke ? 1 : 0;
```

---

## 7. ネットワーク同期設計

### 7.1 同期が必要なデータ

#### GameFlowManager（Host Authority）
- `[Networked] int CurrentDay`
- `[Networked] GamePhase CurrentPhase`
- `[Networked] NetworkArray<SpecialAbilityType> AvailableSpecialAbilities`（サイズ3）

#### UyopyonState（Input Authority）
- `[Networked] int Weight`
- `[Networked] int Energy`
- `[Networked] NetworkArray<byte> StatusAilments`
- `[Networked] int PlayBuffWeight`
- `[Networked] int PlayBuffEnergy`
- `[Networked] int StudyCombo`
- `[Networked] NetworkString<_16> SpecialAbilityName`
- `[Networked] bool HasEvolved`
- `[Networked] NetworkString<_16> VisualType`

#### PlayerActionData（Input Authority）
- `[Networked] ActionType MorningAction`
- `[Networked] ActionType AfternoonAction`
- `[Networked] bool IsActionFixed` - 確定ボタンが押されたか

### 7.2 RPC設計

#### [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
- `RPC_SubmitActions(ActionType morning, ActionType afternoon)` - 行動選択の送信

#### [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
- `RPC_StartPreparationPhase(int day)` - 準備フェーズ開始
- `RPC_ShowBlackout(string text)` - 暗転表示
- `RPC_ShowJankenResult(PlayerRef winner, Genre winGenre)` - ジャンケン結果表示
- `RPC_PlayActionAnimation(PlayerRef player, ActionType action)` - 行動アニメーション再生
- `RPC_GameEnd(PlayerRef winner)` - ゲーム終了処理

### 7.3 ログの同期方法
`[Networked, Capacity(100)] NetworkLinkedList<NetworkString<_128>> GameLogs`を使用するか、RPCで各クライアントにログ追加を指示する。

---

## 8. UI設計

### 8.1 UI要素の一覧と責務

#### 8.1.1 行動選択ボタン（4個）
- **EatButton**, **SleepButton**, **PlayButton**, **ClinicButton**
- 責務: 行動選択、ホバー時に効果詳細を表示
- 子オブジェクト: 元気増減Text, 重さ増減Text, 状態異常確率Text, 説明Panel

#### 8.1.2 特殊能力関連（3個）
- **BeforeSpecialActionText**: 「〇〇kgで進化すると...」（進化前のみ表示）
- **SpecialActionButton**: 進化後に選択した特殊能力ボタン
- **SpecialActionChoicePanel**: 進化時に3つの選択肢を表示するオーバーレイ

#### 8.1.3 ステータス表示（2個）
- **MyStatusPanel**: 自分のプレイヤー名、元気、重さ、状態異常
- **OppStatusPanel**: 相手のプレイヤー名、元気、重さ、状態異常、各行動時の増減予測

#### 8.1.4 うーぴょん画像（2個）
- **MyUyopyonImage**: 自分のうーぴょん（重さに応じてscale変更、アニメーション再生）
- **OppUyopyonImage**: 相手のうーぴょん（重さに応じてscale変更、アニメーション再生）

#### 8.1.5 行動表示パネル（6個）
- **MyYesterdayAfternoonActionPanel**: 前日午後の自分の行動
- **OppYesterdayAfternoonActionPanel**: 前日午後の相手の行動
- **MyTodayMorningActionPanel**: 今日午前の自分の行動（選択中は太枠）
- **OppTodayMorningActionPanel**: 今日午前の相手の行動
- **MyTodayAfternoonActionPanel**: 今日午後の自分の行動（選択中は太枠）
- **OppTodayAfternoonActionPanel**: 今日午後の相手の行動

#### 8.1.6 操作ボタン（4個）
- **FixButton**: 行動確定ボタン
- **ClearButton**: 選択クリアボタン
- **SettingButton**: 設定パネル表示
- **BackResultButton**: リザルト画面に戻る（ゲーム終了後のみ表示）

#### 8.1.7 パネル・オーバーレイ（4個）
- **SettingPanel**: 音量調整、投了ボタン
- **LogArea**: ScrollViewでログ表示（RPCで同期）
- **BlackoutPanel**: 暗転演出用（「ゲームスタート！」「1日目」など）
- **ResultPanel**: リザルト画面（勝敗、最終ステータス、再戦/タイトルボタン）

### 8.2 UI更新のタイミング

| UI要素 | 更新タイミング | 更新方法 |
|--------|------------|---------|
| StatusPanel | UyopyonState.Render() | Networkedプロパティの変化を検知 |
| ActionPanel | 選択フェーズ・実行フェーズ | ボタンクリック・RPC受信 |
| LogArea | 各処理後 | RPC経由でログ追加 |
| UyopyonImage | 行動実行時・重さ変化時 | アニメーション再生、scale調整 |

---

## 9. パラメーター定義（ScriptableObject）

### 9.1 基本パラメーター
```csharp
public int InitialWeight = 1;
public int InitialEnergy = 100;
public int EvolutionWeightThreshold = 200;
public int VictoryWeightThreshold = 700;
```

### 9.2 状態異常確率
```csharp
public float SicknessProbabilityPerWeight = 0.1f; // 重さ÷10 = %
public float SicknessSleepApneaProbability = 0.5f;
public float SicknessDiabetesProbability = 0.5f;
public float InjuryProbabilityPerWeight = 0.1f;
public float InjuryBackPainProbability = 0.5f;
public float InjuryHeatstrokeProbability = 0.5f;
```

### 9.3 行動パラメーター
```csharp
// 連続使用ペナルティ
public int ConsecutiveActionPenalty = 20;

// たべる
public int EatEnergyChange = -15;
public int EatWeightChange = 50;

// ねむる
public int SleepEnergyChange = 50;

// あそぶ
public int PlayEnergyChange = -30;
public int PlayWeightBuffIncrement = 10; // ※修正済み
public int PlayEnergyBuffIncrement = 10; // ※修正済み

// つういん
public int ClinicEnergyChange = -20;
```

### 9.4 特殊能力パラメーター
```csharp
// がむしゃら
public int GamushuraEnergyCost = -40;
public float GamushuraWeightMultiplier = 1.2f;
public float GamushuraEnergyMultiplier = 1.2f;
public int GamushuraBuffWeightIncrement = 15;
public int GamushuraBuffEnergyIncrement = 15;
public float GamusharaMixWeightMultiplier = 0f;
public float GamusharaMixEnergyMultiplier = 0.5f;
public int GamusharaMixBuffWeightIncrement = 15;
public int GamusharaMixBuffEnergyIncrement = 15;

// べんきょう
public int BenkyouEnergyCost = -25;
public int BenkyouBuffWeightIncrement = 10; // ※あそぶと同じバフを増加
public int BenkyouComboBuffIncrement = 10; // 連続ボーナス

// じゅくすい
public int JukusuiEnergyBonus = 20; // ねむるの効果に加えて

// どかぐい
public int DokaguiEnergyCost = -15;
public int DokaguiWeightBonus = 30; // たべるの効果に加えて

// きんとれ
public float KintreWeightMultiplier = 0.5f; // 現在の重さを半分に
public int KintreEnergyCost = -120;
public float KintreBuffMultiplier = 1.5f; // 以降の増加量が1.5倍
```

### 9.5 状態異常パラメーター
```csharp
public int SleepApneaRecoveryReduction = -20;
public int DiabetesMorningWeightChange = -20;
public int DiabetesMorningEnergyChange = -20;
public float BackPainCancelProbability = 0.3f;
public float HeatstrokeForcedClinicProbability = 1.0f;
```

### 9.6 ジャンケンパラメーター
```csharp
public int JankenWinEnergyBase = 10;
public int JankenLoseEnergyBase = -10;
public int JankenEnergyIncrementPerDay = 5;
public int JankenBuffDebuffAmount = 10; // 勝敗時のバフ・デバフ増減量
```

### 9.7 タイミング設定
```csharp
public float BlackoutDuration = 1f;
public float ActionWaitDuration = 2f;
public int SelectionPhaseTimeLimit = 60;
```

### 9.8 その他
```csharp
public int CurrentDay = 1;
public int TotalSpecialAbilities = 6;
public int SelectedSpecialAbilitiesCount = 3;
```

---

## 10. 実装上の注意点

### 10.1 拡張性の考慮
- **キャラクター追加対応**: うーぴょん以外のキャラクターを後から追加できるよう、画像・アニメーションをScriptableObjectまたはPrefabで管理する
- 見た目のみが変わり、ゲームロジックは共通

### 10.2 数値表示ルール
- **正の値**: 青色・太字（例: `<color=blue><b>+50</b></color>`）
- **負の値**: 赤色・太字（例: `<color=red><b>-15</b></color>`）
- **例外**: MyStatusPanelとOppStatusPanelの数値は色をつけない

### 10.3 小数の扱い
全ての計算で小数が発生した場合、**切り捨て**（`Mathf.FloorToInt()`）を使用する。

### 10.4 オーバーレイの排他制御
オーバーレイ（SpecialActionChoicePanel, SettingPanel, ResultPanelなど）が表示されている間は、オーバーレイ上のボタン以外の操作を不可にする。

### 10.5 進化処理の競合回避
2人同時に進化条件を満たした場合:
1. 重さが大きい方が先に選択
2. 重さが同値なら元気が大きい方が先
3. 元気も同値ならランダムで先に選ぶプレイヤーを決定
4. 先に選んだプレイヤーの選択が完了するまで、もう一方のプレイヤーは待機
5. 選ばれた特殊能力は候補から除外され、後のプレイヤーは選べない

### 10.6 BGM・SE設定
- **BGM**: ゲーム進行中、ゲーム終了時の2種類
- **SE**: 進化時、技実行時（各行動ごと）、クリック音、行動書き換え音、ケガ・病気発症音、デバフ音、ゲーム開始・終了時

全て後から設定可能なように、AudioClipをScriptableObjectまたはManagerクラスで管理する。

### 10.7 ログ同期の設計
RPCまたはNetworked Listを使用し、両プレイヤーの画面に同じログが同じ順序で表示されるようにする。

### 10.8 元気不足時の自動ねむる処理
選択フェーズ終了時（確定ボタン押下時）に、選択した行動を実行するための元気が足りない場合、その行動を自動的に「ねむる」に変更する。

### 10.9 熱中症による強制つういん処理
- 午前に熱中症発症 → 今日の午後を強制的に「つういん」に変更
- 午後に熱中症発症 → 翌日の準備フェーズ開始時に午前を「つういん」に自動セット、変更不可フラグを立てる

### 10.10 べんきょうの連続ボーナス管理
`StudyCombo`カウンターを管理し、べんきょう以外の行動を選択した時点で0にリセットする。

---

## 11. 処理フローの詳細

### 11.1 ゲーム開始処理
```
1. BlackoutPanel表示「ゲームスタート！」（1秒）
2. 特殊能力6種から3種をランダム選出（ホスト側で決定、同期）
3. 設定ボタン以外の操作ロック
4. BGM・SE再生
5. ログエリア「ゲームスタート！」
6. 準備フェーズへ移行
```

### 11.2 準備フェーズ処理
```
1. BlackoutPanel表示「〇日目」（1秒）
2. 前日の午後の行動をYesterdayAfternoonActionPanelに反映（初日は非表示）
3. 糖尿病チェック → 重さ-20、元気-20
4. 進化判定（Weight >= 200 && !HasEvolved）
   a. 2人同時進化の場合、優先順位決定
   b. SpecialActionChoicePanelを表示（優先順位順）
   c. 選択完了後、SpecialAbilityName・HasEvolved・VisualTypeを更新
5. ログエリア「〇日目の行動を選択してください。」
6. 選択フェーズへ移行
```

### 11.3 選択フェーズ処理
```
1. 操作ロック解除
2. MyTodayMorningActionPanelを太枠にする
3. タイマー開始（60秒）
4. プレイヤーが行動選択ボタンをクリック
   a. 午前が選択されていなければ午前にセット、太枠を午後に移動
   b. 午前が選択済みなら午後にセット
5. クリアボタン → 午前・午後をリセット、午前を太枠に
6. 確定ボタン押下
   a. 午前・午後が両方選択されているか確認
   b. 選択されていなければエラーメッセージ
   c. 元気不足チェック → 自動的に「ねむる」に変更
   d. IsActionFixed = true, RPC_SubmitActions()でホストに送信
7. 両プレイヤーがIsActionFixed = trueになったら実行フェーズへ移行
8. タイムアウト（60秒経過）
   a. 未選択の行動は「ねむる」に自動設定
   b. IsActionFixed = true
```

### 11.4 実行フェーズ処理（午前）
```
1. BlackoutPanel表示「行動開始！」（1秒）
2. ログエリア「〇日目の行動を開始します。」
3. 相手の選択をOppTodayMorningActionPanelに反映（2秒待機）
4. ログエリア「{相手名}は午前に{行動名}、午後に{行動名}を選択しました。」
5. 相手の行動による自分の行動への影響を反映（該当する場合のみ）
6. BlackoutPanel表示「午前の行動！」（1秒）
7. ジャンケン判定
   a. 勝敗判定
   b. エフェクト・効果音再生
   c. ログエリアに勝敗結果表示
   d. 元気・重さ・バフの増減反映（2秒待機）
8. 午前の行動を同時実行
   a. MyUyopyonImageとOppUyopyonImageのアニメーション再生
   b. 行動の効果適用（重さ・元気・バフ・状態異常）
   c. ログエリアに行動結果表示（2秒待機）
9. 勝利判定（Weight >= 700）
   a. 達成していればゲーム終了処理へ
   b. 未達成なら午後の処理へ
```

### 11.5 実行フェーズ処理（午後）
```
（午前と同様の処理を午後の行動で実行）
1. BlackoutPanel表示「午後の行動！」（1秒）
2. ジャンケン判定・効果適用
3. 午後の行動実行
4. 勝利判定
   a. 達成していればゲーム終了処理へ
   b. 未達成なら次の日の準備フェーズへ
```

### 11.6 ゲーム終了処理
```
1. 勝者側のうーぴょんで勝利アニメーション再生
2. 敗者側のうーぴょんで敗北アニメーション再生
3. BGM・SE変更（ゲーム終了時のBGM・SE）
4. ログエリア「ゲーム終了！{勝者名}のうーぴょんが目標の重さに到達しました！」
5. 4秒待機
6. ResultPanel表示
   a. 勝者名、終了日数・午前/午後
   b. 両プレイヤーの最終重さ・元気・特殊能力
   c. 再戦ボタン、タイトルに戻るボタン、最終盤面表示ボタン
7. 操作ロック解除
```

---

## 12. デバッグ・テスト用機能

### 12.1 推奨するデバッグ機能
- **Inspector上でパラメーター即時変更**: ScriptableObjectを使用することで実現
- **シード値固定モード**: ランダム処理のシード値をEditor上で指定可能にする
- **ログ詳細モード**: 各処理の詳細な内部計算をログに出力
- **フェーズスキップ機能**: Editor上で特定フェーズにジャンプ

### 12.2 エラーハンドリング
- ネットワーク切断時の処理（投了扱い、またはリザルト画面に遷移）
- パラメーター異常値のチェック（Inspector上で警告表示）

---

## まとめ

この仕様書は、REQUIREMENTS.mdの内容をAIが実装する際に参照しやすいように、以下の観点で整理しました：

1. **データ構造の明確化**: クラス、列挙型、ScriptableObjectの設計
2. **処理フローの体系化**: フェーズ管理、行動実行、ジャンケン処理の流れ
3. **ネットワーク同期の設計**: Networkedプロパティ、RPCの使用方法
4. **パラメーター管理の一元化**: 全61個のパラメーターをScriptableObjectで管理
5. **実装上の注意点の強調**: 拡張性、数値表示、競合回避など

実装時は、この仕様書とCLAUDE.mdを併せて参照し、Photon Fusion 2.0.7の機能を活用してください。
