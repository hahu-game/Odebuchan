# ActionCalculator.cs コードレビュー

## 概要
行動の効果を計算するユーティリティクラス。静的メソッドのみを持つ。

## SOLID原則の評価

### ✅ Single Responsibility Principle (SRP) - 単一責任の原則
**評価: 良好**

**良い点**:
- 行動効果の計算に集中している ✅
- 静的メソッドのみで実装されている ✅

### ✅ Open/Closed Principle (OCP) - 開放/閉鎖の原則
**評価: 良好**

- 新しい行動タイプを追加する場合、switch文の拡張のみで対応可能
- 拡張に開いている設計

### ✅ Liskov Substitution Principle (LSP) - リスコフの置換原則
**評価: 該当なし**

- 継承関係がないため、LSPは適用されない

### ✅ Interface Segregation Principle (ISP) - インターフェース分離の原則
**評価: 該当なし**

- インターフェースを使用していないため、ISPは適用されない

### ✅ Dependency Inversion Principle (DIP) - 依存性逆転の原則
**評価: 良好**

- パラメータとして依存関係を受け取っている ✅
- 静的メソッドのため、依存関係が明確

## DRY原則の評価

### ⚠️ コードの重複
**評価: 軽微な重複**

**問題点**:
1. **switch文の重複**:
   - `GetBaseEnergy()` (258-268行目)
   - `GetBaseWeight()` (270-277行目)
   - `GetSpecialAbilityBaseEnergy()` (279-292行目)
   - `GetSpecialAbilityBaseWeight()` (294-303行目)
   - 同じパターンが繰り返されている

2. **特殊能力名の文字列比較**:
   - `GetSpecialAbilityBaseEnergy()` (281-291行目)
   - `GetSpecialAbilityBaseWeight()` (296-302行目)
   - `IsConsecutiveAction()` (148-186行目)
   - 文字列比較が複数箇所で使用されている

**推奨改善**:
```csharp
// Enum比較の使用
private static int GetSpecialAbilityBaseEnergy(SpecialAbilityType abilityType, GameParameters gameParams)
{
    switch (abilityType)
    {
        case SpecialAbilityType.Gaishoku: return gameParams.GaishokuEnergyChange;
        case SpecialAbilityType.Kintre: return gameParams.KintreEnergyCost;
        // ...
    }
}

// 特殊能力名からEnumへの変換ヘルパー
private static SpecialAbilityType? ParseSpecialAbilityName(string abilityName)
{
    if (Enum.TryParse<SpecialAbilityType>(abilityName, out var result))
        return result;
    return null;
}
```

## その他の問題点

### 1. 文字列比較の使用
- `specialAbilityName == "Gaishoku"`等の文字列比較が多数使用されている
- 推奨: Enum比較を使用

### 2. マジックナンバー
- `0.5f` (200行目, 188行目): 定数化すべき
- `1.5f` (208行目): 定数化すべき

### 3. メソッドの可読性
- `IsConsecutiveAction()` (132-190行目): 複雑な条件分岐
  - 推奨: より小さなメソッドに分割

## 改善提案の優先度

### 🟡 中優先度
1. **文字列比較の改善**: Enum比較の使用
2. **マジックナンバーの定数化**: 定数クラスの作成
3. **メソッドの分割**: `IsConsecutiveAction()`の分割

### 🟢 低優先度
1. **switch文の統一**: 共通処理の抽出

## 総合評価

**評価: A-**

**良い点**:
- 責任が明確に分離されている ✅
- 静的メソッドで実装されている ✅
- 拡張可能な設計 ✅
- 計算ロジックが適切に実装されている ✅

**改善が必要な点**:
- 文字列比較の使用（軽微）
- マジックナンバー（軽微）
- メソッドの複雑さ（軽微）

**全体的に良好な設計です。**







