using Fusion;
using UnityEngine;

/// <summary>
/// プレイヤーの行動選択データを管理するクラス
/// 各プレイヤーに1つずつスポーンされる
/// </summary>
public class PlayerActionData : NetworkBehaviour
{
    /// <summary>
    /// このデータの所有者
    /// </summary>
    [Networked]
    public PlayerRef OwnerPlayer { get; set; }

    /// <summary>
    /// 午前の行動
    /// </summary>
    [Networked]
    public ActionData MorningAction { get; set; }

    /// <summary>
    /// 午後の行動
    /// </summary>
    [Networked]
    public ActionData AfternoonAction { get; set; }

    /// <summary>
    /// 行動が確定されたか（確定ボタンが押されたか）
    /// </summary>
    [Networked]
    public bool IsActionFixed { get; set; } = false;

    /// <summary>
    /// 前日の午後の行動（連続使用ペナルティ判定用）
    /// </summary>
    [Networked]
    public ActionData LastAfternoonAction { get; set; }

    /// <summary>
    /// 午前の行動がロックされているか（熱中症による強制つういん）
    /// </summary>
    [Networked]
    public bool MorningActionLocked { get; set; } = false;

    /// <summary>
    /// 午後の行動がロックされているか（熱中症による強制つういん）
    /// </summary>
    [Networked]
    public bool AfternoonActionLocked { get; set; } = false;

    // 変更検知用のフィールド
    private ActionData _lastMorningAction;
    private ActionData _lastAfternoonAction;
    private bool _lastIsActionFixed;
    private ActionData _lastLastAfternoonAction;

    public override void Spawned()
    {
        // OwnerPlayerはGameManager.SpawnPlayerActionData()で設定される

        // 初期値として空の状態を設定
        MorningAction = ActionData.Empty();
        AfternoonAction = ActionData.Empty();
        LastAfternoonAction = ActionData.Empty();

        // 変更検知用のフィールドを無効な値で初期化（初期値の変更を確実に検知するため）
        _lastMorningAction = new ActionData(ActionType.SpecialAbility, Genre.None);
        _lastAfternoonAction = new ActionData(ActionType.SpecialAbility, Genre.None);
        _lastIsActionFixed = false;
        _lastLastAfternoonAction = new ActionData(ActionType.SpecialAbility, Genre.None);

        // GameManagerに登録（全クライアントで実行）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayerActionData(this);
        }

