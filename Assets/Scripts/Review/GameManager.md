# GameManager.cs コードレビュー

## 概要
ゲーム全体の進行を管理し、プレイヤーごとのUyopyonStateとPlayerActionDataを管理するクラス。

## SOLID原則の評価

### ✅ Single Responsibility Principle (SRP) - 単一責任の原則
**評価: 部分的に違反**

**問題点**:
- 複数の責任を持っている:
  1. ネットワークオブジェクトのスポーン管理
  2. Dictionaryによるプレイヤーデータの管理
  3. プレイヤー名の取得
  4. 投了フラグの管理
  5. ネットワークオブジェクトの登録/登録解除

**推奨改善**:
- `NetworkObjectSpawner`クラスを分離してスポーン処理を委譲
- `PlayerRegistry`クラスを分離してDictionary管理を委譲
- `SurrenderManager`クラスを分離して投了機能を委譲

### ⚠️ Open/Closed Principle (OCP) - 開放/閉鎖の原則
**評価: 概ね良好**

- 新しいプレイヤータイプを追加する場合、既存コードの修正が必要になる可能性がある
- Dictionaryの管理方法は拡張可能

### ✅ Liskov Substitution Principle (LSP) - リスコフの置換原則
**評価: 該当なし**

- 継承関係がないため、LSPは適用されない

### ✅ Interface Segregation Principle (ISP) - インターフェース分離の原則
**評価: 該当なし**

- インターフェースを使用していないため、ISPは適用されない

### ⚠️ Dependency Inversion Principle (DIP) - 依存性逆転の原則
**評価: 部分的に違反**

**問題点**:
- `FindFirstObjectByType<NetworkRunner>()`を直接呼び出している（99行目, 161行目）
- 具体的な実装に依存している

**推奨改善**:
- NetworkRunnerをコンストラクタインジェクションまたはプロパティインジェクションで注入
- インターフェースを導入して依存関係を逆転

## DRY原則の評価

### ⚠️ コードの重複
**評価: 重複あり**

**問題点**:
1. **Runnerのnullチェックと取得処理の重複**:
   - `SpawnUyopyon()` (99-107行目)
   - `SpawnPlayerActionData()` (159-167行目)
   - 同じパターンが2回繰り返されている

2. **プレハブのnullチェックパターンの重複**:
   - `SpawnUyopyon()` (110-114行目)
   - `SpawnPlayerActionData()` (170-174行目)

3. **Dictionary登録メソッドの類似パターン**:
   - `RegisterUyopyonState()` (235-254行目)
   - `RegisterPlayerActionData()` (260-279行目)
   - `RegisterNetworkPlayer()` (285-302行目)
   - 同じパターンが3回繰り返されている

**推奨改善**:
```csharp
// ヘルパーメソッドの追加
private NetworkRunner GetOrFindRunner()
{
    if (_runner == null)
    {
        _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner == null)
        {
            Debug.LogError("[GameManager] NetworkRunnerが見つかりません");
        }
    }
    return _runner;
}

// ジェネリックメソッドでDictionary登録を統一
private void RegisterPlayerData<T>(Dictionary<PlayerRef, T> dict, PlayerRef player, T data, string typeName) where T : class
{
    if (data == null)
    {
        Debug.LogWarning($"[GameManager] {typeName}がnullです");
        return;
    }
    
    if (dict.ContainsKey(player))
    {
        Debug.LogWarning($"[GameManager] Player {player} の {typeName} は既に登録されています");
        return;
    }
    
    dict.Add(player, data);
    Debug.Log($"[GameManager] Player {player} の {typeName} を登録しました。Total: {dict.Count}");
}
```

## その他の問題点

### 1. 未使用のフィールド
- `CurrentRound` (56行目): 宣言されているが使用されていない

### 2. パブリックプロパティの設計
- `uyopyonStateDict` (49行目): Dictionaryを直接公開している
  - 推奨: `IReadOnlyDictionary`を返すか、カスタムプロパティでラップ

### 3. エラーハンドリング
- `GetUyopyonState()` (200-217行目): nullを返しているが、呼び出し側でnullチェックが必要
  - 推奨: 例外をスローするか、`TryGet`パターンを使用

### 4. メモリ管理
- `OnDestroy()` (406-414行目): 適切にクリーンアップしている ✅
- Dictionaryのクリア処理が実装されている ✅

## 改善提案の優先度

### 🔴 高優先度
1. **Runner取得処理の重複削除**: ヘルパーメソッド化
2. **Dictionary登録処理の統一**: ジェネリックメソッド化
3. **未使用フィールドの削除**: `CurrentRound`の削除

### 🟡 中優先度
1. **責任の分離**: NetworkObjectSpawner、PlayerRegistry、SurrenderManagerへの分離
2. **依存性注入**: NetworkRunnerの注入

### 🟢 低優先度
1. **IReadOnlyDictionaryの使用**: カプセル化の強化
2. **TryGetパターンの導入**: nullチェックの改善

## 総合評価

**評価: B+**

**良い点**:
- メモリ管理が適切に実装されている
- 登録/登録解除の仕組みが整っている
- クリーンアップ処理が実装されている

**改善が必要な点**:
- 責任が多すぎる（SRP違反）
- コードの重複がある（DRY違反）
- 依存関係の管理が不十分（DIP違反）





