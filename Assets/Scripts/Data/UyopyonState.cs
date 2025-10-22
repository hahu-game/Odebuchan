using Fusion;
using UnityEngine;

/// <summary>
/// うーぴょんのアバターであり、育成ステータスをネットワーク同期する。
/// </summary>
public class UyopyonState : NetworkBehaviour
{
    // [Networked(OnChanged = ...)] を削除
    [Networked]
    public PlayerRef OwnerPlayer { get; set; }

    [Networked]
    public int Weight { get; set; } = 10; // 初期値

    [Networked]
    public int Energy { get; set; } = 50; // 初期値

    [Networked]
    public byte CurrentStatus { get; set; } = 0;

    private int _lastWeight;
    private int _lastEnergy;

    public override void Spawned()
    {
        // UIControllerにこのオブジェクトの参照を登録
        if (UIController.Instance != null)
        {
            UIController.Instance.RegisterUyopyon(this);
        }

        // 初期値を設定
        _lastWeight = Weight;
        _lastEnergy = Energy;

        // UIの初期表示をトリガー
        UpdateDisplay(Weight, Energy);
    }

    /// <summary>
    /// 全クライアントで実行されるレンダリング処理。データの変更をチェックする。
    /// </summary>
    public override void Render()
    {
        // Weightの変更をチェック
        if (_lastWeight != Weight)
        {
            UIController.Instance.UpdateWeightDisplay(OwnerPlayer, Weight);
            _lastWeight = Weight;
        }

        // Energyの変更をチェック
        if (_lastEnergy != Energy)
        {
            UIController.Instance.UpdateEnergyDisplay(OwnerPlayer, Energy);
            _lastEnergy = Energy;
        }
    }

    /// <summary>
    /// UIControllerに通知して表示を更新するヘルパーメソッド（Spawnedでの初期表示用）
    /// </summary>
    private void UpdateDisplay(int weight, int energy)
    {
        if (UIController.Instance != null)
        {
            UIController.Instance.UpdateWeightDisplay(OwnerPlayer, weight);
            UIController.Instance.UpdateEnergyDisplay(OwnerPlayer, energy);
        }
    }

}