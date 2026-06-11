using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InstallCrashTestTree
{
    private const string TargetScenePath = "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Scenes/demo_city_night.unity";
    private const string TreePrefabPath = "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/tree_1.prefab";
    private const string ParentName = "CollisionTestObjects";
    private const string TreeNamePrefix = "CrashTestTree_";
    private const string PreferredReferenceVehicleName = "Car4";
    private const float TreeGroundLift = 2.6f;
    private const int TreeCount = 3;
    private const float FirstTreeDistance = 20f;
    private const float TreeSpacing = 20f;

    [InitializeOnLoadMethod]
    private static void AutoInstallIfTargetSceneIsOpen()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != TargetScenePath || HasInstalledTree(scene))
            {
                return;
            }

            Install();
        };
    }

    [MenuItem("Tools/Install Crash Test Tree")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Exit Play Mode before installing CrashTestTree_01.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TargetScenePath)
        {
            scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        }

        GameObject vehicle = FindReferenceVehicle(scene);
        if (vehicle == null)
        {
            Debug.LogError("No reference vehicle was found. Expected a VehicleManager list or a GameObject with SimpleCarController and Rigidbody.");
            return;
        }

        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefabPath);
        if (treePrefab == null)
        {
            Debug.LogError($"Tree prefab not found: {TreePrefabPath}");
            return;
        }

        GameObject parent = GetOrCreateParent(scene);
        RemoveExistingTestTrees(parent);

        GameObject firstTree = null;
        for (int i = 0; i < TreeCount; i++)
        {
            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, scene);
            tree.name = $"{TreeNamePrefix}{i + 1:00}";
            tree.transform.SetParent(parent.transform, true);
            tree.transform.localScale = Vector3.one * 1.15f;

            Vector3 treePosition = FindTreePosition(vehicle.transform, parent.transform, i);
            tree.transform.position = treePosition;
            tree.transform.rotation = Quaternion.identity;

            ClearStaticFlags(tree);
            ConfigureTreePhysics(tree);
            firstTree ??= tree;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = firstTree;

        Debug.Log($"Installed {TreeCount} crash test trees from {TreePrefabPath}. Reference vehicle: {vehicle.name}.");
    }

    private static bool HasInstalledTree(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != ParentName)
            {
                continue;
            }

            foreach (Transform child in root.transform)
            {
                if (child.name.StartsWith(TreeNamePrefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static GameObject FindReferenceVehicle(Scene scene)
    {
        GameObject preferredVehicle = FindVehicleByName(scene, PreferredReferenceVehicleName);
        if (preferredVehicle != null)
        {
            return preferredVehicle;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (VehicleManager manager in root.GetComponentsInChildren<VehicleManager>(true))
            {
                if (manager.vehicles != null && manager.vehicles.Length > 0 && manager.vehicles[0] != null)
                {
                    return manager.vehicles[0].gameObject;
                }
            }
        }

        List<SimpleCarController> controllers = new List<SimpleCarController>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            controllers.AddRange(root.GetComponentsInChildren<SimpleCarController>(true));
        }

        controllers.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.name, b.name));
        foreach (SimpleCarController controller in controllers)
        {
            if (controller.GetComponent<Rigidbody>() != null)
            {
                return controller.gameObject;
            }
        }

        return null;
    }

    private static GameObject FindVehicleByName(Scene scene, string vehicleName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (SimpleCarController controller in root.GetComponentsInChildren<SimpleCarController>(true))
            {
                if (controller.name == vehicleName && controller.GetComponent<Rigidbody>() != null)
                {
                    return controller.gameObject;
                }
            }

            foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
            {
                if (body.name == vehicleName)
                {
                    return body.gameObject;
                }
            }
        }

        return null;
    }

    private static GameObject GetOrCreateParent(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == ParentName)
            {
                return root;
            }
        }

        GameObject parent = new GameObject(ParentName);
        SceneManager.MoveGameObjectToScene(parent, scene);
        return parent;
    }

    private static void RemoveExistingTestTrees(GameObject parent)
    {
        List<GameObject> existingTrees = new List<GameObject>();
        foreach (Transform child in parent.transform)
        {
            if (child.name.StartsWith(TreeNamePrefix, StringComparison.Ordinal))
            {
                existingTrees.Add(child.gameObject);
            }
        }

        foreach (GameObject existingTree in existingTrees)
        {
            UnityEngine.Object.DestroyImmediate(existingTree);
        }
    }

    private static Vector3 FindTreePosition(Transform vehicle, Transform generatedParent, int treeIndex)
    {
        float baseDistance = FirstTreeDistance + TreeSpacing * treeIndex;
        float[] distances = { baseDistance, baseDistance + 5f, baseDistance - 5f, baseDistance + 10f };
        foreach (float distance in distances)
        {
            Vector3 candidate = vehicle.position + vehicle.forward * distance;
            if (TryProjectToGround(candidate, vehicle, generatedParent, out Vector3 groundedPosition))
            {
                groundedPosition.y += TreeGroundLift;
                return groundedPosition;
            }
        }

        Vector3 fallback = vehicle.position + vehicle.forward * baseDistance;
        fallback.y = vehicle.position.y + TreeGroundLift;
        return fallback;
    }

    private static bool TryProjectToGround(Vector3 candidate, Transform vehicle, Transform generatedParent, out Vector3 groundedPosition)
    {
        Ray ray = new Ray(candidate + Vector3.up * 120f, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, 260f, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            Transform hitTransform = hit.collider.transform;
            if (hitTransform.IsChildOf(vehicle) || hitTransform.IsChildOf(generatedParent))
            {
                continue;
            }

            groundedPosition = hit.point;
            return true;
        }

        groundedPosition = Vector3.zero;
        return false;
    }

    private static void ClearStaticFlags(GameObject tree)
    {
        foreach (Transform child in tree.GetComponentsInChildren<Transform>(true))
        {
            GameObjectUtility.SetStaticEditorFlags(child.gameObject, 0);
        }
    }

    private static void ConfigureTreePhysics(GameObject tree)
    {
        foreach (Collider collider in tree.GetComponentsInChildren<Collider>(true))
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }

        Rigidbody rb = tree.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = tree.AddComponent<Rigidbody>();
        }

        rb.mass = 120f;
        rb.useGravity = true;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.None;
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = 1.5f;
        rb.angularDamping = 6f;
