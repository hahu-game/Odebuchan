# リザルト画面→タイトル画面遷移のテストチェックリスト

## 修正内容の概要

### 問題の原因
1. **破棄の順序が間違っていた**：GameManagerやGameFlowManagerを先に破棄してから、NetworkRunner.Shutdown()を呼んでいた
2. **手動での破棄が不要**：シーン遷移でGameSceneのオブジェクトは自動的に破棄される
3. **OnDisconnectedFromServerの誤動作**：TitleSceneにいるのに処理が実行される可能性があった

### 修正内容
1. **UIController.cs - ReturnToTitleCoroutine()**
   - Shutdown()を先に実行
   - 手動での破棄処理を削除（シーン遷移に任せる）
   - 不要なメソッド（CleanupGameSceneSingletons、CleanupGameSceneObjects）を削除

2. **NetworkRunnerHandler.cs - OnDisconnectedFromServer()**
   - TitleSceneにいる場合は何もしないチェックを追加
   - ログ出力を追加して状態を把握しやすくした

3. **シングルトンのクリーンアップ**
   - GameManager、UIController、GameFlowManagerの全てでOnDestroy()にInstanceをnull化する処理を追加

## テスト手順

### 1回目のマッチング→タイトル遷移
1. Unity Editorで再生開始
2. ランダムマッチボタンをクリック
3. マッチング成功を確認
4. ゲームを進行してリザルト画面を表示
5. 「タイトルに戻る」ボタンをクリック
6. **確認項目**：
   - [ ] タイトル画面に正常に遷移する
   - [ ] エラーログが出ない
   - [ ] 画面がフリーズしない
   - [ ] コンソールに以下のログが出る：
     - `[UIController] ReturnToTitleCoroutine: タイトルへの遷移を開始します`
     - `[UIController] NetworkRunnerをシャットダウンします`
     - `[UIController] Shutdown呼び出し完了`
     - `[UIController] ReturnToTitleCoroutine: TitleSceneに遷移します`
     - `[UIController] LoadScene呼び出し完了`
     - `[NetworkRunnerHandler] サーバーから切断されました`
     - `[NetworkRunnerHandler] 現在のシーン: GameScene`（または TitleScene）
     - `[TitleScreenManager] OnSceneLoaded: scene=TitleScene`
     - `[TitleScreenManager] TitleSceneに戻ってきました。UI参照を再取得します`

### 2回目のマッチング→タイトル遷移
7. タイトル画面でプレイヤー名を入力
8. ランダムマッチボタンをクリック
9. **確認項目**：
   - [ ] マッチングパネルが表示される
   - [ ] マッチング成功する
10. ゲームを進行してリザルト画面を表示
11. 「タイトルに戻る」ボタンをクリック
12. **確認項目**：
   - [ ] タイトル画面に正常に遷移する
   - [ ] エラーログが出ない
   - [ ] 1回目と同じログが出る

### 3回目のマッチング→タイトル遷移
13. タイトル画面でランダムマッチボタンをクリック
14. **確認項目**：
   - [ ] マッチングパネルが表示される
   - [ ] マッチング成功する
15. ゲームを進行してリザルト画面を表示
16. 「タイトルに戻る」ボタンをクリック
17. **確認項目**：
   - [ ] タイトル画面に正常に遷移する
   - [ ] エラーログが出ない
   - [ ] **特に確認**：`[GameFlowManager] ゲーム終了済みのため、処理を中断します`のログが出ない

### 4回目以降の確認
18. 3回目と同じ手順を繰り返す
19. **確認項目**：
   - [ ] 何回繰り返してもエラーが出ない
   - [ ] メモリリークが発生していない（Profilerで確認）

## 期待される動作

### 正常な遷移フロー
```
1. リザルトボタンクリック
   ↓
2. UIController.ReturnToTitleCoroutine()開始
   ↓
3. NetworkRunner.Shutdown()
   ↓
4. SceneManager.LoadScene("TitleScene")
   ↓
5. GameSceneのオブジェクトが自動破棄
   ↓
6. TitleSceneロード完了
   ↓
7. TitleScreenManager.OnSceneLoaded()
   ↓
8. UI参照の再取得
   ↓
9. UI状態の初期化
   ↓
10. タイトル画面が使用可能
```

### シングルトンのライフサイクル
```
GameScene開始時:
- GameManager.Instance = 新しいインスタンス
- GameFlowManager.Instance = 新しいインスタンス
- UIController.Instance = 新しいインスタンス

GameScene終了時:
- OnDestroy()で各Instanceをnull化
- シーン遷移でGameObjectが破棄される

次のGameScene開始時:
- 再び新しいインスタンスが設定される
```

## エラーが発生した場合の確認項目

1. **どのログの後にエラーが発生したか**
2. **エラーメッセージの内容**
3. **スタックトレース**
4. **何回目のマッチングで発生したか**
5. **ホストとクライアントのどちらで発生したか**

## 修正の妥当性チェック

- [x] Shutdown()を先に実行している
- [x] シーン遷移でオブジェクトの破棄を任せている
- [x] OnDestroy()でシングルトンのInstanceをnull化している
- [x] TitleSceneでの誤動作を防ぐチェックを追加している
- [x] 不要なコードを削除している
- [x] コルーチン実行中にGameObjectを破棄していない
