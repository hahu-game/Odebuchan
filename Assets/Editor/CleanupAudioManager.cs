using UnityEngine;
using UnityEditor;

/// <summary>
/// AudioManager PrefabからButtonSoundPlayerコンポーネントを削除するツール
/// UIButtonSoundへの移行完了後に実行する
/// </summary>
public class CleanupAudioManager
{
    [MenuItem("Tools/Cleanup AudioManager (Remove ButtonSoundPlayer)")]
    public static void RemoveButtonSoundPlayerFromPrefab()
    {
        // AudioManager Prefabをロード
        string prefabPath = "Assets/Prefabs/AudioManager.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"AudioManager prefab not found at {prefabPath}");
            return;
        }

        // Prefabを編集モードで開く
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(assetPath);

        // ButtonSoundPlayerコンポーネントを探して削除
        ButtonSoundPlayer buttonSoundPlayer = prefabContents.GetComponent<ButtonSoundPlayer>();

        if (buttonSoundPlayer != null)
        {
            Object.DestroyImmediate(buttonSoundPlayer);
            Debug.Log("ButtonSoundPlayer component removed from AudioManager prefab");

            // 変更を保存
            PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
            Debug.Log("AudioManager prefab saved successfully");
        }
        else
        {
            Debug.Log("ButtonSoundPlayer component not found on AudioManager prefab (already removed?)");
        }

        // Prefab編集モードを終了
        PrefabUtility.UnloadPrefabContents(prefabContents);

        AssetDatabase.Refresh();
        Debug.Log("Cleanup complete!");
    }
}
