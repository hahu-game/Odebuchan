# メモリ使用量削減対策 - Odebuchanプロジェクト

このドキュメントは、Odebuchanプロジェクトにおけるメモリ使用量を削減するための対策をまとめたものです。

## 既に実装済みの対策

### ✅ 1. ログエントリの最大数制限
- **場所**: `Assets/Scripts/UI/UIController.cs`
- **問題**: ログエントリが無制限に蓄積されていた
- **対策**: `maxLogEntries`（デフォルト100件）を設定し、超過時に古いエントリを自動削除
- **状態**: 実装済み

### ✅ 2. コルーチンの重複実行防止
- **場所**: `Assets/Scripts/UI/UIController.cs`
- **問題**: `ScrollToBottom()`コルーチンが重複実行されていた
- **対策**: `_scrollToBottomCoroutine`参照を保持し、新しいコルーチン開始前に既存のコルーチンを停止
- **状態**: 実装済み

### ✅ 3. ゲーム開始時のログクリア
- **場所**: `Assets/Scripts/UI/UIController.cs`
- **問題**: 前回のログが残っていた
- **対策**: `Start()`で`ClearLog()`を呼び出し
- **状態**: 実装済み

### ✅ 4. FindObjectsByTypeの頻繁な使用によるパフォーマンス問題
- **場所**: `UIController.cs`, `TurnProcessor.cs`, `GameFlowManager.cs`
- **問題**: `FindObjectsByType<PlayerActionData>()`や`FindObjectsByType<NetworkPlayer>()`が頻繁に呼ばれていた
- **対策**:
  - GameManagerにNetworkPlayerのDictionaryを追加（`_networkPlayers`）
  - `RegisterNetworkPlayer()`, `GetNetworkPlayer()`, `GetPlayerName()`メソッドを追加
  - NetworkPlayer.Spawned()でGameManagerに自動登録
  - UIController.csの`OnFixButtonClicked()`, `OnClearButtonClicked()`, `GetPlayerName()`を最適化
  - TurnProcessor.csとGameFlowManager.csの`GetPlayerName()`を最適化
- **状態**: 実装済み

### ✅ 5. Render()メソッドでの毎フレーム処理の最適化
- **場所**: `UyopyonState.cs`, `PlayerActionData.cs`
- **問題**: 位置チェックが毎フレーム実行され、FindFirstObjectByTypeも毎フレーム呼ばれていた
- **対策**:
  - UyopyonState.Render()から位置チェックのループを削除
  - PlayerActionData.Render()のFindFirstObjectByType<NetworkRunner>()をRunnerプロパティに変更
- **状態**: 実装済み

### ✅ 6. TextMeshProのメッシュ再生成の最適化
- **場所**: `Assets/Scripts/UI/UIController.cs`
- **問題**: テキストが変更されていない場合でも更新処理が実行されていた
- **対策**:
  - `SetTextIfChanged()`ヘルパーメソッドを追加
  - `UpdateWeightDisplay()`, `UpdateEnergyDisplay()`, `UpdateActionEffectDisplay()`を最適化
  - テキストが実際に変更された場合のみ代入するよう修正
- **状態**: 実装済み

---

## 未実装の対策（優先度順）

### 🔴 優先度: 高

（全て実装済み）

### ✅ 7. デバッグログの条件付きコンパイル対応
- **場所**: 全スクリプト（特にRender()を持つクラス）
- **問題**: `Debug.Log()`が頻繁に呼ばれ、本番環境でもログ出力が続いていた
- **対策**:
  - `DebugLogger`ユーティリティクラスを作成（`Assets/Scripts/Utility/DebugLogger.cs`）
  - `[Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]`属性で本番ビルドでは無効化
  - ActionCalculator.cs, PlayerActionData.cs, UyopyonState.cs, NetworkPlayer.csのDebug.LogをDebugLoggerに置き換え
- **状態**: 実装済み

