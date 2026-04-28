using UnityEngine;
using System.Diagnostics;

/// <summary>
/// 条件付きコンパイルでデバッグログを制御するユーティリティクラス
/// 本番ビルドではログを無効化してパフォーマンスを向上させる
/// </summary>
public static class DebugLogger
{
    /// <summary>
    /// デバッグログを出力（エディタと開発ビルドのみ）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        UnityEngine.Debug.Log(message);
    }

    /// <summary>
    /// デバッグログを出力（オブジェクトコンテキスト付き）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message, Object context)
    {
        UnityEngine.Debug.Log(message, context);
    }

    /// <summary>
    /// 警告ログを出力（エディタと開発ビルドのみ）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    /// <summary>
    /// 警告ログを出力（オブジェクトコンテキスト付き）
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message, Object context)
    {
        UnityEngine.Debug.LogWarning(message, context);
    }

    /// <summary>
    /// エラーログを出力（常に出力）
    /// 本番環境でもエラーは把握する必要があるため
    /// </summary>
    public static void LogError(string message)
    {
        UnityEngine.Debug.LogError(message);
    }

    /// <summary>
    /// エラーログを出力（オブジェクトコンテキスト付き）
    /// </summary>
    public static void LogError(string message, Object context)
    {
        UnityEngine.Debug.LogError(message, context);
    }

    /// <summary>
    /// 詳細なデバッグログを出力（頻繁に呼ばれる箇所用）
    /// VERBOSE_LOGGINGが定義されている場合のみ出力
    /// </summary>
    [Conditional("VERBOSE_LOGGING")]
    public static void LogVerbose(string message)
    {
        UnityEngine.Debug.Log($"[Verbose] {message}");
    }
}
