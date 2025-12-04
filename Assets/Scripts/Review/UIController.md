# UIController.cs コードレビュー

## 概要
ゲーム内の全てのUI要素を管理し、ネットワーク同期されたデータに基づいて表示を更新するクラス。

## SOLID原則の評価

### ❌ Single Responsibility Principle (SRP) - 単一責任の原則
**評価: 重大な違反**

**問題点**:
- 非常に多くの責任を持っている（2950行以上の巨大なクラス）:
  1. UI要素の参照管理（100以上のpublicフィールド）
  2. 行動選択の処理
  3. ログ管理
  4. ブラックアウト表示
  5. 特殊能力選択UI
  6. リザルト画面表示
  7. 状態異常表示
  8. 行動効果予測表示
  9. ツールチップ表示
  10. プレイヤー名の取得

**推奨改善**:
- `ActionSelectionController`: 行動選択の処理
- `LogController`: ログ管理
- `BlackoutController`: ブラックアウト表示
- `SpecialAbilityUIController`: 特殊能力選択UI
- `ResultScreenController`: リザルト画面
- `StatusAilmentDisplayController`: 状態異常表示
- `ActionEffectDisplayController`: 行動効果予測表示
- `TooltipController`: ツールチップ表示

### ⚠️ Open/Closed Principle (OCP) - 開放/閉鎖の原則
**評価: 部分的に違反**

**問題点**:
- 新しいUI要素を追加する場合、既存コードの修正が必要
- switch文が多数存在

**推奨改善**:
- StrategyパターンでUI更新処理を実装
- Commandパターンでボタン処理を実装

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
- `GameFlowManager.Instance`に直接依存している
- `FindFirstObjectByType<NetworkRunner>()`を直接呼び出している

**推奨改善**:
- インターフェースを導入して依存関係を逆転
- 依存性注入を使用

## DRY原則の評価

### ❌ コードの重複
**評価: 重大な重複**

**問題点**:
1. **特殊能力ボタンの設定処理の重複**:
   - `Start()` (263-292行目): 6つのボタンに対して同じパターンが繰り返されている
   ```csharp
   if (gaishokuButton != null)
   {
       gaishokuButton.onClick.RemoveAllListeners();
       gaishokuButton.onClick.AddListener(() => OnSpecialAbilityButtonClicked(SpecialAbilityType.Gaishoku));
   }
   // 同じパターンが6回繰り返されている
   ```

2. **特殊能力ボタン更新メソッドの重複**:
   - `UpdateGaishokuButton()`, `UpdateKintreButton()`, `UpdateBenkyouButton()`等
   - 同じパターンが6回繰り返されている

3. **プレイヤー名取得の重複**:
   - `GetPlayerName()` (2444行目付近)
   - `GameManager.GetPlayerName()`が既に存在するのに使用していない

4. **テキスト更新処理の重複**:
   - `SetTextIfChanged()`が実装されているが、全ての箇所で使用されていない
   - 80箇所以上で`text`プロパティへの直接代入が行われている

5. **行動表示更新の重複**:
   - `UpdateActionDisplay()`が複数の条件分岐で呼ばれている
   - 同じパターンが繰り返されている

**推奨改善**:
```csharp
// 特殊能力ボタンの設定を統一
private void SetupSpecialAbilityButtons()
{
    var buttonConfigs = new[]
    {
        (gaishokuButton, SpecialAbilityType.Gaishoku),
        (kintreButton, SpecialAbilityType.Kintre),
        (gamusharaButton, SpecialAbilityType.Gamushara),
        (benkyouButton, SpecialAbilityType.Benkyou),
        (jukusuiButton, SpecialAbilityType.Jukusui),
        (dokaguiButton, SpecialAbilityType.Dokagui)
    };
    
    foreach (var (button, abilityType) in buttonConfigs)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnSpecialAbilityButtonClicked(abilityType));
        }
    }
}

// 特殊能力ボタン更新の統一
private void UpdateSpecialAbilityButton<T>(
    T buttonConfig,
    UyopyonState state,
    PlayerActionData actionData,
    GameParameters gameParams,
    bool isMorning,
    ActionData? localMorningAction) where T : ISpecialAbilityButtonConfig
{
    // 共通の更新処理
}

// プレイヤー名取得をGameManagerに統一
private string GetPlayerName(PlayerRef player)
{
    return GameManager.Instance?.GetPlayerName(player) ?? $"Player{player.PlayerId}";
}
```

## その他の問題点

### 1. クラスサイズ
- **2950行以上**: 非常に大きすぎる
- 推奨: 10以上のクラスに分割

### 2. パブリックフィールドの多さ
- **100以上のpublicフィールド**: Inspectorで設定するため必要だが、管理が困難
- 推奨: グループ化された構造体やクラスにまとめる

### 3. メソッドの長さ
- 多くのメソッドが100行以上
- 推奨: より小さなメソッドに分割

### 4. 条件分岐の複雑さ
- 多数のif文がネストされている
- 推奨: 早期リターンパターンの使用

### 5. マジックナンバー
- `100` (203行目): `maxLogEntries`のデフォルト値として定義されているが、他の箇所でも使用されている可能性

## 改善提案の優先度

### 🔴 高優先度
1. **クラスの分割**: 10以上のクラスに分割
2. **特殊能力ボタン処理の統一**: ループ処理への変更
3. **プレイヤー名取得の統一**: `GameManager.GetPlayerName()`を使用
4. **テキスト更新の統一**: `SetTextIfChanged()`の徹底的な使用

### 🟡 中優先度
1. **責任の分離**: 各UI機能を独立したクラスに分離
2. **Strategyパターンの導入**: UI更新処理の拡張性向上
3. **依存性注入**: インターフェースの導入

### 🟢 低優先度
1. **Commandパターンの導入**: ボタン処理の拡張性向上
2. **Observerパターンの導入**: UI更新の効率化

## 総合評価

**評価: C**

**良い点**:
- `SetTextIfChanged()`ヘルパーメソッドが実装されている ✅
- ログ管理の最適化が実装されている ✅
- コルーチンの重複実行防止が実装されている ✅

**改善が必要な点**:
- **クラスサイズが大きすぎる**（SRP重大違反）
- **コードの重複が多い**（DRY違反）
- **責任が多すぎる**（SRP違反）
- **パブリックフィールドが多すぎる**

**緊急度: 高**
このクラスは最も優先的にリファクタリングが必要です。