        DebugLogger.Log($"[PlayerActionData] Player {OwnerPlayer} Spawned: Morning={MorningAction.Type}, Afternoon={AfternoonAction.Type}");
    }

    /// <summary>
    /// 毎フレーム呼ばれる。ネットワーク化されたプロパティの変更を検知してUIを更新する
    /// </summary>
    public override void Render()
    {
        // 自分のプレイヤーのデータかどうかをチェック（Runnerプロパティを使用）
        bool isMyPlayer = (Runner != null && Runner.LocalPlayer == OwnerPlayer);

        // GameFlowManagerから現在のフェーズを取得
        bool isExecutionPhase = (GameFlowManager.Instance != null &&
                                  GameFlowManager.Instance.CurrentPhase == GamePhase.Execution);

        // 午前の行動が変更された場合
        if (!_lastMorningAction.Equals(MorningAction))
        {
            DebugLogger.Log($"[PlayerActionData] Player {OwnerPlayer} の午前の行動が変更されました: {_lastMorningAction.Type} -> {MorningAction.Type}");

            if (UIController.Instance != null)
            {
                // 自分のプレイヤー、または実行フェーズの場合のみ表示
                if (isMyPlayer || isExecutionPhase)
                {
                    // 確定していない場合は空欄を表示
                    if (!IsActionFixed)
                    {
                        UIController.Instance.UpdateActionDisplay(OwnerPlayer, true, ActionData.Empty());
                    }
                    else
                    {
                        UIController.Instance.UpdateActionDisplay(OwnerPlayer, true, MorningAction);
                    }
                }
            }

            _lastMorningAction = MorningAction;
        }

        // 午後の行動が変更された場合
        if (!_lastAfternoonAction.Equals(AfternoonAction))
        {
            DebugLogger.Log($"[PlayerActionData] Player {OwnerPlayer} の午後の行動が変更されました: {_lastAfternoonAction.Type} -> {AfternoonAction.Type}");

            if (UIController.Instance != null)
            {
                // 自分のプレイヤー、または実行フェーズの場合のみ表示
                if (isMyPlayer || isExecutionPhase)
                {
                    // 確定していない場合は空欄を表示
                    if (!IsActionFixed)
                    {
                        UIController.Instance.UpdateActionDisplay(OwnerPlayer, false, ActionData.Empty());
                    }
                    else
                    {
                        UIController.Instance.UpdateActionDisplay(OwnerPlayer, false, AfternoonAction);
                    }
                }
            }

            _lastAfternoonAction = AfternoonAction;
        }

        // 確定フラグが変更された場合
        if (_lastIsActionFixed != IsActionFixed)
        {
            DebugLogger.Log($"[PlayerActionData] Player {OwnerPlayer} の確定フラグが変更されました: {_lastIsActionFixed} -> {IsActionFixed}");

            // 確定フラグが変更された場合、UIを更新
            if (UIController.Instance != null)
            {
                // 自分のプレイヤー、または実行フェーズの場合のみ表示
                if (isMyPlayer || isExecutionPhase)
                {
                    if (!IsActionFixed)
                    {
                        // 確定解除された場合は空欄を表示
                        UIController.Instance.UpdateActionDisplay(OwnerPlayer, true, ActionData.Empty());
                        UIController.Instance.UpdateActionDisplay(OwnerPlayer, false, ActionData.Empty());
                    }
                    else
                    {
                        // 確定された場合は現在の行動を表示
                        UIController.Instance.UpdateActionDisplay(OwnerPlayer, true, MorningAction);
                        UIController.Instance.UpdateActionDisplay(OwnerPlayer, false, AfternoonAction);
                    }
                }
            }

            _lastIsActionFixed = IsActionFixed;
        }

        // 昨日の午後の行動が変更された場合
        if (!_lastLastAfternoonAction.Equals(LastAfternoonAction))
        {
            DebugLogger.Log($"[PlayerActionData] Player {OwnerPlayer} の昨日の午後の行動が変更されました: {_lastLastAfternoonAction.Type} -> {LastAfternoonAction.Type}");

            if (UIController.Instance != null)
            {
                // 昨日の午後の行動は常に全員に表示（ActionData全体を渡す）
                UIController.Instance.UpdateYesterdayAfternoonDisplay(OwnerPlayer, LastAfternoonAction);
            }

            _lastLastAfternoonAction = LastAfternoonAction;
        }
    }


    /// <summary>
    /// Despawn時にGameManagerから登録解除
    /// </summary>
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterPlayerActionData(OwnerPlayer);
        }
    }

    /// <summary>
    /// 行動をリセット（新しい日の開始時に呼び出す）
    /// </summary>
    public void ResetForNewDay()
    {
        if (Object.HasStateAuthority)
        {
            // 前日の午後の行動を保存
            LastAfternoonAction = AfternoonAction;

            // 午前・午後を空の状態にリセット
            MorningAction = ActionData.Empty();
            AfternoonAction = ActionData.Empty();

            // 確定フラグをリセット
            IsActionFixed = false;

            // 午後のロックフラグをリセット（午前のロックは熱中症処理で個別に管理）
            AfternoonActionLocked = false;

            // 全クライアントで自分と相手の今日の行動を空欄にする
            RPC_ClearActionDisplay();

            DebugLogger.Log($"[PlayerActionData] Player {OwnerPlayer} の行動をリセットしました");
        }
    }

    /// <summary>
    /// 全クライアントで行動表示を空欄にクリアする
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ClearActionDisplay()
    {
        if (UIController.Instance != null)
        {
            DebugLogger.Log($"[PlayerActionData] Player {OwnerPlayer} の行動表示を空欄にクリアします");

            // 午前・午後の行動表示を空欄に
            UIController.Instance.UpdateActionDisplay(OwnerPlayer, true, ActionData.Empty());
            UIController.Instance.UpdateActionDisplay(OwnerPlayer, false, ActionData.Empty());
        }
    }

    /// <summary>
    /// 午前の行動を設定（クライアント側からRPC経由で呼び出す）
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetMorningAction(ActionData action, RpcInfo info = default)
    {
        // セキュリティチェック: 送信元が自分のOwnerPlayerの場合のみ許可
        if (info.Source != OwnerPlayer)
        {
            Debug.LogWarning($"[PlayerActionData] 不正なRPC: Player={info.Source}が Player={OwnerPlayer}の行動を変更しようとしました");
            return;
        }

        if (!MorningActionLocked)
        {
            MorningAction = action;
            DebugLogger.Log($"[PlayerActionData] Player {OwnerPlayer} の午前の行動を設定: {action.Type}");
        }
        else
        {
            Debug.LogWarning($"[PlayerActionData] Player {OwnerPlayer} の午前の行動はロックされています");
        }
    }

    /// <summary>
    /// 午後の行動を設定（クライアント側からRPC経由で呼び出す）
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetAfternoonAction(ActionData action, RpcInfo info = default)
    {
        // セキュリティチェック: 送信元が自分のOwnerPlayerの場合のみ許可
        if (info.Source != OwnerPlayer)
        {
            Debug.LogWarning($"[PlayerActionData] 不正なRPC: Player={info.Source}が Player={OwnerPlayer}の行動を変更しようとしました");
            return;
        }

        if (!AfternoonActionLocked)
        {
            AfternoonAction = action;
            DebugLogger.Log($"[PlayerActionData] Player {OwnerPlayer} の午後の行動を設定: {action.Type}");
        }
        else
        {
            Debug.LogWarning($"[PlayerActionData] Player {OwnerPlayer} の午後の行動はロックされています");
        }
    }

    /// <summary>
    /// 行動を確定（クライアント側からRPC経由で呼び出す）
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_FixActions(RpcInfo info = default)
    {
        // セキュリティチェック: 送信元が自分のOwnerPlayerの場合のみ許可
        if (info.Source != OwnerPlayer)
        {
            Debug.LogWarning($"[PlayerActionData] 不正なRPC: Player={info.Source}が Player={OwnerPlayer}の行動を確定しようとしました");
            return;
        }

        IsActionFixed = true;
        DebugLogger.Log($"[PlayerActionData] Player {OwnerPlayer} の行動が確定されました");
    }

    /// <summary>
    /// 行動をクリア（クライアント側からRPC経由で呼び出す）
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ClearActions(RpcInfo info = default)
    {
        // セキュリティチェック: 送信元が自分のOwnerPlayerの場合のみ許可
        if (info.Source != OwnerPlayer)
        {
            Debug.LogWarning($"[PlayerActionData] 不正なRPC: Player={info.Source}が Player={OwnerPlayer}の行動をクリアしようとしました");
            return;
        }

        if (!MorningActionLocked)
        {
            MorningAction = ActionData.Empty();
        }
        if (!AfternoonActionLocked)
        {
            AfternoonAction = ActionData.Empty();
        }
        IsActionFixed = false;
        DebugLogger.Log($"[PlayerActionData] Player {OwnerPlayer} の行動がクリアされました");
    }
}
