# PROCESS.md - Odebuchanプロジェクト段階的実装手順書

このドキュメントは、AI_REQUIREMENTS.mdの仕様に基づいて、Odebuchanプロジェクトを段階的に実装するための手順書です。

## 既に実装済みの項目

以下の機能はすでにプロジェクトに実装されています：

### 1. TitleSceneの基本機能
- ✅ プレイヤー名入力フィールド（TitleScreenManager.cs）
- ✅ ランダムマッチボタン（TitleScreenManager.cs）
- ✅ フレンドマッチボタンとセッション名入力（TitleScreenManager.cs）
- ✅ マッチング中UI（オーバーレイパネル、キャンセルボタン）

### 2. ネットワーク接続とマッチング機能
- ✅ Photon Fusion 2.0.7による接続処理（NetworkRunnerHandler.cs）
- ✅ Shared Modeでの2人マッチング（NetworkRunnerHandler.cs）
- ✅ 2人揃った時のGameSceneへの自動遷移（NetworkRunnerHandler.cs）
- ✅ WebGL用の1秒待機処理（DelayedSceneLoad）

### 3. ネットワークプレイヤー管理
- ✅ NetworkPlayerオブジェクトのスポーン（NetworkRunnerHandler.cs）
- ✅ プレイヤー名のネットワーク同期（NetworkPlayer.cs）
- ✅ UyopyonStateオブジェクトのスポーン（GameManager.cs）

### 4. 基本的なステータス管理とUI表示
- ✅ UyopyonStateクラスの基礎構造（Weight, Energy, CurrentStatusプロパティ）
- ✅ UIControllerによる自分と相手の名前、重さ、元気の表示
- ✅ Render()メソッドによる変更検知とUI更新

### 5. プロジェクト構成
- ✅ Unity 6000.2.7f2でのプロジェクト設定
- ✅ WebGLビルド設定
- ✅ TitleSceneとGameSceneのビルド順序設定

---

## 実装手順

以下、未実装の機能を段階的に実装していきます。各段階は細かく分けられており、エラーが発生した際に原因を特定しやすい構成になっています。

---

## フェーズ1: 基本データ構造の整備

### 1.1 列挙型の定義

**目的**: ゲーム全体で使用する列挙型を定義する

**作業内容**:
1. `Assets/Scripts/Data/GameEnums.cs`を新規作成
2. 以下の列挙型を定義:
   - `GamePhase`: Preparation, Selection, Execution, GameEnd
   - `ActionType`: Eat, Sleep, Play, Clinic, SpecialAbility
   - `Genre`: Rock, Scissors, Paper, None
   - `SpecialAbilityType`: Gaishoku, Kintre, Gamushara, Benkyou, Jukusui, Dokagui
   - `StatusAilment`: SleepApnea, Diabetes, BackPain, Heatstroke

**確認項目**:
- [ ] Unityエディタでコンパイルエラーが発生しないこと
- [ ] 他のスクリプトから`GamePhase.Preparation`などの形式で参照できること

**参照スクリプト**: `ScriptPlans/1.1.GameEnums.cs.md`

---

### 1.2 ScriptableObjectの作成

**目的**: ゲームパラメーターを一元管理するScriptableObjectを作成する

**作業内容**:
1. `Assets/Scripts/Data/GameParameters.cs`を新規作成
2. 61個のパラメーターを定義（AI_REQUIREMENTS.md 9章参照）
3. `[CreateAssetMenu]`属性を付与

**確認項目**:
- [ ] Unityエディタでコンパイルエラーが発生しないこと
- [ ] Unity > Assets > Create > Odebuchan > GameParametersでScriptableObjectを作成できること
- [ ] `Assets/Data/`フォルダを作成し、GameParametersアセットを配置できること

**参照スクリプト**: `ScriptPlans/1.2.GameParameters.cs.md`

---

### 1.3 ActionData構造体の定義

**目的**: プレイヤーの行動選択データを保持する構造体を定義する

**作業内容**:
1. `Assets/Scripts/Data/ActionData.cs`を新規作成
2. `ActionType`と`Genre`を保持する構造体を定義
3. Fusion対応のため`INetworkStruct`を実装

**確認項目**:
- [ ] Unityエディタでコンパイルエラーが発生しないこと
- [ ] NetworkBehaviourクラスから`[Networked]`プロパティとして使用できること

**参照スクリプト**: `ScriptPlans/1.3.ActionData.cs.md`

---

### 1.4 UyopyonStateの拡張

**目的**: AI_REQUIREMENTSに記載された全てのプロパティをUyopyonStateに追加する

**作業内容**:
1. `UyopyonState.cs`に以下のプロパティを追加:
   - `NetworkArray<byte> StatusAilments` (サイズ4)
   - `int PlayBuffWeight`
   - `int PlayBuffEnergy`
   - `int StudyCombo`
   - `NetworkString<_16> SpecialAbilityName`
   - `bool HasEvolved`
   - `NetworkString<_16> VisualType`
2. 初期値を設定するメソッドを追加
3. Render()メソッドを拡張して新しいプロパティの変更を検知

**確認項目**:
- [ ] Unityエディタでコンパイルエラーが発生しないこと
- [ ] Play実行時にUyopyonStateがスポーンされ、初期値が設定されること

**参照スクリプト**: `ScriptPlans/1.4.UyopyonState.cs.md`

---

## フェーズ2: ゲームフェーズ管理システム

### 2.1 GameFlowManagerの作成（基本構造）

**目的**: ゲーム全体のフェーズを管理するクラスを作成する

**作業内容**:
1. `Assets/Scripts/Game/GameFlowManager.cs`を新規作成
2. NetworkBehaviourを継承
3. シングルトンパターンを実装
4. 以下のプロパティを定義:
   - `[Networked] int CurrentDay`
   - `[Networked] GamePhase CurrentPhase`
   - `[Networked, Capacity(3)] NetworkArray<SpecialAbilityType> AvailableSpecialAbilities`
5. GameParametersへの参照を追加
6. フェーズ遷移のスタブメソッドを定義（実装は次の段階）

