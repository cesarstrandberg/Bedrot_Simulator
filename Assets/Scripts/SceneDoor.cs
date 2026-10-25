using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Invisible trigger box: walk in -> fade to black -> load targetScene -> appear at the SpawnPoint
// with id = spawnPointId -> fade back in. Put it on a GameObject with a trigger collider.
public class SceneDoor : MonoBehaviour
{
    public string targetScene = "City_Environment";
    public string spawnPointId = "City_FrontDoor";
    public float fadeDuration = 0.6f;

    static bool transitioning;
    static float ignoreUntil;   // stops you bouncing straight back if you land in/near a door

    void OnTriggerEnter(Collider other)
    {
        if (transitioning || Time.unscaledTime < ignoreUntil) return;
        if (other.GetComponentInParent<PlayerMovement>() == null) return;

        transitioning = true;
        var runner = new GameObject("SceneTransition").AddComponent<SceneTransitionRunner>();
        DontDestroyOnLoad(runner.gameObject);
        runner.StartCoroutine(runner.Run(targetScene, spawnPointId, fadeDuration));
    }

    // Lives across the scene load so the fade can finish on the other side.
    class SceneTransitionRunner : MonoBehaviour
    {
        public IEnumerator Run(string scene, string spawnId, float fade)
        {
            Image black = BuildOverlay();

            yield return Fade(black, 0f, 1f, fade);

            PlayerInteraction hands = FindFirstObjectByType<PlayerInteraction>();
            if (hands != null) hands.PocketHeldJoint();
            GameState.Save();
            GameState.nextSpawnPoint = spawnId;
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null; // let the new scene's Awake/Start run (stats/clock read GameState there)

            PlacePlayer(spawnId);
            ignoreUntil = Time.unscaledTime + 1.5f;

            yield return Fade(black, 1f, 0f, fade);
            transitioning = false;
            Destroy(gameObject);
        }

        static void PlacePlayer(string spawnId)
        {
            SpawnPoint target = null;
            foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
                if (sp.id == spawnId) target = sp;

            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (target == null || player == null)
            {
                Debug.LogWarning("SceneDoor: no SpawnPoint '" + spawnId + "' or no player in this scene.");
                return;
            }

            // SpawnPoints sit on the floor; the player's pivot is the middle of its capsule.
            float halfHeight = 1f;
            CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
            if (capsule != null) halfHeight = capsule.height * 0.5f * player.transform.lossyScale.y + 0.05f;
            Vector3 pos = target.transform.position + Vector3.up * halfHeight;

            Quaternion yaw = Quaternion.Euler(0f, target.transform.eulerAngles.y, 0f);
            player.transform.SetPositionAndRotation(pos, yaw);
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = pos;
                rb.rotation = yaw;
                rb.linearVelocity = Vector3.zero;
            }
        }

        Image BuildOverlay()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var imgGO = new GameObject("Black");
            imgGO.transform.SetParent(transform, false);
            var img = imgGO.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        static IEnumerator Fade(Image img, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                img.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
            img.color = new Color(0f, 0f, 0f, to);
        }
    }
}
