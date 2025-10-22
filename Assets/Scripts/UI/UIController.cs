using Fusion;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// ゲーム内のすべてのUI要素を管理し、ネットワークからの同期データに基づいて表示を更新する。
/// </summary>
public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    // === 自分のUI要素 ===
    public TextMeshProUGUI myNameText;
    public TextMeshProUGUI myWeightText;
    public TextMeshProUGUI myEnergyText;

    // === 相手のUI要素 ===
    public TextMeshProUGUI opponentNameText;
    public TextMeshProUGUI opponentWeightText;
    public TextMeshProUGUI opponentEnergyText;

    private Dictionary<PlayerRef, UyopyonState> _uyopyons = new Dictionary<PlayerRef, UyopyonState>();
    private PlayerRef _localPlayerRef;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        var runner = FindFirstObjectByType<NetworkRunner>();

        if (runner != null && runner.LocalPlayer.PlayerId != 0)
        {
            _localPlayerRef = runner.LocalPlayer;
        }
    }

    /// <summary>
    /// UyopyonState.Spawned()から呼ばれ、うーぴょんの参照を登録する
    /// </summary>
    public void RegisterUyopyon(UyopyonState uyopyon)
    {
        if (uyopyon.OwnerPlayer != PlayerRef.None && !_uyopyons.ContainsKey(uyopyon.OwnerPlayer))
        {
            _uyopyons.Add(uyopyon.OwnerPlayer, uyopyon);

            // 初回表示の更新を強制実行（OnChangedが呼ばれない初期値の設定に対応）
            UpdateWeightDisplay(uyopyon.OwnerPlayer, uyopyon.Weight);
            UpdateEnergyDisplay(uyopyon.OwnerPlayer, uyopyon.Energy);
        }
    }

    // === プレイヤー名更新（NetworkPlayerから呼ばれる） ===
    public void UpdateMyName(string newName) => myNameText.text = newName;
    public void UpdateOpponentName(string newName) => opponentNameText.text = newName;

    // === ステータス更新（UyopyonStateから呼ばれる） ===

    public void UpdateWeightDisplay(PlayerRef player, int weight)
    {
        if (player == _localPlayerRef)
        {
            myWeightText.text = $"重さ: {weight}";
        }
        else
        {
            opponentWeightText.text = $"重さ: {weight}";
        }
    }

    public void UpdateEnergyDisplay(PlayerRef player, int energy)
    {
        if (player == _localPlayerRef)
        {
            myEnergyText.text = $"元気: {energy}%";
        }
        else
        {
            opponentEnergyText.text = $"元気: {energy}%";
        }
    }

    // ... その他、ラウンド表示、メッセージ表示などのメソッド ...
}