**確認項目**:
- [ ] Unityエディタでコンパイルエラーが発生しないこと
- [ ] GameSceneにGameFlowManagerオブジェクトを配置できること
- [ ] GameParametersアセットをInspectorで設定できること

**参照スクリプト**: `ScriptPlans/2.1.GameFlowManager.cs.md`

---

### 2.2 GameFlowManager: ゲーム開始処理の実装

**目的**: ゲーム開始時の処理を実装する

**作業内容**:
1. `StartGame()`メソッドを実装:
   - 特殊能力6種から3種をランダム選出
   - CurrentDay = 1に設定
   - CurrentPhase = Preparationに設定
   - RPC経由で全クライアントにブラックアウト表示を指示
2. `RPC_ShowBlackout(string text)`を実装
3. ゲーム開始時に`StartGame()`を呼び出す仕組みを追加

**確認項目**:
- [ ] Play実行時に2人マッチング後、CurrentDay = 1, CurrentPhase = Preparationになること
- [ ] AvailableSpecialAbilitiesに3つの特殊能力がランダムに設定されること
- [ ] ホストとクライアント両方で同じ値が確認できること

**参照スクリプト**: `ScriptPlans/2.2.GameFlowManager.cs.md`

---

### 2.3 PlayerActionDataの作成

**目的**: プレイヤーの行動選択状態を管理するNetworkBehaviourを作成する

**作業内容**:
1. `Assets/Scripts/Data/PlayerActionData.cs`を新規作成
2. NetworkBehaviourを継承
3. 以下のプロパティを定義:
   - `[Networked] ActionData MorningAction`
   - `[Networked] ActionData AfternoonAction`
   - `[Networked] bool IsActionFixed`
   - `PlayerRef OwnerPlayer`
4. GameManagerでPlayerActionDataをスポーンする処理を追加

**確認項目**:
- [ ] Unityエディタでコンパイルエラーが発生しないこと
- [ ] Play実行時に各プレイヤーに対してPlayerActionDataがスポーンされること

**参照スクリプト**: `ScriptPlans/2.3.PlayerActionData.cs.md`

---

## フェーズ3: 基本UIの構築

### 3.1 GameSceneの基本UI配置

**目的**: 行動選択ボタンとステータスパネルの基本レイアウトを作成する

**作業内容**:
1. GameSceneに以下のUI要素を配置:
   - Canvas (Screen Space - Overlay)
   - MyStatusPanel (左上): プレイヤー名、重さ、元気
   - OppStatusPanel (右上): 相手の名前、重さ、元気
   - ActionButtonsPanel (中央下): 4つのボタン（たべる、ねむる、あそぶ、つういん）
   - FixButton (右下): 確定ボタン
   - ClearButton (左下): クリアボタン
2. UIControllerに各UI要素への参照を追加
3. ボタンのクリックイベントを空のメソッドに紐付け

**確認項目**:
- [ ] GameSceneを開いた時に配置したUI要素が表示されること
- [ ] UIControllerのInspectorで全てのUI参照が設定されていること
- [ ] ボタンをクリックしてもエラーが発生しないこと（処理は未実装でOK）

**参照スクリプト**: `ScriptPlans/3.1.UIController.cs.md`

---

### 3.2 行動選択ボタンの実装（クライアント側）

**目的**: プレイヤーが行動を選択できるようにする

**作業内容**:
1. UIControllerに以下のメソッドを実装:
   - `OnActionButtonClicked(ActionType action)`
   - `OnFixButtonClicked()`
   - `OnClearButtonClicked()`
2. 選択状態を保持する変数を追加（午前・午後の選択状態）
3. 選択した行動を視覚的に表示する処理を追加
4. クリアボタンで選択をリセットする処理を追加
5. 確定ボタンで選択をPlayerActionDataに送信する処理を追加（RPC使用）

**確認項目**:
- [ ] たべるボタンをクリックすると午前の選択として記録されること
- [ ] もう一度別のボタンをクリックすると午後の選択として記録されること
- [ ] クリアボタンで選択がリセットされること
- [ ] 確定ボタンで選択がネットワーク送信されること（Debug.Logで確認）

**参照スクリプト**: `ScriptPlans/3.2.UIController.cs.md`

---

### 3.3 行動表示パネルの追加

**目的**: 選択した行動と実行された行動を視覚的に表示する

**作業内容**:
1. GameSceneに以下のUI要素を追加:
   - MyYesterdayAfternoonActionPanel
   - OppYesterdayAfternoonActionPanel
   - MyTodayMorningActionPanel
   - OppTodayMorningActionPanel
   - MyTodayAfternoonActionPanel
   - OppTodayAfternoonActionPanel
2. UIControllerに各パネルへの参照を追加
3. 行動選択時に対応するパネルを更新するメソッドを実装
4. 太枠表示で現在選択中の時間帯を示す処理を追加

**確認項目**:
- [ ] 午前の行動を選択するとMyTodayMorningActionPanelに表示されること
- [ ] 午後の行動を選択するとMyTodayAfternoonActionPanelに表示されること
- [ ] 選択中の時間帯のパネルが太枠で表示されること

**参照スクリプト**: `ScriptPlans/3.3.UIController.cs.md`

---

### 3.4 ブラックアウトパネルとログエリアの追加

**目的**: ゲーム進行状況を表示するUIを追加する

**作業内容**:
1. GameSceneに以下のUI要素を追加:
   - BlackoutPanel: 全画面を覆う黒パネル + テキスト
   - LogArea: ScrollViewでログを表示
2. UIControllerに以下のメソッドを実装:
   - `ShowBlackout(string text, float duration)`
   - `AddLog(string message)`
3. GameFlowManagerから呼び出せるようにする

**確認項目**:
- [ ] ゲーム開始時に「ゲームスタート！」とブラックアウト表示されること
- [ ] 1秒後にブラックアウトが消えること
- [ ] ログエリアにメッセージが追加されること

**参照スクリプト**: `ScriptPlans/3.4.UIController.cs.md`

---

## フェーズ4: 選択フェーズの実装

### 4.1 GameFlowManager: 準備フェーズの実装

**目的**: 準備フェーズの処理を実装する

