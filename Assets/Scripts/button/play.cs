using UnityEngine;
using UnityEngine.SceneManagement;

public class play : MonoBehaviour
{
    public void scenechange()
    {
        SceneManager.LoadScene("start");
    }
    
}
