using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ApartmentFloorColliderSetup
{
    [MenuItem("Bedrot/Add Floor Collider to llApartment")]
    public static void AddFloorCollider()
    {
        Transform apartmentRoot = FindInactive("llApartment");
        if (apartmentRoot == null)
        {
            Debug.LogError("ApartmentFloorColliderSetup: Could not find 'llApartment' in the open scene.");
            return;
        }

        Transform floor = FindRecursive(apartmentRoot, "apartmentFloor");
        if (floor == null)
        {
            Debug.LogError("ApartmentFloorColliderSetup: Could not find 'apartmentFloor' under llApartment.");
            return;
        }

        MeshCollider existing = floor.GetComponent<MeshCollider>();
        if (existing != null)
        {
            Debug.LogWarning("ApartmentFloorColliderSetup: apartmentFloor already has a MeshCollider. Nothing changed.");
            Selection.activeGameObject = floor.gameObject;
            return;
        }

        MeshCollider collider = floor.gameObject.AddComponent<MeshCollider>();
        collider.convex = false;

        EditorUtility.SetDirty(floor.gameObject);
        EditorSceneManager.MarkSceneDirty(floor.gameObject.scene);

        Selection.activeGameObject = floor.gameObject;
        Debug.Log("ApartmentFloorColliderSetup: Added MeshCollider to apartmentFloor only. Save the scene (Ctrl+S) to keep it.");
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
