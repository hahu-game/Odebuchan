using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// GameSceneからAudioManagerを削除するツール
/// TitleSceneから開始する前提の場合に使用
/// </summary>
public class RemoveAudioManagerFromGameScene
{
    [MenuItem("Tools/Remove AudioManager from GameScene")]
    public static void RemoveAudioManager()
    {
        // GameSceneを開く
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);

        // AudioManagerを探す
        GameObject audioManager = GameObject.Find("AudioManager");

        if (audioManager != null)
        {
            // 削除
            Object.DestroyImmediate(audioManager);
            Debug.Log("AudioManager removed from GameScene");

            // シーンを保存
            EditorSceneManager.SaveScene(scene);
            Debug.Log("GameScene saved");

            Debug.LogWarning("Note: GameSceneを直接Play実行すると音が鳴りません。常にTitleSceneから開始してください。");
        }
        else
        {
            Debug.Log("AudioManager not found in GameScene (already removed?)");
        }
    }
}