### ✅ 8. Dictionaryの適切なクリーンアップ
- **場所**: `GameManager.cs`, `UyopyonState.cs`, `PlayerActionData.cs`, `NetworkPlayer.cs`
- **問題**: Dictionaryがゲーム終了時やDespawn時にクリアされていなかった
- **対策**:
  - GameManagerに`ClearAllDictionaries()`メソッドを追加
  - `UnregisterUyopyonState()`, `UnregisterPlayerActionData()`, `UnregisterNetworkPlayer()`メソッドを追加
  - GameManager.OnDestroy()で全Dictionaryをクリア
  - 各ネットワークオブジェクトのDespawned()でGameManagerから登録解除
- **状態**: 実装済み

---

## 未実装の対策（優先度順）

### 🟡 優先度: 中

#### 1. ネットワークオブジェクトの適切なDespawn
**場所**:
- `Assets/Scripts/Title/NetworkRunnerHandler.cs`
- `Assets/Scripts/Game/GameManager.cs`

**問題**:
- ネットワークオブジェクトが適切にDespawnされていない可能性
- シーン遷移時に古いネットワークオブジェクトが残っている可能性

**対策**:
- シーン遷移前に全てのネットワークオブジェクトをDespawn
- `OnDestroy()`で適切にクリーンアップ
- NetworkRunnerのShutdown時に全てのオブジェクトをクリーンアップ

**期待される効果**: メモリリーク防止

---

### 🟢 優先度: 低

#### 7. 文字列補間の最適化
**場所**: 全スクリプト

**問題**:
- `$"{value}"`形式の文字列補間が頻繁に使用されている
- 文字列生成によるメモリアロケーションが発生

**対策**:
- 頻繁に呼ばれる箇所では`StringBuilder`を使用
- キャッシュ可能な文字列はキャッシュする
- 数値の文字列化は必要最小限にする

**期待される効果**: メモリアロケーション削減（効果は小さい）

---

#### 8. 配列の再生成の削減
**場所**:
- `Assets/Scripts/Data/UyopyonState.cs` (325-329行目)

**問題**:
- `Render()`で毎回新しい配列が生成されている
- `byte[] ailments = new byte[4];`が毎フレーム実行される可能性

**対策**:
- 配列をメンバー変数として保持し、再利用する
- 変更があった場合のみ新しい配列を生成する

**期待される効果**: メモリアロケーション削減（効果は小さい）

---

## 実装の優先順位

1. **最優先**: FindObjectsByTypeの削除（パフォーマンスへの影響が大きい）
2. **高**: Render()メソッドの最適化（毎フレーム実行されるため）
3. **高**: TextMeshProの更新最適化（UI更新は頻繁）
4. **中**: デバッグログの最適化（開発中は有効、本番で無効化）
5. **中**: Dictionaryのクリーンアップ（メモリリーク防止）
6. **中**: ネットワークオブジェクトの適切なDespawn（メモリリーク防止）

---

## 測定と検証

### メモリプロファイラーの使用
- Unity Profilerでメモリ使用量を測定
- 特に以下の項目を確認:
  - GC Alloc（ガベージコレクションによるアロケーション）
  - Mono Memory（Monoヒープの使用量）
  - Texture Memory（テクスチャメモリ）
  - Mesh Memory（メッシュメモリ）

### 測定ポイント
1. ゲーム開始時
2. ゲーム中（1分後、5分後、10分後）
3. ゲーム終了時
4. シーン遷移時

### 目標値
- GC Alloc: 1フレームあたり1MB以下
- Mono Memory: 100MB以下（WebGLビルド時）
- メモリ使用量の増加率: 1分あたり5MB以下

---

## 注意事項

- 修正は段階的に行い、各修正後にメモリ使用量を測定する
- パフォーマンスとメモリ使用量のバランスを考慮する
- WebGLビルドでは特にメモリ制約が厳しいため、注意が必要
- デバッグログの削除は、デバッグのしやすさを考慮して行う

---

## 参考資料

- [Unity Manual - Memory Profiler](https://docs.unity3d.com/Manual/ProfilerMemory.html)
- [Unity Manual - Performance Optimization](https://docs.unity3d.com/Manual/PerformanceOptimization.html)
- [Photon Fusion Documentation - Network Objects](https://doc.photonengine.com/fusion/current/manual/network-objects)

