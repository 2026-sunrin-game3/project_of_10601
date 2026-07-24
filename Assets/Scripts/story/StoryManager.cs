using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryManager : MonoBehaviour
{
    [SerializeField] private GameObject[] storyObjects;
    [SerializeField] private float changeInterval = 2f;

    [SerializeField] private string nextSceneName = "SampleScene";

    private void Start()
    {
        StartCoroutine(ShowStory());
    }

    private IEnumerator ShowStory()
    {
        HideAllObjects();

        for (int i = 0; i < storyObjects.Length; i++)
        {
            HideAllObjects();

            storyObjects[i].SetActive(true);

            yield return new WaitForSeconds(changeInterval);
        }

        // 마지막 컷 숨기기 (선택사항)
        HideAllObjects();

        SceneManager.LoadScene(nextSceneName);
    }

    private void HideAllObjects()
    {
        foreach (GameObject storyObject in storyObjects)
        {
            if (storyObject != null)
            {
                storyObject.SetActive(false);
            }
        }
    }
}
