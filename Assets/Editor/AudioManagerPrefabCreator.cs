using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// AudioManagerをPrefab化し、GameSceneに配置するエディタツール
/// </summary>
public class AudioManagerPrefabCreator
{
    [MenuItem("Tools/Setup AudioManager Prefab")]
    public static void SetupAudioManagerPrefab()
    {
        // 1. TitleSceneを開く
        var titleScene = EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity", OpenSceneMode.Single);

        // 2. AudioManagerを探す
        GameObject audioManager = GameObject.Find("AudioManager");

        if (audioManager == null)
        {
            Debug.LogError("AudioManager not found in TitleScene!");
            return;
        }

        // 3. Prefabsフォルダが存在しない場合は作成
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // 4. Prefabとして保存
        string prefabPath = "Assets/Prefabs/AudioManager.prefab";

        // 既存のPrefabがあれば削除
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }

        // Prefabを作成
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(audioManager, prefabPath);

        if (prefab == null)
        {
            Debug.LogError("Failed to create AudioManager prefab!");
            return;
        }

        Debug.Log($"AudioManager prefab created successfully at {prefabPath}");

        // 5. GameSceneを開く
        var gameScene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);

        // 6. GameSceneに既にAudioManagerがあるか確認
        GameObject existingAudioManager = GameObject.Find("AudioManager");

        if (existingAudioManager != null)
        {
            Debug.Log("AudioManager already exists in GameScene. Replacing with prefab instance.");
            Object.DestroyImmediate(existingAudioManager);
        }

        // 7. PrefabをGameSceneにインスタンス化
        GameObject prefabInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (prefabInstance == null)
        {
            Debug.LogError("Failed to instantiate AudioManager prefab in GameScene!");
            return;
        }

        Debug.Log("AudioManager prefab instantiated in GameScene successfully!");

        // 8. GameSceneを保存
        EditorSceneManager.SaveScene(gameScene);

        Debug.Log("Setup complete! AudioManager is now available in both TitleScene and GameScene.");

        // 9. TitleSceneに戻してAudioManagerをPrefabインスタンスに置き換え
        EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity", OpenSceneMode.Single);

        // TitleSceneのAudioManagerもPrefabインスタンスに置き換え
        GameObject titleAudioManager = GameObject.Find("AudioManager");
        if (titleAudioManager != null && PrefabUtility.GetPrefabAssetType(titleAudioManager) == PrefabAssetType.NotAPrefab)
        {
            // 元のTransformを保存
            Vector3 position = titleAudioManager.transform.position;
            Quaternion rotation = titleAudioManager.transform.rotation;
            Vector3 scale = titleAudioManager.transform.localScale;

            // 削除して再インスタンス化
            Object.DestroyImmediate(titleAudioManager);
            GameObject newInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            // Transformを復元
            newInstance.transform.position = position;
            newInstance.transform.rotation = rotation;
            newInstance.transform.localScale = scale;

            EditorSceneManager.SaveScene(titleScene);
            Debug.Log("TitleScene AudioManager replaced with prefab instance.");
        }

        AssetDatabase.Refresh();
        Debug.Log("All done! AudioManager prefab is set up in both scenes.");
    }
}