**作業内容**:
1. `StartPreparationPhase()`メソッドを実装:
   - CurrentPhaseをPreparationに設定
   - ブラックアウトで「〇日目」を表示
   - 糖尿病チェック（実装は後回し）
   - 進化判定（実装は後回し）
   - 選択フェーズへ遷移
2. `RPC_StartPreparationPhase(int day)`を実装

**確認項目**:
- [ ] ゲーム開始時に「1日目」とブラックアウト表示されること
- [ ] CurrentPhaseがPreparationになること
- [ ] その後、選択フェーズに自動遷移すること

**参照スクリプト**: `ScriptPlans/4.1.GameFlowManager.cs.md`

---

### 4.2 GameFlowManager: 選択フェーズの実装

**目的**: 選択フェーズの処理を実装する

**作業内容**:
1. `StartSelectionPhase()`メソッドを実装:
   - CurrentPhaseをSelectionに設定
   - UIの操作ロック解除（UIController経由）
   - **行動ボタンのOutlineを表示**（`UIController.Instance.ShowActionButtonOutlines()`を呼ぶ）
   - 60秒タイマー開始
   - 両プレイヤーが確定するか、タイムアウトで実行フェーズへ遷移
2. タイマー処理を実装（FixedUpdateNetworkで監視）
3. 両プレイヤーの確定状態を監視する処理を実装
4. 選択フェーズ終了時に**行動ボタンのOutlineを非表示**（`UIController.Instance.HideActionButtonOutlines()`を呼ぶ）

**確認項目**:
- [ ] 選択フェーズになると行動選択ボタンがクリックできること
- [ ] **選択フェーズ開始時に行動ボタンにオレンジ色（#FFB101）のOutlineが表示されること**
- [ ] 60秒経過すると自動的に次のフェーズに進むこと
- [ ] 両プレイヤーが確定ボタンを押すと即座に次のフェーズに進むこと
- [ ] タイムアウト時に未選択の行動が「ねむる」になること
- [ ] **選択フェーズ終了時に行動ボタンのOutlineが非表示になること**

**参照スクリプト**: `ScriptPlans/4.2.GameFlowManager.cs.md`

---

### 4.3 UIController: 操作ロック機能の実装

**目的**: フェーズに応じてUIの操作可否を制御する

**作業内容**:
1. UIControllerに`SetActionButtonsInteractable(bool interactable)`メソッドを実装
2. GameFlowManagerから呼び出して、選択フェーズ以外では操作不可にする

**確認項目**:
- [ ] 準備フェーズでは行動選択ボタンがクリックできないこと
- [ ] 選択フェーズになるとクリックできるようになること
- [ ] 実行フェーズでは再びクリックできなくなること

**参照スクリプト**: `ScriptPlans/4.3.UIController.cs.md`

---

## フェーズ5: 実行フェーズの実装（基本行動）

### 5.1 TurnProcessorの基本構造

**目的**: 行動実行を管理するクラスを作成する

**作業内容**:
1. `TurnProcessor.cs`を以下の構造に変更:
   - NetworkBehaviourを継承
   - GameParametersへの参照を追加
   - `ProcessMorning()`と`ProcessAfternoon()`メソッドのスタブを定義
2. GameFlowManagerからTurnProcessorを呼び出す仕組みを追加

**確認項目**:
- [ ] Unityエディタでコンパイルエラーが発生しないこと
- [ ] GameFlowManagerからTurnProcessorのメソッドを呼び出せること

**参照スクリプト**: `ScriptPlans/5.1.TurnProcessor.cs.md`

---

### 5.2 ジャンケン判定システムの実装

**目的**: 行動のジャンルに基づいてジャンケン判定を行う

**作業内容**:
1. TurnProcessorに`JudgeJanken(Genre p1Genre, Genre p2Genre)`メソッドを実装
2. 勝敗に応じた元気・重さ・バフの変更を適用するメソッドを実装
3. ジャンケン結果をログに追加する処理を実装
4. RPC経由で全クライアントにジャンケン結果を通知

**確認項目**:
- [ ] 午前の行動でジャンケン判定が正しく行われること
- [ ] 勝者の元気が増加、敗者の元気が減少すること
- [ ] ログに勝敗結果が表示されること
- [ ] あいこの場合は元気が変化しないこと

**参照スクリプト**: `ScriptPlans/5.2.TurnProcessor.cs.md`

---

### 5.3 基本行動の実装（たべる・ねむる）

**目的**: たべるとねむるの行動効果を実装する

**作業内容**:
1. TurnProcessorに`ExecuteAction(PlayerRef player, ActionType action)`メソッドを実装
2. たべるの効果:
   - 元気-15、重さ+50（+ PlayBuffWeight）
   - 病気発症判定（実装は後回し）
3. ねむるの効果:
   - 元気+50（+ PlayBuffEnergy）
   - 睡眠時無呼吸症候群の影響（実装は後回し）
4. 効果適用後、UyopyonStateを更新
5. ログに実行結果を追加

**確認項目**:
- [ ] たべるを実行すると重さが50増加、元気が15減少すること
- [ ] ねむるを実行すると元気が50増加すること
- [ ] 両プレイヤーのUIに正しく反映されること

**参照スクリプト**: `ScriptPlans/5.3.TurnProcessor.cs.md`

---

### 5.4 基本行動の実装（あそぶ・つういん）

**目的**: あそぶとつういんの行動効果を実装する

**作業内容**:
1. あそぶの効果:
   - 元気-30
   - PlayBuffWeight +10、PlayBuffEnergy +10
   - ケガ発症判定（実装は後回し）
2. つういんの効果:
   - 元気-20
   - 状態異常を全てクリア
3. 効果適用後、UyopyonStateを更新
4. ログに実行結果を追加

**確認項目**:
- [ ] あそぶを実行すると元気が30減少、PlayBuffが各10増加すること
- [ ] つういんを実行すると元気が20減少すること
- [ ] 両プレイヤーのUIに正しく反映されること
- [ ] あそぶ後にたべるを実行すると、重さが60増加すること（50+10のバフ）

**参照スクリプト**: `ScriptPlans/5.4.TurnProcessor.cs.md`

