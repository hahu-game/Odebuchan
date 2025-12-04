# GameFlowManager.cs コードレビュー

## 概要
ゲーム全体のフェーズ管理を行うクラス。ホスト側でフェーズを管理し、全クライアントに同期する。

## SOLID原則の評価

### ⚠️ Single Responsibility Principle (SRP) - 単一責任の原則
**評価: 違反**

**問題点**:
- 複数の責任を持っている:
  1. フェーズ管理（Preparation, Selection, Execution, GameEnd）
  2. 特殊能力の選択管理
  3. 進化判定と処理
  4. 糖尿病・熱中症のチェック
  5. タイマー管理
  6. RPCによるUI更新指示
  7. プレイヤー名の取得

**推奨改善**:
- `PhaseManager`: フェーズ遷移の管理
- `EvolutionManager`: 進化判定と処理
- `StatusAilmentManager`: 状態異常のチェック
- `TimerManager`: タイマー管理
- `PlayerNameResolver`: プレイヤー名の取得

### ⚠️ Open/Closed Principle (OCP) - 開放/閉鎖の原則
**評価: 部分的に違反**

**問題点**:
- 新しいフェーズを追加する場合、`GameFlowManager`の修正が必要
- 特殊能力の選択ロジックがハードコードされている

**推奨改善**:
- フェーズ遷移をStrategyパターンで実装
- 特殊能力選択をStrategyパターンで実装

### ✅ Liskov Substitution Principle (LSP) - リスコフの置換原則
**評価: 該当なし**

- 継承関係がないため、LSPは適用されない

### ✅ Interface Segregation Principle (ISP) - インターフェース分離の原則
**評価: 該当なし**

- インターフェースを使用していないため、ISPは適用されない

### ⚠️ Dependency Inversion Principle (DIP) - 依存性逆転の原則
**評価: 部分的に違反**

**問題点**:
- `GameManager.Instance`に直接依存している
- `UIController.Instance`に直接依存している
- `TurnProcessor`への参照を直接保持している

**推奨改善**:
- インターフェースを導入して依存関係を逆転
- 依存性注入を使用

## DRY原則の評価

### ⚠️ コードの重複
**評価: 重複あり**

**問題点**:
1. **プレイヤー名取得の重複**:
   - `GetPlayerName()` (1181-1211行目)
   - `TurnProcessor.GetPlayerName()` と同様の実装
   - `GameFlowManager.GetPlayerName()` と同様の実装
   - 同じロジックが3箇所に存在

2. **数値フォーマット処理の重複**:
   - `FormatNumber()` (1216-1230行目)
   - `TurnProcessor.FormatNumber()` と同様の実装

3. **Object.IsValidチェックの重複**:
   - 複数箇所で同じパターンが繰り返されている（234行目, 249行目, 302行目, 335行目, 369行目, 437行目）

4. **RPC_AddLog呼び出しの重複**:
   - ログ追加処理が多数の箇所で繰り返されている

**推奨改善**:
```csharp
// プレイヤー名取得をGameManagerに統一
// 既にGameManager.GetPlayerName()が実装されているため、それを使用

// 数値フォーマットをユーティリティクラスに統一
public static class NumberFormatter
{
    public static string FormatNumber(int value)
    {
        if (value > 0) return $"<color=blue><b>+{value}</b></color>";
        else if (value < 0) return $"<color=red><b>{value}</b></color>";
        else return "±0";
    }
}

// Object.IsValidチェックのヘルパーメソッド
private bool IsObjectValid()
{
    return Object != null && Object.IsValid;
}
```

## その他の問題点

### 1. 長大なメソッド
- `CheckEvolution()` (884-1064行目): 180行以上の長大なメソッド
  - 推奨: 複数のメソッドに分割
    - `CollectEvolutionCandidates()`
    - `ProcessEvolutionSelection()`
    - `ApplyEvolutionResult()`

- `DetermineEvolutionOrder()` (1095-1159行目): 複雑なロジック
  - 推奨: より小さなメソッドに分割

### 2. マジックナンバー
- `60秒のタイムアウト` (1005行目): `gameParams.SelectionPhaseTimeLimit`を使用すべき
- `60ティック = 1秒` (137行目): コメントで説明されているが、定数化すべき

### 3. 非同期処理のエラーハンドリング
- `try-catch`ブロックは実装されているが、エラー後の復旧処理が不十分

### 4. リストのシャッフル処理
- `ShuffleList()` (1164-1176行目): 毎回`new System.Random()`を生成している
  - 推奨: クラスメンバーとして保持

## 改善提案の優先度

### 🔴 高優先度
1. **プレイヤー名取得の統一**: `GameManager.GetPlayerName()`を使用
2. **数値フォーマットの統一**: ユーティリティクラスに統一
3. **長大なメソッドの分割**: `CheckEvolution()`の分割

### 🟡 中優先度
1. **責任の分離**: PhaseManager、EvolutionManager等への分離
2. **マジックナンバーの定数化**: TickRate等の定数化
3. **Randomインスタンスの再利用**: クラスメンバーとして保持

### 🟢 低優先度
1. **Strategyパターンの導入**: フェーズ遷移の拡張性向上
2. **依存性注入**: インターフェースの導入

## 総合評価

**評価: B**

**良い点**:
- 非同期処理が適切に実装されている
- エラーハンドリングが実装されている
- RPCによるネットワーク同期が適切

**改善が必要な点**:
- 責任が多すぎる（SRP違反）
- コードの重複が多い（DRY違反）
- 長大なメソッドが存在
- マジックナンバーが存在







