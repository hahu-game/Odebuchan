# TurnProcessor.cs コードレビュー

## 概要
1日の処理（午前・午後の行動実行）を管理するクラス。ホスト側でのみ実行される。

## SOLID原則の評価

### ⚠️ Single Responsibility Principle (SRP) - 単一責任の原則
**評価: 部分的に違反**

**問題点**:
- 複数の責任を持っている:
  1. 行動実行の管理
  2. ジャンケン判定
  3. 連続使用ペナルティのチェック
  4. 状態異常の発症判定（病気・ケガ）
  5. 状態異常の効果適用（腰痛・熱中症）
  6. 勝利判定
  7. ログ出力
  8. プレイヤー名の取得

**推奨改善**:
- `ActionExecutor`: 行動実行の管理
- `JankenJudge`: ジャンケン判定
- `StatusAilmentManager`: 状態異常の管理
- `VictoryChecker`: 勝利判定
- `LogService`: ログ出力

### ✅ Open/Closed Principle (OCP) - 開放/閉鎖の原則
**評価: 概ね良好**

- `SpecialAbilityExecutor`に特殊能力の処理を委譲している ✅
- 新しい行動タイプを追加する場合、switch文の修正が必要

### ✅ Liskov Substitution Principle (LSP) - リスコフの置換原則
**評価: 該当なし**

- 継承関係がないため、LSPは適用されない

### ✅ Interface Segregation Principle (ISP) - インターフェース分離の原則
**評価: 該当なし**

- インターフェースを使用していないため、ISPは適用されない

### ⚠️ Dependency Inversion Principle (DIP) - 依存性逆転の原則
**評価: 部分的に違反**

**問題点**:
- `GameFlowManager.Instance`に直接依存している
- `GameManager.Instance`に直接依存している
- `UIController.Instance`に直接依存している

**推奨改善**:
- インターフェースを導入して依存関係を逆転
- 依存性注入を使用

## DRY原則の評価

### ⚠️ コードの重複
**評価: 重複あり**

**問題点**:
1. **プレイヤー名取得の重複**:
   - `GetPlayerName()` (347-377行目)
   - `GameFlowManager.GetPlayerName()` と同様の実装
   - `GameManager.GetPlayerName()` が既に存在するのに使用していない

2. **数値フォーマット処理の重複**:
   - `FormatNumber()` (382-396行目)
   - `GameFlowManager.FormatNumber()` と同様の実装

3. **ProcessMorningとProcessAfternoonの類似構造**:
   - 同じパターンが繰り返されている（65-98行目 vs 103-141行目）
   - ジャンケン判定、連続使用ペナルティ、行動実行の順序が同じ

4. **状態異常発症判定の類似パターン**:
   - `CheckSickness()` (662-731行目)
   - `CheckInjury()` (739-839行目)
   - 同じパターンが繰り返されている

**推奨改善**:
```csharp
// プレイヤー名取得をGameManagerに統一
private string GetPlayerName(PlayerRef player)
{
    return GameManager.Instance?.GetPlayerName(player) ?? $"Player{player.PlayerId}";
}

// 数値フォーマットをユーティリティクラスに統一
// NumberFormatter.FormatNumber()を使用

// 午前・午後の処理を統一
private async Task ProcessTimeSlot(
    Dictionary<PlayerRef, PlayerActionData> playerActions,
    bool isMorning,
    Func<PlayerActionData, ActionData> getAction,
    Func<PlayerActionData, ActionType> getPreviousAction)
{
    var players = playerActions.Keys.ToList();
    if (players.Count != 2) return;
    
    PlayerRef p1 = players[0];
    PlayerRef p2 = players[1];
    
    ActionData p1Action = getAction(playerActions[p1]);
    ActionData p2Action = getAction(playerActions[p2]);
    
    await ProcessJanken(p1, p2, p1Action.Genre, p2Action.Genre);
    CheckConsecutivePenalty(p1, p1Action.Type, getPreviousAction(playerActions[p1]));
    CheckConsecutivePenalty(p2, p2Action.Type, getPreviousAction(playerActions[p2]));
    await ExecuteAction(p1, p1Action, isMorning);
    await ExecuteAction(p2, p2Action, isMorning);
    await CheckVictory(isMorning);
}

// 状態異常発症判定の統一
private void CheckAilment(
    PlayerRef player,
    int weight,
    bool isSickness,
    StatusAilment ailment1,
    StatusAilment ailment2,
    Action<PlayerRef, StatusAilment> onAilmentOccurred)
{
    float chance = weight * (isSickness 
        ? gameParams.SicknessProbabilityPerWeight 
        : gameParams.InjuryProbabilityPerWeight);
    
    if (UnityEngine.Random.Range(0f, 100f) < chance)
    {
        UyopyonState state = GetUyopyonState(player);
        if (state == null) return;
        
        // 既存の状態異常をチェック
        bool hasAilment1 = state.HasStatusAilment(ailment1);
        bool hasAilment2 = state.HasStatusAilment(ailment2);
        
        StatusAilment newAilment;
        if (hasAilment1 && hasAilment2) return;
        else if (hasAilment1) newAilment = ailment2;
        else if (hasAilment2) newAilment = ailment1;
        else newAilment = UnityEngine.Random.Range(0f, 1f) < 0.5f ? ailment1 : ailment2;
        
        state.SetStatusAilment(newAilment, true);
        onAilmentOccurred(player, newAilment);
    }
}
```

## その他の問題点

### 1. 長大なメソッド
- `ExecuteAction()` (201-265行目): 複数の責任を持っている
- `CheckVictory()` (888-980行目): 複雑なロジック

### 2. マジックナンバー
- `2人` (71行目, 109行目, 894行目): 定数化すべき
- `30%` (861行目): `gameParams.BackPainCancelProbability`を使用すべき

### 3. 文字列比較
- `action.SpecialAbilityName.ToString() == "Benkyou"` (226行目): Enum比較を使用すべき

### 4. エラーハンドリング
- nullチェックは実装されているが、エラー後の処理が不十分な箇所がある

## 改善提案の優先度

### 🔴 高優先度
1. **プレイヤー名取得の統一**: `GameManager.GetPlayerName()`を使用
2. **数値フォーマットの統一**: ユーティリティクラスに統一
3. **午前・午後処理の統一**: 共通メソッドの作成

### 🟡 中優先度
1. **責任の分離**: ActionExecutor、JankenJudge等への分離
2. **状態異常判定の統一**: 共通メソッドの作成
3. **マジックナンバーの定数化**: 定数クラスの作成

### 🟢 低優先度
1. **依存性注入**: インターフェースの導入
2. **文字列比較の改善**: Enum比較の使用

## 総合評価

**評価: B**

**良い点**:
- `SpecialAbilityExecutor`に処理を委譲している ✅
- 非同期処理が適切に実装されている ✅
- RPCによるネットワーク同期が適切 ✅

**改善が必要な点**:
- 責任が多すぎる（SRP違反）
- コードの重複が多い（DRY違反）
- 長大なメソッドが存在
- マジックナンバーが存在



