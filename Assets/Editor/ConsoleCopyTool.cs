using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

public static class ConsoleCopyTool
{
    [MenuItem("Tools/Copy Visible Console Logs")]
    public static void CopyConsoleLogs()
    {
        // UnityEditor.LogEntries クラスを取得
        var logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
        var getCount = logEntries.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
        var getEntry = logEntries.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
        var startGettingEntries = logEntries.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
        var endGettingEntries = logEntries.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);

        var entryType = Type.GetType("UnityEditor.LogEntry,UnityEditor.dll");
        var entry = Activator.CreateInstance(entryType);

        int count = (int)getCount.Invoke(null, null);
        StringBuilder sb = new StringBuilder();

        // タグ除去用の正規表現
        Regex tagPattern = new Regex("<.*?>", RegexOptions.Compiled);

        startGettingEntries.Invoke(null, null);
        for (int i = 0; i < count; i++)
        {
            getEntry.Invoke(null, new object[] { i, entry });
            string message = entryType.GetField("message").GetValue(entry).ToString();

            // 改行がある場合は最初の行だけ
            string line = message.Split('\n')[0];

            // <color=...>や<b>などのタグを除去
            line = tagPattern.Replace(line, string.Empty);

            // 前後の空白をトリム
            line = line.Trim();

            sb.AppendLine(line);
        }
        endGettingEntries.Invoke(null, null);

        // クリップボードにコピー
        EditorGUIUtility.systemCopyBuffer = sb.ToString();

        Debug.Log($"Console logs (formatted) copied to clipboard ({count} entries)");
    }
}