---

### 5.5 GameFlowManager: 実行フェーズの実装

**目的**: 実行フェーズの全体的な流れを実装する

**作業内容**:
1. `StartExecutionPhase()`メソッドを実装:
   - CurrentPhaseをExecutionに設定
   - ブラックアウト「行動開始！」を表示
   - 相手の選択を表示（2秒待機）
   - TurnProcessor.ProcessMorning()を呼び出し
   - 勝利判定（実装は後回し）
   - TurnProcessor.ProcessAfternoon()を呼び出し
   - 勝利判定（実装は後回し）
   - 次の日の準備フェーズへ遷移
2. 非同期処理（await UniTask.Delay）を使用して待機処理を実装

**確認項目**:
- [ ] 確定ボタンを押すと実行フェーズに遷移すること
- [ ] 「行動開始！」とブラックアウト表示されること
- [ ] 2秒待機後、相手の選択が表示されること
- [ ] 午前、午後の順で行動が実行されること
- [ ] 実行後、次の日の準備フェーズに遷移すること

**参照スクリプト**: `ScriptPlans/5.5.GameFlowManager.cs.md`

---

### 5.6 連続使用ペナルティの実装

**目的**: 同じ行動を連続で選択した場合の元気ペナルティを実装する

**作業内容**:
1. PlayerActionDataに`ActionType LastAfternoonAction`プロパティを追加
2. TurnProcessorで連続使用判定を実装:
   - 前日午後と今日午前が同じ → 元気-20
   - 今日午前と今日午後が同じ → 元気-20
3. ペナルティ適用後、ログに追加

**確認項目**:
- [ ] 1日目午前・午後で同じ行動を選択すると、午後実行時に元気が追加で20減少すること
- [ ] 1日目午後と2日目午前で同じ行動を選択すると、2日目午前実行時に元気が追加で20減少すること
- [ ] ログに「連続使用ペナルティ！」と表示されること

**参照スクリプト**: `ScriptPlans/5.6.TurnProcessor.cs.md`

---

## フェーズ6: 状態異常システムの実装

### 6.1 状態異常の発症処理（病気）

**目的**: たべる実行時の病気発症判定を実装する

**作業内容**:
1. TurnProcessorに`CheckSickness(PlayerRef player, int weight)`メソッドを実装
2. 発症確率 = 現在の重さ ÷ 10 (%)
3. 発症した場合、50%ずつで睡眠時無呼吸症候群か糖尿病を発症
4. すでに同じ病気の場合は、もう一方の病気を発症
5. UyopyonState.StatusAilmentsを更新
6. ログに発症メッセージを追加

**確認項目**:
- [ ] たべる実行時に病気発症判定が行われること
- [ ] 重さ100の場合、約10%の確率で病気が発症すること
- [ ] 発症した状態異常がUIに表示されること（UIControllerの拡張が必要）
- [ ] すでに睡眠時無呼吸症候群の場合、2回目の発症で糖尿病になること

**参照スクリプト**: `ScriptPlans/6.1.TurnProcessor.cs.md`

---

### 6.2 状態異常の発症処理（ケガ）

**目的**: あそぶ実行時のケガ発症判定を実装する

**作業内容**:
1. TurnProcessorに`CheckInjury(PlayerRef player, int weight)`メソッドを実装
2. 発症確率 = 現在の重さ ÷ 10 (%)
3. 発症した場合、50%ずつで腰痛か熱中症を発症
4. すでに同じケガの場合は、もう一方のケガを発症
5. UyopyonState.StatusAilmentsを更新
6. ログに発症メッセージを追加

**確認項目**:
- [ ] あそぶ実行時にケガ発症判定が行われること
- [ ] 重さ100の場合、約10%の確率でケガが発症すること
- [ ] 発症した状態異常がUIに表示されること
- [ ] すでに腰痛の場合、2回目の発症で熱中症になること

**参照スクリプト**: `ScriptPlans/6.2.TurnProcessor.cs.md`

---

### 6.3 状態異常の効果適用（睡眠時無呼吸症候群）

**目的**: ねむる実行時の睡眠時無呼吸症候群の影響を実装する

**作業内容**:
1. TurnProcessorのねむる実行処理を修正:
   - 睡眠時無呼吸症候群がある場合、元気回復量を-20する
2. ログに状態異常の影響を追加

**確認項目**:
- [ ] 睡眠時無呼吸症候群の状態でねむるを実行すると、元気回復量が30になること（50 - 20）
- [ ] ログに「睡眠時無呼吸症候群の影響！」と表示されること

**参照スクリプト**: `ScriptPlans/6.3.TurnProcessor.cs.md`

---

### 6.4 状態異常の効果適用（糖尿病）

**目的**: 準備フェーズでの糖尿病の朝処理を実装する

**作業内容**:
1. GameFlowManagerのStartPreparationPhase()に糖尿病処理を追加:
   - 糖尿病の状態異常がある場合、重さ-20、元気-20
2. ログに状態異常の影響を追加

**確認項目**:
- [ ] 糖尿病の状態で朝を迎えると、重さと元気が各20減少すること
- [ ] ログに「糖尿病の影響！」と表示されること

**参照スクリプト**: `ScriptPlans/6.4.GameFlowManager.cs.md`

---

### 6.5 状態異常の効果適用（腰痛）

**目的**: 腰痛による行動キャンセル処理を実装する

**作業内容**:
1. TurnProcessorの行動実行前に腰痛チェックを追加:
   - 腰痛がある場合、30%の確率で行動がキャンセルされ「ねむる」に変更
2. ログに状態異常の影響を追加

**確認項目**:
- [ ] 腰痛の状態で行動を選択すると、30%の確率でねむるに変更されること
- [ ] ログに「腰痛で動けない！」と表示されること

**参照スクリプト**: `ScriptPlans/6.5.TurnProcessor.cs.md`

---

### 6.6 状態異常の効果適用（熱中症）

**目的**: 熱中症による強制つういん処理を実装する

**作業内容**:
1. TurnProcessorであそぶ実行時の熱中症発症後、強制つういん処理を追加:
   - 午前に発症 → 今日の午後を「つういん」に強制変更
   - 午後に発症 → 翌日の準備フェーズで午前を「つういん」に自動セット
