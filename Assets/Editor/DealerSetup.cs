using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class DealerSetup
{
    const string WalkingFbxPath = "Assets/3D_Models/Characters/DrugDealer/Walking.fbx";
    const string ControllerPath = "Assets/Animations/DealerAnimator.controller";
    const string JarPrefabPath = "Assets/Prefab/weed_jar.prefab";

    [MenuItem("Bedrot/Setup Dealer Situation")]
    public static void SetupDealerSituation()
    {
        Transform situation = FindInactive("Dealer_Situation");
        if (situation == null)
        {
            Debug.LogError("DealerSetup: Could not find 'Dealer_Situation' in the open scene. Open ApartmentScene first.");
            return;
        }

        Transform dealer = FindRecursive(situation, "DrugDealer");
        if (dealer == null)
        {
            Debug.LogError("DealerSetup: Could not find 'DrugDealer' under 'Dealer_Situation'.");
            return;
        }

        Transform stopPos = FindRecursive(situation, "Dealer_StopPos");
        if (stopPos == null)
        {
            Debug.LogError("DealerSetup: Could not find 'Dealer_StopPos' under 'Dealer_Situation'.");
            return;
        }

        GameObject dealerGO = dealer.gameObject;

        NavMeshAgent agent = dealerGO.GetComponent<NavMeshAgent>();
        if (agent == null) agent = dealerGO.AddComponent<NavMeshAgent>();
        agent.stoppingDistance = 0.3f;

        if (dealerGO.GetComponent<Collider>() == null)
        {
            CapsuleCollider col = dealerGO.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.35f;
            col.center = new Vector3(0f, 0.9f, 0f);
        }

        DealerAI dealerAI = dealerGO.GetComponent<DealerAI>();
        if (dealerAI == null) dealerAI = dealerGO.AddComponent<DealerAI>();
        dealerAI.stopPoint = stopPos;

        DealerClickable clickable = dealerGO.GetComponent<DealerClickable>();
        if (clickable == null) clickable = dealerGO.AddComponent<DealerClickable>();
        clickable.dealerAI = dealerAI;

        DrugSite drugSite = Object.FindFirstObjectByType<DrugSite>(FindObjectsInactive.Include);
        if (drugSite != null)
        {
            drugSite.dealerAI = dealerAI;
            dealerAI.drugSite = drugSite;
            EditorUtility.SetDirty(drugSite);
        }
        else
        {
            Debug.LogWarning("DealerSetup: Could not find a DrugSite in the scene to link the dealer to. Assign DrugSite.dealerAI manually.");
        }

        SetupAnimator(dealerGO);
        SetupJarInHand(dealerAI, dealerGO);

        NavMeshSurface surface = situation.GetComponent<NavMeshSurface>();
        if (surface == null) surface = situation.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.BuildNavMesh();

        EditorUtility.SetDirty(dealerGO);
        EditorUtility.SetDirty(situation.gameObject);
        EditorSceneManager.MarkSceneDirty(situation.gameObject.scene);

        Selection.activeGameObject = dealerGO;
        Debug.Log("DealerSetup: Dealer wired up (NavMeshAgent, DealerClickable, Animator, jar-in-hand, DrugSite link) and NavMesh baked over the whole scene. " +
                   "Save the scene (Ctrl+S) to keep it.");
    }

    static void SetupAnimator(GameObject dealerGO)
    {
        Animator animator = dealerGO.GetComponent<Animator>();
        if (animator == null) animator = dealerGO.AddComponent<Animator>();

        AnimationClip walkClip = FindClipInFbx(WalkingFbxPath);
        if (walkClip == null)
        {
            Debug.LogWarning("DealerSetup: Could not find a walking AnimationClip inside " + WalkingFbxPath + ".");
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

            AnimatorStateMachine rootSM = controller.layers[0].stateMachine;
            AnimatorState idle = rootSM.AddState("Idle");
            AnimatorState walk = rootSM.AddState("Walk");
            walk.motion = walkClip;
            rootSM.defaultState = idle;

            AnimatorStateTransition toWalk = idle.AddTransition(walk);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.15f;
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            AnimatorStateTransition toIdle = walk.AddTransition(idle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.15f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        }
        else if (walkClip != null)
        {
            // Om kontrollern redan fanns men saknade klippet (t.ex. skapad innan Walking.fbx importerades)
            foreach (ChildAnimatorState s in controller.layers[0].stateMachine.states)
            {
                if (s.state.name == "Walk") s.state.motion = walkClip;
            }
        }

        animator.runtimeAnimatorController = controller;
    }

    static AnimationClip FindClipInFbx(string fbxPath)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (o is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }
        return null;
    }

    static void SetupJarInHand(DealerAI dealerAI, GameObject dealerGO)
    {
        if (dealerAI.weedJarInHand != null) return;

        Transform hand = FindHandBone(dealerGO.transform);
        if (hand == null)
        {
            Debug.LogWarning("DealerSetup: Could not find a hand bone on DrugDealer to attach the jar to. Assign DealerAI.weedJarInHand manually.");
            return;
        }

        GameObject jarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(JarPrefabPath);
        if (jarPrefab == null)
        {
            Debug.LogWarning("DealerSetup: Could not find weed_jar prefab at " + JarPrefabPath);
            return;
        }

        GameObject jarInstance = (GameObject)PrefabUtility.InstantiatePrefab(jarPrefab, hand);
        jarInstance.transform.localPosition = Vector3.zero;
        jarInstance.transform.localRotation = Quaternion.identity;

        dealerAI.weedJarInHand = jarInstance;
    }

    static Transform FindHandBone(Transform root)
    {
        Transform fallback = null;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (!n.Contains("hand")) continue;

            if (n.Contains("right") || n.Contains("_r") || n.Contains(".r") || n.Contains(" r"))
            {
                return t;
            }

            if (fallback == null) fallback = t;
        }
        return fallback;
    }

    static Transform FindInactive(string name)
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform result = FindRecursive(root.transform, name);
            if (result != null) return result;
        }
        return null;
    }

    static Transform FindRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
