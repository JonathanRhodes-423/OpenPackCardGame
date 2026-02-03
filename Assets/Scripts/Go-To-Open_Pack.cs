using UnityEngine;

public class GoToOpenPack: MonoBehaviour
{
    public void GoToPackOpening()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("PackOpening");
    }

}