2. PlayerActionDataに`bool MorningActionLocked`フラグを追加
3. UIControllerで変更不可状態を視覚的に表示

**確認項目**:
- [ ] 午前にあそぶで熱中症が発症すると、午後の行動が「つういん」に変更されること
- [ ] 午後にあそぶで熱中症が発症すると、翌日午前の行動が「つういん」に固定されること
- [ ] 固定された行動は変更できないこと（ボタンがグレーアウト）

**参照スクリプト**: `ScriptPlans/6.6.TurnProcessor.cs.md`

---

### 6.7 UIController: 状態異常の表示

**目的**: 状態異常をUIに表示する

**作業内容**:
1. MyStatusPanelとOppStatusPanelに状態異常アイコン表示エリアを追加
2. UIControllerに`UpdateStatusAilmentDisplay(PlayerRef player, byte[] ailments)`メソッドを実装
3. UyopyonState.Render()から状態異常の変更を検知して呼び出す

**確認項目**:
- [ ] 病気やケガが発症すると、対応するアイコンが表示されること
- [ ] つういんを実行すると、アイコンが消えること
- [ ] 両プレイヤーの状態異常が正しく表示されること

**参照スクリプト**: `ScriptPlans/6.7.UIController.cs.md`

---

## フェーズ7: 特殊能力システムの実装

### 7.1 進化判定と特殊能力選択UIの実装

**目的**: 重さ200kgで進化し、特殊能力を選択できるようにする

**作業内容**:
1. GameSceneにSpecialActionChoicePanelを追加（オーバーレイ）
2. 3つの選択肢ボタンを配置
3. UIControllerに`ShowSpecialAbilityChoice(SpecialAbilityType[] choices)`メソッドを実装
4. GameFlowManagerのStartPreparationPhase()に進化判定を追加:
   - Weight >= 200 && !HasEvolved の場合、進化処理を開始
   - AvailableSpecialAbilitiesから選択肢を表示
   - 選択完了まで次の処理を待機
5. 選択完了後、UyopyonState.SpecialAbilityName、HasEvolved、VisualTypeを更新

**確認項目**:
- [ ] 重さが200kgに到達すると、準備フェーズで進化判定が行われること
- [ ] SpecialActionChoicePanelが表示され、3つの選択肢が表示されること
- [ ] 選択肢をクリックすると、その特殊能力が設定されること
- [ ] 進化後は再度進化判定が行われないこと

**参照スクリプト**: `ScriptPlans/7.1.GameFlowManager.cs.md`, `ScriptPlans/7.1.UIController.cs.md`

---

### 7.2 2人同時進化の優先順位処理

**目的**: 2人が同時に進化条件を満たした場合の優先順位を実装する

**作業内容**:
1. GameFlowManagerに`DetermineEvolutionOrder()`メソッドを実装:
   - 重さが大きい方が先
   - 重さが同値なら元気が大きい方が先
   - 元気も同値ならランダム
2. 先に選んだプレイヤーの選択が完了するまで、もう一方を待機させる
3. 選ばれた特殊能力をAvailableSpecialAbilitiesから除外

**確認項目**:
- [ ] 2人が同時に200kgに到達した場合、重さの大きい方が先に選択できること
- [ ] 先のプレイヤーが選択完了後、後のプレイヤーが選択できること
- [ ] 先に選ばれた特殊能力は後のプレイヤーの選択肢に表示されないこと

**参照スクリプト**: `ScriptPlans/7.2.GameFlowManager.cs.md`

---

### 7.3 特殊能力: がいしょくの実装

**目的**: がいしょくの効果を実装する

**作業内容**:
1. TurnProcessorにがいしょくの処理を追加:
   - たべると同じ効果（元気-15、重さ+50 + PlayBuffWeight）
   - ジャンルがPaper（パー）
2. UIに特殊能力ボタン（SpecialButton）を追加（進化後のみ表示）。
3. SpecialButtonはがいしょく以外の特殊能力も含め、選択された特殊能力の名前が表示されるボタンにする。

**確認項目**:
- [ ] がいしょくを選択すると、特殊能力ボタンが表示されること
- [ ] がいしょくを実行すると、たべると同じ効果が適用されること
- [ ] ジャンケン判定でパーとして扱われること

**参照スクリプト**: `ScriptPlans/7.3.TurnProcessor.cs.md`, `ScriptPlans/7.3.UIController.cs.md`

---

### 7.4 特殊能力: がむしゃらの実装

**目的**: がむしゃらの効果を実装する

**作業内容**:
1. TurnProcessorにがむしゃらの処理を追加:
   - 元気-40
   - 4パターンからランダム選択（各25%）:
     1. たべるの重さ増加 × 1.2倍
     2. ねむるの元気増加 × 1.2倍
     3. PlayBuffWeight +15, PlayBuffEnergy +15
     4. MIX: 重さ増加×0.5、元気増加×0.5、バフ各+8
   - ジャンルがNone（ジャンケン判定なし）
2. ログに選ばれた効果を表示

**確認項目**:
- [ ] がむしゃらを実行すると、元気が40減少すること
- [ ] 4パターンのいずれかがランダムに選ばれること
- [ ] 各パターンの効果が正しく適用されること
- [ ] ジャンケン判定が行われないこと

**参照スクリプト**: `ScriptPlans/7.4.TurnProcessor.cs.md`

---

### 7.5 特殊能力: べんきょうの実装

**目的**: べんきょうの効果を実装する

**作業内容**:
1. TurnProcessorにべんきょうの処理を追加:
   - 元気-25
   - PlayBuffWeight +10
   - StudyCombo +1、次回べんきょう時にPlayBuffWeight += (StudyCombo × 10)
   - べんきょう以外の行動でStudyCombo = 0
2. ログに連続ボーナスを表示

**確認項目**:
- [ ] べんきょうを実行すると、元気が25減少、PlayBuffWeightが10増加すること
- [ ] 2回連続でべんきょうを実行すると、2回目はPlayBuffWeightが20増加すること
- [ ] べんきょう以外の行動を実行すると、StudyComboが0にリセットされること

