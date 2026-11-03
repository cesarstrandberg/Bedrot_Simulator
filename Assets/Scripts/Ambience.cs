using UnityEngine;
using UnityEngine.SceneManagement;

// Background sound that's always on in a scene, so it's never dead silent: room tone in the lecture
// hall, distant traffic in the city. Builds itself (no scene wiring). The sounds are made in code for
// now (ProceduralAudio) - to use a real recording, put it in Resources/ named Ambience_<SceneName>
// (e.g. Resources/Ambience_LectureHall.wav) and it's picked over the generated one.
public class Ambience : MonoBehaviour
{
    static Ambience instance;
    AudioSource source;
    AudioClip roomTone, traffic;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn()
    {
        if (instance != null) return;
        var go = new GameObject("Ambience");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<Ambience>();
        instance.source = go.AddComponent<AudioSource>();
        instance.source.loop = true;
        instance.source.playOnAwake = false;
        instance.source.spatialBlend = 0f;
        SceneManager.sceneLoaded += (s, m) => instance.ForScene(s.name);
        instance.ForScene(SceneManager.GetActiveScene().name);
    }

    void ForScene(string scene)
    {
        AudioClip clip = Resources.Load<AudioClip>("Ambience_" + scene);
        // Lecture hall = students murmuring (Resources/Ambience_LectureHall.wav), kept very low
        float volume = scene == "LectureHall" ? 0.06f : 0.25f;
        if (clip == null)
        {
            switch (scene)
            {
                case "LectureHall":
                    clip = roomTone != null ? roomTone : (roomTone = ProceduralAudio.RoomTone());
                    volume = 0.12f;
                    break;
                case "City_Environment":
                    clip = traffic != null ? traffic : (traffic = ProceduralAudio.CityTraffic());
                    volume = 0.18f;
                    break;
            }
        }

        if (clip == null) { source.Stop(); source.clip = null; return; }
        if (source.clip == clip && source.isPlaying) return;
        source.clip = clip;
        source.volume = volume;
        source.Play();
    }
}
