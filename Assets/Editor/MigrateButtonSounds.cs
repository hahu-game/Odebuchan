using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Reflection;

/// <summary>
/// ボタンのOnClick設定をButtonSoundPlayerからUIButtonSoundに移行するエディタツール
/// </summary>
public class MigrateButtonSounds
{
    [MenuItem("Tools/Migrate Button Sounds to UIButtonSound")]
    public static void MigrateAllButtonSounds()
    {
        string[] scenePaths = new string[]
        {
            "Assets/Scenes/TitleScene.unity",
            "Assets/Scenes/GameScene.unity"
        };

        foreach (string scenePath in scenePaths)
        {
            Debug.Log($"Processing scene: {scenePath}");
            MigrateSceneButtonSounds(scenePath);
        }

        Debug.Log("Migration complete! All buttons now use UIButtonSound.");
    }

    private static void MigrateSceneButtonSounds(string scenePath)
    {
        // シーンを開く
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // シーン内のすべてのButtonコンポーネントを取得
        Button[] buttons = Object.FindObjectsOfType<Button>();

        int migratedCount = 0;

        foreach (Button button in buttons)
        {
            // UIButtonSoundコンポーネントを追加（まだなければ）
            UIButtonSound uiButtonSound = button.GetComponent<UIButtonSound>();
            if (uiButtonSound == null)
            {
                uiButtonSound = button.gameObject.AddComponent<UIButtonSound>();
            }

            // OnClickイベントを確認
            UnityEngine.Events.UnityEvent onClick = button.onClick;
            int eventCount = onClick.GetPersistentEventCount();

            bool modified = false;

            for (int i = 0; i < eventCount; i++)
            {
                // イベントのターゲットを取得
                Object target = onClick.GetPersistentTarget(i);
                string methodName = onClick.GetPersistentMethodName(i);

                // ButtonSoundPlayerの参照を探す
                if (target != null && target.GetType().Name == "ButtonSoundPlayer")
                {
                    // 既存のイベントを削除
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(onClick, i);

                    // 新しいイベントを追加
                    if (methodName == "PlayButtonClickSE")
                    {
                        UnityEditor.Events.UnityEventTools.AddPersistentListener(onClick, uiButtonSound.PlayClickSound);
                        Debug.Log($"Migrated {button.name}: PlayButtonClickSE → PlayClickSound");
                    }
                    else if (methodName == "PlayClearButtonSE")
                    {
                        UnityEditor.Events.UnityEventTools.AddPersistentListener(onClick, uiButtonSound.PlayClearSound);
                        Debug.Log($"Migrated {button.name}: PlayClearButtonSE → PlayClearSound");
                    }
                    else if (methodName == "PlayFriendMatchButtonSE")
                    {
                        // フレンドマッチボタンは通常のクリック音に変更
                        UnityEditor.Events.UnityEventTools.AddPersistentListener(onClick, uiButtonSound.PlayClickSound);
                        Debug.Log($"Migrated {button.name}: PlayFriendMatchButtonSE → PlayClickSound");
                    }

                    modified = true;
                    migratedCount++;

                    // インデックスが変わるので再度チェック
                    i--;
                    eventCount = onClick.GetPersistentEventCount();
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(button);
            }
        }

        // シーンを保存
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"Scene {scenePath}: Migrated {migratedCount} button event(s)");
    }
}
