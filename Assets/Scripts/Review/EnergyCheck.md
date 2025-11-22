# EnergyCheck.cs コードレビュー

## 概要
元気チェック用のユーティリティクラス。行動選択時に元気が足りるかどうかをチェックする。

## SOLID原則の評価

### ✅ Single Responsibility Principle (SRP) - 単一責任の原則
**評価: 良好**

**良い点**:
- 元気チェックに集中している ✅
- 静的メソッドのみで実装されている ✅

### ✅ Open/Closed Principle (OCP) - 開放/閉鎖の原則
**評価: 良好**

- 新しい行動タイプを追加する場合、switch文の拡張のみで対応可能 ✅

### ✅ Liskov Substitution Principle (LSP) - リスコフの置換原則
**評価: 該当なし**

- 継承関係がないため、LSPは適用されない

### ✅ Interface Segregation Principle (ISP) - インターフェース分離の原則
**評価: 該当なし**

- インターフェースを使用していないため、ISPは適用されない

### ✅ Dependency Inversion Principle (DIP) - 依存性逆転の原則
**評価: 良好**

- パラメータとして依存関係を受け取っている ✅
- 静的メソッドのため、依存関係が明確 ✅

## DRY原則の評価

### ⚠️ コードの重複
**評価: 軽微な重複**

**問題点**:
1. **元気消費量取得の重複**:
   - `GetActionEnergyCostByType()` (150-169行目)
   - `GetSpecialAbilityEnergyCost()` (174-206行目)
   - `ActionCalculator`クラスと同様の実装が重複している可能性

2. **連続使用判定の重複**:
   - `CanPerformAction()` (22-68行目)
   - `CanUseSpecialAbility()` (82-130行目)
   - 同じパターンが繰り返されている

**推奨改善**:
```csharp
// ActionCalculatorのメソッドを再利用
private static int GetActionEnergyCostByType(ActionType actionType, GameParameters gameParams)
{
    // ActionCalculatorのメソッドを使用
    return ActionCalculator.GetBaseEnergy(actionType, gameParams);
}

// 連続使用判定の統一
private static bool IsConsecutiveAction(
    ActionType actionType,
    string specialAbilityName,
    bool isMorning,
    ActionData? morningAction,
    ActionData? previousAction)
{
    // ActionCalculator.IsConsecutiveAction()を使用
    // ただし、ActionCalculatorはPlayerActionDataを必要とするため、
    // このクラス用のオーバーロードが必要
}
```

## その他の問題点

### 1. デバッグログ
- `UnityEngine.Debug.Log()`が直接使用されている
- 推奨: `DebugLogger.Log()`を使用

### 2. メソッドの複雑さ
- `CanPerformAction()` (22-68行目): 複雑な条件分岐
- `CanUseSpecialAbility()` (82-130行目): 複雑な条件分岐
  - 推奨: より小さなメソッドに分割

### 3. ActionCalculatorとの重複
- `ActionCalculator`クラスと同様のロジックが実装されている
- 推奨: `ActionCalculator`のメソッドを再利用

## 改善提案の優先度

### 🔴 高優先度
1. **ActionCalculatorとの重複削除**: `ActionCalculator`のメソッドを再利用
2. **デバッグログの統一**: `DebugLogger`を使用

### 🟡 中優先度
1. **メソッドの分割**: 複雑なメソッドの分割

## 総合評価

**評価: B**

**良い点**:
- 責任が明確に分離されている ✅
- 静的メソッドで実装されている ✅
- 元気チェックロジックが適切 ✅

**改善が必要な点**:
- **ActionCalculatorとの重複**（DRY違反）
- デバッグログの統一
- メソッドの複雑さ