**参照スクリプト**: `ScriptPlans/7.5.TurnProcessor.cs.md`

---

### 7.6 特殊能力: じゅくすい・どかぐいの実装

**目的**: じゅくすいとどかぐいの効果を実装する

**作業内容**:
1. TurnProcessorにじゅくすいの処理を追加:
   - 選択条件: 自分と相手の朝時点の重さ合計が奇数
   - 効果: 元気+70（ねむるの+50 + ボーナス+20）
2. TurnProcessorにどかぐいの処理を追加:
   - 選択条件: 自分と相手の朝時点の重さ合計が偶数
   - 効果: 元気-15、重さ+80（たべるの+50 + ボーナス+30）
3. UIで選択条件を満たさない場合はボタンをグレーアウト

**確認項目**:
- [ ] 重さ合計が奇数の場合、じゅくすいが選択可能になること
- [ ] じゅくすいを実行すると、元気が70増加すること
- [ ] 重さ合計が偶数の場合、どかぐいが選択可能になること
- [ ] どかぐいを実行すると、重さが80増加すること

**参照スクリプト**: `ScriptPlans/7.6.TurnProcessor.cs.md`

---

### 7.7 特殊能力: きんとれの実装

**目的**: きんとれの効果を実装する

**作業内容**:
1. TurnProcessorにきんとれの処理を追加:
   - 元気-120
   - 重さ×0.5（半分になる）
   - 今後の重さ・元気増加量が1.5倍になる（バフ倍率を記録）
2. UyopyonStateに`float BuffMultiplier`プロパティを追加
3. 各行動の効果計算時にBuffMultiplierを適用

**確認項目**:
- [ ] きんとれを実行すると、元気が120減少、重さが半分になること
- [ ] きんとれ後にたべるを実行すると、重さ増加量が75になること（50×1.5）
- [ ] きんとれ後にねむるを実行すると、元気増加量が75になること（50×1.5）

**参照スクリプト**: `ScriptPlans/7.7.TurnProcessor.cs.md`

---

### 7.8 UIController: 特殊能力ボタンと進化前表示の実装

**目的**: 特殊能力に関するUIを完成させる

**作業内容**:
1. GameSceneにBeforeSpecialActionTextを追加（進化前のみ表示）
2. GameSceneにSpecialActionButtonを追加（進化後のみ表示）
3. UIControllerに以下のメソッドを実装:
   - `UpdateSpecialAbilityDisplay(PlayerRef player, bool hasEvolved, string abilityName)`
   - `ShowBeforeEvolutionText(int currentWeight)`
4. 進化前は「200kgで進化すると...」と表示
5. 進化後は選択した特殊能力ボタンを表示

**確認項目**:
- [ ] 重さ200kg未満の場合、BeforeSpecialActionTextが表示されること
- [ ] 進化後、BeforeSpecialActionTextが非表示になり、SpecialActionButtonが表示されること
- [ ] SpecialActionButtonをクリックすると、選択した特殊能力が実行できること

**参照スクリプト**: `ScriptPlans/7.8.UIController.cs.md`

---

## フェーズ8: 勝利判定とゲーム終了処理

### 8.1 勝利判定の実装

**目的**: 重さ700kg到達で勝利判定を行う

**作業内容**:
1. TurnProcessorに`CheckVictory()`メソッドを実装:
   - 午前・午後の行動実行後に呼び出す
   - Weight >= 700のプレイヤーがいればゲーム終了フラグを立てる
2. GameFlowManagerに`EndGame(PlayerRef winner)`メソッドを実装:
   - CurrentPhaseをGameEndに設定
   - 勝者と敗者を記録
   - ゲーム終了処理を開始

**確認項目**:
- [ ] 午前の行動で重さが700kgに到達すると、その時点でゲームが終了すること
- [ ] 午後の行動で重さが700kgに到達すると、その時点でゲームが終了すること
- [ ] ゲーム終了後、次のターンに進まないこと

**参照スクリプト**: `ScriptPlans/8.1.TurnProcessor.cs.md`, `ScriptPlans/8.1.GameFlowManager.cs.md`

---

### 8.2 ゲーム終了アニメーションとBGM変更

**目的**: ゲーム終了時の演出を実装する

**作業内容**:
1. GameFlowManagerのEndGame()に以下を追加:
   - 勝利側のうーぴょんで勝利アニメーション再生
   - 敗北側のうーぴょんで敗北アニメーション再生
   - BGM・SE変更（ゲーム終了時のBGM・SE）
   - 4秒待機
2. UIControllerに`PlayVictoryAnimation(PlayerRef player)`と`PlayDefeatAnimation(PlayerRef player)`メソッドを追加（スタブ、アニメーションは後で設定）

**確認項目**:
- [ ] ゲーム終了時に勝利アニメーションが再生されること（現時点ではログ出力でOK）
- [ ] ゲーム終了時に敗北アニメーションが再生されること（現時点ではログ出力でOK）
- [ ] 4秒待機後、リザルト画面が表示されること

**参照スクリプト**: `ScriptPlans/8.2.GameFlowManager.cs.md`, `ScriptPlans/8.2.UIController.cs.md`

---

### 8.3 リザルト画面の実装

**目的**: ゲーム終了後のリザルト画面を表示する

**作業内容**:
1. GameSceneにResultPanelを追加:
   - 勝者名
   - 終了日数・午前/午後
   - 両プレイヤーの最終重さ・元気・特殊能力
   - 再戦ボタン
   - タイトルに戻るボタン
2. UIControllerに`ShowResultPanel(PlayerRef winner, int day, bool isMorning)`メソッドを実装
3. 再戦ボタン、タイトルに戻るボタンの処理を実装

**確認項目**:
- [ ] ゲーム終了後、ResultPanelが表示されること
- [ ] 勝者名、終了日数、最終ステータスが正しく表示されること
- [ ] タイトルに戻るボタンでTitleSceneに遷移すること

**参照スクリプト**: `ScriptPlans/8.3.UIController.cs.md`

---

## フェーズ9: 細かい機能の実装

### 9.1 元気不足時の自動ねむる処理

