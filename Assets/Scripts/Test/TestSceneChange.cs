using System.Collections;
using UnityEngine;

public class TestSceneChange : MonoBehaviour
{


    public void OnTestSceneChangeButtonClicked()
    {
        TestSceneChangeButton();
    }
    public void TestSceneChangeButton()
    {
        try
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
            Debug.Log("[UIController] LoadSceneåƒÇ—èoÇµäÆóπ");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIController] LoadSceneíÜÇ…ó·äOî≠ê∂: {e.Message}");
        }
    }
}