#else
        rb.drag = 1.5f;
        rb.angularDrag = 6f;
#endif

        CapsuleCollider trunkCollider = tree.AddComponent<CapsuleCollider>();
        ConfigureTrunkCollider(tree, trunkCollider);

        CrashableTree crashableTree = tree.GetComponent<CrashableTree>();
        if (crashableTree == null)
        {
            crashableTree = tree.AddComponent<CrashableTree>();
        }

        crashableTree.triggerSpeed = 5f;
        crashableTree.mass = 120f;
        crashableTree.linearDamping = 1.5f;
        crashableTree.angularDamping = 6f;
        crashableTree.impulseScale = 22f;
        crashableTree.torqueScale = 14f;
        crashableTree.settleDelay = 1.1f;
        crashableTree.maxLinearSpeed = 8f;
        crashableTree.maxAngularSpeed = 3f;
        crashableTree.freezeYAfterImpact = true;
        crashableTree.freezeAfterSettle = true;
    }

    private static void ConfigureTrunkCollider(GameObject tree, CapsuleCollider trunkCollider)
    {
        Bounds bounds = CalculateRendererBounds(tree);
        float inverseYScale = Mathf.Approximately(tree.transform.lossyScale.y, 0f) ? 1f : 1f / tree.transform.lossyScale.y;
        float inverseXZScale = Mathf.Approximately(tree.transform.lossyScale.x, 0f) ? 1f : 1f / tree.transform.lossyScale.x;

        float localHeight = Mathf.Clamp(bounds.size.y * 0.78f * inverseYScale, 3.8f, 6.5f);
        float localRadius = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.13f * inverseXZScale, 0.3f, 0.55f);

        trunkCollider.direction = 1;
        trunkCollider.isTrigger = false;
        trunkCollider.height = localHeight;
        trunkCollider.radius = localRadius;
        trunkCollider.center = new Vector3(0f, localHeight * 0.5f, 0f);
    }

    private static Bounds CalculateRendererBounds(GameObject tree)
    {
        Renderer[] renderers = tree.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(tree.transform.position + Vector3.up * 2f, new Vector3(1f, 4f, 1f));
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }
}
