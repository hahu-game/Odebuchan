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

    public override void Spawned()
    {
        // InputAuthorityから所有者を設定
        OwnerPlayer = Object.InputAuthority;
        
        // 初期値としてねむるを設定
        MorningAction = ActionData.Default();
        AfternoonAction = ActionData.Default();
        LastAfternoonAction = ActionData.Default();

        Debug.Log($"[PlayerActionData] Spawned for Player {OwnerPlayer}");
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

            // 午前・午後をデフォルト（ねむる）にリセット
            MorningAction = ActionData.Default();
            AfternoonAction = ActionData.Default();

            // 確定フラグをリセット
            IsActionFixed = false;

            // 午後のロックフラグをリセット（午前のロックは熱中症処理で個別に管理）
            AfternoonActionLocked = false;

            Debug.Log($"[PlayerActionData] Player {OwnerPlayer} の行動をリセットしました");
        }
    }

    /// <summary>
    /// 午前の行動を設定（クライアント側からRPC経由で呼び出す）
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetMorningAction(ActionData action)
    {
        if (!MorningActionLocked)
        {
            MorningAction = action;
            Debug.Log($"[PlayerActionData] Player {OwnerPlayer} の午前の行動を設定: {action.Type}");
        }
        else
        {
            Debug.LogWarning($"[PlayerActionData] Player {OwnerPlayer} の午前の行動はロックされています");
        }
    }

    /// <summary>
    /// 午後の行動を設定（クライアント側からRPC経由で呼び出す）
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetAfternoonAction(ActionData action)
    {
        if (!AfternoonActionLocked)
        {
            AfternoonAction = action;
            Debug.Log($"[PlayerActionData] Player {OwnerPlayer} の午後の行動を設定: {action.Type}");
        }
        else
        {
            Debug.LogWarning($"[PlayerActionData] Player {OwnerPlayer} の午後の行動はロックされています");
        }
    }

    /// <summary>
    /// 行動を確定（クライアント側からRPC経由で呼び出す）
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_FixActions()
    {
        IsActionFixed = true;
        Debug.Log($"[PlayerActionData] Player {OwnerPlayer} の行動が確定されました");
    }

    /// <summary>
    /// 行動をクリア（クライアント側からRPC経由で呼び出す）
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ClearActions()
    {
        if (!MorningActionLocked)
        {
            MorningAction = ActionData.Default();
        }
        if (!AfternoonActionLocked)
        {
            AfternoonAction = ActionData.Default();
        }
        IsActionFixed = false;
        Debug.Log($"[PlayerActionData] Player {OwnerPlayer} の行動がクリアされました");
    }
}