**目的**: 元気が足りない場合、自動的にねむるに変更する

**作業内容**:
1. UIControllerのOnFixButtonClicked()に元気チェックを追加:
   - 選択した行動に必要な元気があるか確認
   - 足りなければ「ねむる」に自動変更
   - ログに警告を表示
2. 選択時にも元気不足を視覚的に表示し、選択できないようにする（実行フェーズのときなどと同じようにボタンを押せない状態にする）

**確認項目**:
- [ ] 実行フェーズ中にその行動を行うための元気が足りない場合（行動によって元気が0より少なくなる場合）、ねむるに行動が自動変更されること
- [ ] ログに「元気が足りないため、ねむるに変更されました」と表示されること

**参照スクリプト**: `ScriptPlans/9.1.UIController.cs.md`

---

### 9.2 設定パネルの実装

**目的**: 音量調整と投了ボタンを実装する

**作業内容**:
1. GameSceneにSettingPanelを追加:
   - 音量調整スライダー（BGM、SE）
   - 投了ボタン
   - 閉じるボタン
2. UIControllerに設定パネル表示/非表示のメソッドを実装
3. 投了ボタンで自分を敗北扱いにする処理を実装

**確認項目**:
- [ ] 設定ボタンでSettingPanelが表示されること
- [ ] 音量スライダーでBGM・SEの音量が変更されること
- [ ] 投了ボタンで自分が敗北してゲームが終了すること

**参照スクリプト**: `ScriptPlans/9.2.UIController.cs.md`

---

### 9.3 行動選択時の効果予測表示

**目的**: 行動ボタンにホバーすると、効果の詳細が表示される

**作業内容**:
1. 各行動ボタンに説明Panelを追加（子オブジェクト）
2. UIControllerに`ShowActionTooltip(ActionType action)`メソッドを実装:
   - 元気増減、重さ増減、状態異常確率を表示
   - 現在のバフを考慮した実際の効果量を計算
3. ホバー時に表示、マウスアウトで非表示

**確認項目**:
- [ ] たべるボタンにホバーすると、「元気-15、重さ+50」と表示されること
- [ ] PlayBuffが10の場合、たべるボタンにホバーすると「重さ+60」と表示されること
- [ ] マウスアウトで説明Panelが非表示になること

**参照スクリプト**: `ScriptPlans/9.3.UIController.cs.md`

---

### 9.4 相手のステータスパネルでの行動効果予測表示

**目的**: 相手のステータスパネルに、各行動時の増減予測を表示する

**作業内容**:
1. OppStatusPanelに各行動の効果予測テキストを追加
2. UIControllerに`UpdateOpponentActionPreview(PlayerRef player)`メソッドを実装:
   - 相手の現在のバフ、状態異常を考慮して各行動の効果を計算
   - テキストとして表示
3. 相手のステータスが変化したら自動更新

**確認項目**:
- [ ] 相手のOppStatusPanelに「たべる: 重さ+50」などの予測が表示されること
- [ ] 相手のバフが変化すると、予測表示も更新されること

**参照スクリプト**: `ScriptPlans/9.4.UIController.cs.md`

---

### 9.5 うーぴょん画像のスケール変更

**目的**: 重さに応じてうーぴょんの画像サイズを変更する

**作業内容**:
1. GameSceneにMyUyopyonImageとOppUyopyonImageを配置
2. UIControllerに`UpdateUyopyonScale(PlayerRef player, int weight)`メソッドを実装:
   - 重さに応じてscaleを調整（例: 1kg = scale 1.0, 700kg = scale 3.0）
3. UyopyonState.Render()から重さ変化時に呼び出す

**確認項目**:
- [ ] たべるを実行すると、うーぴょんの画像が少し大きくなること
- [ ] 重さが700kgに近づくと、画像がかなり大きくなること

**参照スクリプト**: `ScriptPlans/9.5.UIController.cs.md`

---

### 9.6 進化時のビジュアル変更

**目的**: 進化時にうーぴょんのビジュアルを変更する

**作業内容**:
1. UyopyonState.VisualTypeに応じて画像を切り替える処理を実装
2. UIControllerに`UpdateUyopyonVisual(PlayerRef player, string visualType)`メソッドを実装:
   - "UyopyonBaby"または"UyopyonChild"に応じて画像を切り替え
3. 進化時に呼び出す

**確認項目**:
- [ ] 進化前は"UyopyonBaby"の画像が表示されること
- [ ] 進化後は"UyopyonChild"の画像が表示されること

**参照スクリプト**: `ScriptPlans/9.6.UIController.cs.md`

---

## フェーズ10: アニメーションとエフェクトの追加

### 10.1 行動アニメーションの実装

**目的**: 行動実行時にアニメーションを再生する

**作業内容**:
1. UIControllerに`PlayActionAnimation(PlayerRef player, ActionType action)`メソッドを実装
2. 各行動に対応するアニメーションクリップを用意（仮のアニメーションでも可）
3. TurnProcessorから行動実行時に呼び出す

**確認項目**:
- [ ] たべるを実行すると、たべるアニメーションが再生されること
- [ ] アニメーション再生中は2秒待機すること

**参照スクリプト**: `ScriptPlans/10.1.UIController.cs.md`

---

### 10.2 ジャンケン結果エフェクトの実装

**目的**: ジャンケン結果を視覚的に表示する

**作業内容**:
1. GameSceneにジャンケン結果表示用のエフェクトパネルを追加
2. UIControllerに`ShowJankenEffect(PlayerRef winner, Genre winGenre)`メソッドを実装:
   - 勝者側に「WIN!」、敗者側に「LOSE...」を表示
   - 2秒後に非表示
3. TurnProcessorからジャンケン判定後に呼び出す

**確認項目**:
- [ ] ジャンケンで勝つと「WIN!」エフェクトが表示されること
- [ ] ジャンケンで負けると「LOSE...」エフェクトが表示されること
- [ ] あいこの場合は何も表示されないこと

**参照スクリプト**: `ScriptPlans/10.2.UIController.cs.md`

---

### 10.3 状態異常発症時のエフェクトとSE

