using UnityEngine;

public class GoToOpenPackScene: MonoBehaviour
{
    public void GoToCollectionScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("CollectionScene");
    }
    
}
