using UnityEngine;
using System.Collections;

/// <summary>
/// 3.4実装のテスト用スクリプト
/// GameSceneに配置して実行
/// 手動テスト用メソッドはUIボタンから呼び出せます
/// </summary>
public class Test_UIController_34 : MonoBehaviour
{
    [Header("自動テスト設定")]
    [Tooltip("自動テストを実行するか（falseにすると手動テストのみ）")]
    public bool runAutoTest = false;

    private void Start()
    {
        Debug.Log("[Test_34] Start() が呼ばれました");

        if (runAutoTest)
        {
            // UIControllerの初期化を待つためにコルーチンで実行
            StartCoroutine(WaitAndRunTests());
        }
        else
        {
            Debug.Log("[Test_34] 自動テストはスキップされました。手動テスト用メソッドを呼び出してください。");
        }
    }

    private IEnumerator WaitAndRunTests()
    {
        Debug.Log("[Test_34] 5秒待機中...");
        yield return new WaitForSeconds(5f);

        RunTests();
    }

    private void RunTests()
    {
        Debug.Log("=== 3.4 テスト開始 ===");

        if (UIController.Instance == null)
        {
            Debug.LogError("[Test_34] UIController.Instance が null です！GameSceneにUIControllerが配置されているか確認してください。");
            return;
        }

        Debug.Log("[Test_34] UIController.Instance が見つかりました");

        // テスト A: ブラックアウト表示テスト
        Debug.Log("[Test_34] テストA: ブラックアウト表示（2秒間）");
        UIController.Instance.ShowBlackout("テスト表示", 2f);

        // テスト B: ログ追加テスト（3秒後）
        StartCoroutine(RunTestB());

        // テスト C: 複数ログとブラックアウト同時テスト（6秒後）
        StartCoroutine(RunTestC());
    }

    private IEnumerator RunTestB()
    {
        yield return new WaitForSeconds(3f);

        Debug.Log("[Test_34] テストB: ログ追加");
        UIController.Instance.AddLog("テストログ1");
        UIController.Instance.AddLog("テストログ2");
        UIController.Instance.AddLog("テストログ3");
        UIController.Instance.AddLog("これは長いテストログメッセージです。複数行になるか確認。");
    }

    private IEnumerator RunTestC()
    {
        yield return new WaitForSeconds(6f);

        Debug.Log("[Test_34] テストC: 同時動作テスト");
        UIController.Instance.ShowBlackout("【テスト】同時動作確認", 1.5f);
        UIController.Instance.AddLog("ゲーム開始");
        UIController.Instance.AddLog("プレイヤー1が参加しました");
        UIController.Instance.AddLog("プレイヤー2が参加しました");

        Debug.Log("=== 3.4 テスト完了 ===");
    }

    // ===== 以下、UIボタンから呼び出せる手動テスト用メソッド =====

    /// <summary>
    /// ブラックアウトを1秒表示（ボタンから呼び出し可能）
    /// </summary>
    public void ManualTestBlackout()
    {
        if (UIController.Instance != null)
        {
            Debug.Log("[Test_34] 手動テスト: ブラックアウト表示");
            UIController.Instance.ShowBlackout("手動テスト", 1f);
        }
        else
        {
            Debug.LogError("[Test_34] UIController.Instance が null です！");
        }
    }

    /// <summary>
    /// ログを1行追加（ボタンから呼び出し可能）
    /// </summary>
    public void ManualTestAddLog()
    {
        if (UIController.Instance != null)
        {
            Debug.Log("[Test_34] 手動テスト: ログ追加");
            UIController.Instance.AddLog($"手動ログ {Time.time:F1}秒");
        }
        else
        {
            Debug.LogError("[Test_34] UIController.Instance が null です！");
        }
    }

    /// <summary>
    /// ログをクリア（ボタンから呼び出し可能）
    /// </summary>
    public void ManualTestClearLog()
    {
        if (UIController.Instance != null)
        {
            Debug.Log("[Test_34] 手動テスト: ログクリア");
            UIController.Instance.ClearLog();
        }
        else
        {
            Debug.LogError("[Test_34] UIController.Instance が null です！");
        }
    }

    /// <summary>
    /// 複数ログを一度に追加（スクロールテスト用）
    /// </summary>
    public void ManualTestMultipleLogs()
    {
        if (UIController.Instance != null)
        {
            Debug.Log("[Test_34] 手動テスト: 複数ログ追加（スクロールテスト）");
            for (int i = 1; i <= 10; i++)
            {
                UIController.Instance.AddLog($"スクロールテスト用ログ {i}/10");
            }
        }
        else
        {
            Debug.LogError("[Test_34] UIController.Instance が null です！");
        }
    }
}
