using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameMusicManager : MonoBehaviour
{
    const string MenuScene = "startscene";
    const string StoryScene = "story";
    const string PhaseOneScene = "SampleScene";
    const string PhaseThreeScene = "BattleScene2";
    const string MenuMusicPath = "MUSIC/apalonbeats-battle-battle-music-549411";
    const string BattleMusicPath = "MUSIC/the_mountain-battle-music-179502";

    static GameMusicManager instance;
    AudioSource source;
    AudioClip menuMusic;
    AudioClip battleMusic;
    bool battleFinished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (instance != null) return;
        GameObject musicObject = new GameObject("Persistent Game Music");
        instance = musicObject.AddComponent<GameMusicManager>();
        DontDestroyOnLoad(musicObject);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.volume = .65f;
        menuMusic = Resources.Load<AudioClip>(MenuMusicPath);
        battleMusic = Resources.Load<AudioClip>(BattleMusicPath);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == MenuScene)
        {
            battleFinished = false;
            PlayIfChanged(menuMusic);
            return;
        }

        if (scene.name == StoryScene)
            battleFinished = false;

        if (!battleFinished &&
            (scene.name == StoryScene || scene.name == PhaseOneScene || scene.name == PhaseThreeScene))
            PlayIfChanged(battleMusic);
    }

    void PlayIfChanged(AudioClip clip)
    {
        if (source == null || clip == null) return;
        if (source.clip == clip && source.isPlaying) return;
        source.Stop();
        source.clip = clip;
        source.time = 0f;
        source.Play();
    }

    public static void StopBattleMusic()
    {
        if (instance == null) return;
        instance.battleFinished = true;
        if (instance.source != null && instance.source.clip == instance.battleMusic)
            instance.source.Stop();
    }

    void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }
}