**目的**: 病気やケガが発症した時のエフェクトとSEを追加する

**作業内容**:
1. UIControllerに`ShowStatusAilmentEffect(PlayerRef player, StatusAilment ailment)`メソッドを実装
2. 発症時にエフェクトアニメーション（例: 画面フラッシュ）とSEを再生
3. TurnProcessorから発症判定後に呼び出す

**確認項目**:
- [ ] 病気が発症すると、画面にエフェクトが表示され、SEが鳴ること
- [ ] ケガが発症すると、画面にエフェクトが表示され、SEが鳴ること

**参照スクリプト**: `ScriptPlans/10.3.UIController.cs.md`

---

### 10.4 BGM・SEの統合

**目的**: ゲーム全体のBGMとSEを統合する

**作業内容**:
1. `Assets/Scripts/Audio/AudioManager.cs`を新規作成
2. シングルトンパターンを実装
3. BGMとSEのAudioClipを管理
4. 各種メソッドを実装:
   - `PlayBGM(string bgmName)`
   - `PlaySE(string seName)`
   - `SetBGMVolume(float volume)`
   - `SetSEVolume(float volume)`
5. 各シーン、各処理から呼び出す

**確認項目**:
- [ ] TitleSceneでタイトルBGMが流れること
- [ ] GameSceneでゲームBGMが流れること
- [ ] ボタンクリック時にSEが鳴ること
- [ ] 設定パネルで音量調整ができること

**参照スクリプト**: `ScriptPlans/10.4.AudioManager.cs.md`

---

## フェーズ11: 最終調整とテスト

### 11.1 数値表示の色付け

**目的**: 正の値は青色・太字、負の値は赤色・太字で表示する

**作業内容**:
1. UIControllerの各表示メソッドで色付けを実装:
   - 正の値: `<color=blue><b>+50</b></color>`
   - 負の値: `<color=red><b>-15</b></color>`
2. ただし、MyStatusPanelとOppStatusPanelの数値は色をつけない

**確認項目**:
- [ ] ログに「重さ+50」と青色で表示されること
- [ ] ログに「元気-15」と赤色で表示されること
- [ ] ステータスパネルの数値は色がつかないこと

**参照スクリプト**: `ScriptPlans/11.1.UIController.cs.md`

---

### 11.2 オーバーレイの排他制御

**目的**: オーバーレイ表示中は他の操作を不可にする

**作業内容**:
1. UIControllerに`SetOverlayActive(bool active)`メソッドを実装:
   - オーバーレイ表示中は他のボタンをinteractable = falseにする
2. SpecialActionChoicePanel、SettingPanel、ResultPanel表示時に呼び出す

**確認項目**:
- [ ] SpecialActionChoicePanel表示中は行動選択ボタンがクリックできないこと
- [ ] SettingPanel表示中は他のボタンがクリックできないこと
- [ ] ResultPanel表示中は他のボタンがクリックできないこと

**参照スクリプト**: `ScriptPlans/11.2.UIController.cs.md`

---

### 11.3 エラーハンドリングとログ出力の強化

**目的**: エラーが発生した際にログで確認できるようにする

**作業内容**:
1. 各スクリプトに適切なDebug.LogとDebug.LogErrorを追加
2. ネットワーク切断時の処理を追加:
   - 相手が切断した場合、自分の勝利として処理
3. パラメーター異常値のチェックを追加:
   - GameParametersのOnValidate()で警告表示

**確認項目**:
- [ ] 各処理でログが出力されること
- [ ] ネットワーク切断時にエラーが発生せず、適切に処理されること
- [ ] GameParametersに異常値を設定すると警告が表示されること

**参照スクリプト**: `ScriptPlans/11.3.GameParameters.cs.md`, `ScriptPlans/11.3.NetworkRunnerHandler.cs.md`

---

### 11.4 総合テスト

**目的**: 全ての機能が正しく動作することを確認する

**作業内容**:
1. 以下のシナリオでテストを実施:
   - 2人でマッチング → ゲーム開始 → 行動選択 → 実行 → 勝利まで
   - 病気・ケガ発症 → つういんで治療
   - 進化 → 特殊能力選択 → 特殊能力実行
   - ジャンケン勝利/敗北/あいこ
   - 連続使用ペナルティ
   - 元気不足時の自動ねむる
   - 熱中症による強制つういん
   - べんきょうの連続ボーナス
2. バグを発見したら修正

**確認項目**:
- [ ] 全ての機能が正しく動作すること
- [ ] エラーが発生しないこと
- [ ] ネットワーク同期が正しく行われること
- [ ] UIが正しく更新されること
- [ ] ログが正しく表示されること
- [ ] アニメーションが正しく再生されること
- [ ] BGM・SEが正しく再生されること

---

## フェーズ12: WebGLビルドとデプロイ

### 12.1 WebGLビルド設定の確認

**目的**: WebGLビルドが正しく設定されていることを確認する

**作業内容**:
1. Build SettingsでWebGLが選択されていることを確認
2. TitleSceneとGameSceneが正しい順序で設定されていることを確認
3. Player Settingsの確認:
   - Company Name、Product Nameの設定
   - WebGL用の最適化設定
4. ビルドを実行

**確認項目**:
- [ ] WebGLビルドが成功すること
- [ ] ビルドサイズが適切であること（必要に応じて圧縮設定を調整）

---

### 12.2 WebGLビルドのテスト

**目的**: WebGLビルドでゲームが正しく動作することを確認する

**作業内容**:
1. ローカルサーバーでWebGLビルドを実行
2. 2つのブラウザタブを開いてマルチプレイテスト
3. 全ての機能が正しく動作することを確認

**確認項目**:
- [ ] WebGLビルドでゲームが起動すること
- [ ] 2人でマッチングできること
- [ ] 全ての機能がエディタと同様に動作すること
- [ ] パフォーマンスが許容範囲であること

---

## 完了

以上で、Odebuchanプロジェクトの実装が完了です。各段階を順番に実装していくことで、エラーが発生した際に原因を特定しやすくなっています。

実装中に不明な点や問題が発生した場合は、AI_REQUIREMENTS.mdとCLAUDE.mdを参照してください。
