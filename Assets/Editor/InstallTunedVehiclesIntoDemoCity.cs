using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class InstallTunedVehiclesIntoDemoCity
{
    private const string DemoCityRootPath = "Assets/Versatile Studio Assets";
    private const string SourceScenePath = "Assets/Scenes/CollisionTestScene.unity";
    private const string TargetScenePath = "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Scenes/demo_city_night.unity";
    private const string ImportedRootName = "ImportedTunedVehicles";
    private const string RuntimeCameraName = "VehicleRuntimeCameraAndManager";

    [MenuItem("Tools/Fix Demo City URP Materials")]
    public static void FixDemoCityUrpMaterials()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");

        if (litShader == null || unlitShader == null)
        {
            Debug.LogError("URP shaders were not found. Make sure Universal Render Pipeline is installed and active.");
            return;
        }

        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { DemoCityRootPath });
        int convertedCount = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == null || material.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal))
            {
                continue;
            }

            ConvertMaterialToUrp(material);
            convertedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Converted {convertedCount} Demo City materials to URP-compatible shaders.");
    }

    [MenuItem("Tools/Install Tuned Vehicles Into Demo City")]
    public static void Install()
    {
        if (!File.Exists(SourceScenePath))
        {
            Debug.LogError($"Source scene not found: {SourceScenePath}");
            return;
        }

        if (!File.Exists(TargetScenePath))
        {
            Debug.LogError($"Target scene not found: {TargetScenePath}");
            return;
        }

        Scene targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        RemovePreviousImportRoot(targetScene);

        GameObject importRoot = new GameObject(ImportedRootName);
        SceneManager.MoveGameObjectToScene(importRoot, targetScene);

        Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
        try
        {
            List<GameObject> sourceVehicles = FindSourceVehicles(sourceScene);
            if (sourceVehicles.Count == 0)
            {
                Debug.LogError($"No tuned vehicles were found in {SourceScenePath}.");
                return;
            }

            AddMissingStaticColliders(targetScene);
            List<GameObject> copiedVehicles = CopyVehiclesToTarget(sourceVehicles, importRoot, targetScene);
            GameObject runtimeCamera = CopyRuntimeCameraAndManager(sourceScene, importRoot, targetScene);
            WireRuntimeReferences(runtimeCamera, copiedVehicles);

            EditorSceneManager.MarkSceneDirty(targetScene);
            EditorSceneManager.SaveScene(targetScene);
            Selection.activeObject = importRoot;

            Debug.Log($"Installed {copiedVehicles.Count} tuned vehicles into {TargetScenePath}. Vehicles are under {ImportedRootName}.");
        }
        finally
        {
            if (sourceScene.IsValid() && sourceScene.isLoaded)
            {
                EditorSceneManager.CloseScene(sourceScene, true);
            }
        }
    }

    [MenuItem("Tools/Capture Current Demo City Vehicle Starts")]
    public static void CaptureCurrentDemoCityVehicleStarts()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Exit Play Mode before capturing vehicle start positions. Play Mode changes are not reliable scene defaults.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != TargetScenePath)
        {
            Debug.LogError($"Open the target scene first: {TargetScenePath}");
            return;
        }

        GameObject importRoot = GameObject.Find(ImportedRootName);
        if (importRoot == null)
        {
            Debug.LogError($"{ImportedRootName} was not found in the current scene.");
            return;
        }

        List<GameObject> vehicles = FindImportedVehicles(importRoot);
        if (vehicles.Count == 0)
        {
            Debug.LogError($"No vehicles were found under {ImportedRootName}.");
            return;
        }

        DemoCityVehicleStartDatabase database = GetOrCreateStartDatabase();
        database.starts.Clear();

        foreach (GameObject vehicle in vehicles)
        {
            database.starts.Add(new DemoCityVehicleStart
            {
                vehicleName = vehicle.name,
                position = vehicle.transform.position,
                rotation = vehicle.transform.rotation
            });
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log($"Captured and saved {database.starts.Count} vehicle start transforms from the current Demo City scene.");
    }

    [MenuItem("Tools/Delete Selected Demo City Wall Or Long Strip")]
    public static void DeleteSelectedDemoCityWallOrLongStrip()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Exit Play Mode before deleting scene objects.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != TargetScenePath)
        {
            Debug.LogError($"Open the target scene first: {TargetScenePath}");
            return;
        }

        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            Debug.LogError("Select the wall/long strip object in the Hierarchy first, then run this menu item.");
            return;
        }

        int deletedCount = 0;
        foreach (GameObject selectedObject in selectedObjects)
        {
            if (selectedObject == null || selectedObject.scene != activeScene)
            {
                continue;
            }

            Undo.DestroyObjectImmediate(selectedObject);
            deletedCount++;
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log($"Deleted {deletedCount} selected object(s) from Demo City scene.");
    }

    [MenuItem("Tools/Place Demo City Vehicles On Open Road")]
    public static void PlaceDemoCityVehiclesOnOpenRoad()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Exit Play Mode before moving vehicle start positions.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != TargetScenePath)
        {
            Debug.LogError($"Open the target scene first: {TargetScenePath}");
            return;
        }

        GameObject importRoot = GameObject.Find(ImportedRootName);
        if (importRoot == null)
        {
            Debug.LogError($"{ImportedRootName} was not found in the current scene.");
            return;
        }

        List<GameObject> vehicles = FindImportedVehicles(importRoot);
        if (vehicles.Count == 0)
        {
            Debug.LogError($"No vehicles were found under {ImportedRootName}.");
            return;
        }

        ApplyOpenRoadStarts(vehicles);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log($"Moved {vehicles.Count} Demo City vehicle(s) to the open road start area.");
    }

    [MenuItem("Tools/Add Demo City Static Colliders")]
    public static void AddDemoCityStaticColliders()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Exit Play Mode before adding scene colliders.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != TargetScenePath)
        {
            Debug.LogError($"Open the target scene first: {TargetScenePath}");
            return;
        }

        int addedCount = 0;
        int skippedCount = 0;
        AddMissingStaticColliders(activeScene, ref addedCount, ref skippedCount);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log($"Added {addedCount} static MeshCollider component(s) to Demo City. Skipped {skippedCount} object(s).");
    }

    private static int AddMissingStaticColliders(Scene scene)
    {
        int addedCount = 0;
        int skippedCount = 0;
        AddMissingStaticColliders(scene, ref addedCount, ref skippedCount);
        return addedCount;
    }

    private static void AddMissingStaticColliders(Scene scene, ref int addedCount, ref int skippedCount)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MeshFilter meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                GameObject meshObject = meshFilter.gameObject;
                if (ShouldSkipColliderInstall(meshObject) || meshFilter.sharedMesh == null)
                {
                    skippedCount++;
                    continue;
                }

                if (meshObject.GetComponent<Collider>() != null)
                {
                    skippedCount++;
                    continue;
                }

                MeshCollider meshCollider = Undo.AddComponent<MeshCollider>(meshObject);
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;
                addedCount++;
            }
        }
    }

    private static void RemovePreviousImportRoot(Scene targetScene)
    {
        foreach (GameObject root in targetScene.GetRootGameObjects())
        {
            if (root.name == ImportedRootName)
            {
                UnityEngine.Object.DestroyImmediate(root);
                return;
            }
        }
    }

    private static List<GameObject> FindSourceVehicles(Scene sourceScene)
    {
        List<GameObject> result = new List<GameObject>();
        HashSet<GameObject> seen = new HashSet<GameObject>();

        foreach (GameObject root in sourceScene.GetRootGameObjects())
        {
            foreach (SimpleCarController controller in root.GetComponentsInChildren<SimpleCarController>(true))
            {
                AddVehicle(controller.gameObject, result, seen);
            }

            foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
            {
                if (LooksLikeVehicle(body.gameObject))
                {
                    AddVehicle(body.gameObject, result, seen);
                }
            }
        }

        result.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.name, b.name));
        return result;
    }

    private static void AddVehicle(GameObject vehicle, List<GameObject> result, HashSet<GameObject> seen)
    {
        if (vehicle == null || seen.Contains(vehicle))
        {
            return;
        }

        seen.Add(vehicle);
        result.Add(vehicle);
    }

    private static bool LooksLikeVehicle(GameObject candidate)
    {
        string lowerName = candidate.name.ToLowerInvariant();
        if (lowerName.Contains("car") || lowerName.Contains("vehicle") || lowerName.Contains("bee"))
        {
            return true;
        }

        return candidate.GetComponentInChildren<WheelCollider>(true) != null;
    }

    private static List<GameObject> CopyVehiclesToTarget(List<GameObject> sourceVehicles, GameObject parent, Scene targetScene)
    {
        List<GameObject> copiedVehicles = new List<GameObject>();
        DemoCityVehicleStartDatabase startDatabase = LoadStartDatabase();
        Vector3[] spawnPositions = GetOpenRoadSpawnPositions();
        Quaternion spawnRotation = GetOpenRoadSpawnRotation();

        for (int i = 0; i < sourceVehicles.Count && i < spawnPositions.Length; i++)
        {
            GameObject copy = UnityEngine.Object.Instantiate(sourceVehicles[i]);
            copy.name = sourceVehicles[i].name;
            SceneManager.MoveGameObjectToScene(copy, targetScene);
            copy.transform.SetParent(parent.transform, true);

            // Only placement changes here; all vehicle components keep their tuned serialized values.
            if (TryApplySavedStart(startDatabase, copy))
            {
                copiedVehicles.Add(copy);
                continue;
            }

            copy.transform.position = spawnPositions[i];
            copy.transform.rotation = spawnRotation;

            copiedVehicles.Add(copy);
        }

        return copiedVehicles;
    }

    private static GameObject CopyRuntimeCameraAndManager(Scene sourceScene, GameObject parent, Scene targetScene)
    {
        GameObject sourceRuntimeRoot = null;
        foreach (GameObject root in sourceScene.GetRootGameObjects())
        {
            if (root.GetComponent<VehicleManager>() != null || root.GetComponentInChildren<VehicleManager>(true) != null)
            {
                sourceRuntimeRoot = root;
                break;
            }
        }

        if (sourceRuntimeRoot == null)
        {
            Debug.LogWarning($"VehicleManager was not found in {SourceScenePath}. Vehicles were copied, but camera switching UI was not copied.");
            return null;
        }

        GameObject copy = UnityEngine.Object.Instantiate(sourceRuntimeRoot);
        copy.name = RuntimeCameraName;
        SceneManager.MoveGameObjectToScene(copy, targetScene);
        copy.transform.SetParent(parent.transform, true);
        return copy;
    }

    private static void WireRuntimeReferences(GameObject runtimeCamera, List<GameObject> copiedVehicles)
    {
        if (runtimeCamera == null)
        {
            return;
        }

        SimpleCarController[] controllers = CollectControllers(copiedVehicles);
        CameraFollow cameraFollow = runtimeCamera.GetComponentInChildren<CameraFollow>(true);
        VehicleManager vehicleManager = runtimeCamera.GetComponentInChildren<VehicleManager>(true);
        VehicleRuntimeUI runtimeUi = runtimeCamera.GetComponentInChildren<VehicleRuntimeUI>(true);

        if (cameraFollow != null && copiedVehicles.Count > 0)
        {
            cameraFollow.target = copiedVehicles[0].transform;
        }

        if (vehicleManager != null)
        {
            vehicleManager.vehicles = controllers;
            vehicleManager.currentVehicleIndex = 0;
            vehicleManager.cameraFollow = cameraFollow;
            vehicleManager.autoDiscoverVehicles = true;
        }

        if (runtimeUi != null)
        {
            runtimeUi.vehicleManager = vehicleManager;
        }
    }

    private static SimpleCarController[] CollectControllers(List<GameObject> copiedVehicles)
    {
        List<SimpleCarController> controllers = new List<SimpleCarController>();
        foreach (GameObject vehicle in copiedVehicles)
        {
            SimpleCarController controller = vehicle.GetComponent<SimpleCarController>();
            if (controller != null)
            {
                controllers.Add(controller);
            }
        }

        return controllers.ToArray();
    }

    private static List<GameObject> FindImportedVehicles(GameObject importRoot)
    {
        List<GameObject> vehicles = new List<GameObject>();
        foreach (Rigidbody body in importRoot.GetComponentsInChildren<Rigidbody>(true))
        {
            if (LooksLikeVehicle(body.gameObject))
            {
                vehicles.Add(body.gameObject);
            }
        }

        vehicles.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.name, b.name));
        return vehicles;
    }

    private static bool ShouldSkipColliderInstall(GameObject meshObject)
    {
        if (meshObject.GetComponentInParent<Rigidbody>() != null)
        {
            return true;
        }

        if (meshObject.GetComponentInParent<SimpleCarController>() != null)
        {
            return true;
        }

        Transform current = meshObject.transform;
        while (current != null)
        {
            if (current.name == ImportedRootName || current.name == RuntimeCameraName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static DemoCityVehicleStartDatabase LoadStartDatabase()
    {
        return AssetDatabase.LoadAssetAtPath<DemoCityVehicleStartDatabase>(DemoCityVehicleStartDatabase.AssetPath);
    }

    private static DemoCityVehicleStartDatabase GetOrCreateStartDatabase()
    {
        DemoCityVehicleStartDatabase database = LoadStartDatabase();
        if (database != null)
        {
            return database;
        }

        database = ScriptableObject.CreateInstance<DemoCityVehicleStartDatabase>();
        AssetDatabase.CreateAsset(database, DemoCityVehicleStartDatabase.AssetPath);
        return database;
    }

    private static bool TryApplySavedStart(DemoCityVehicleStartDatabase database, GameObject vehicle)
    {
        if (database == null)
        {
            return false;
        }

        foreach (DemoCityVehicleStart start in database.starts)
        {
            if (!string.Equals(start.vehicleName, vehicle.name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            vehicle.transform.position = start.position;
            vehicle.transform.rotation = start.rotation;
            return true;
        }

        return false;
    }

    private static void ApplyOpenRoadStarts(List<GameObject> vehicles)
    {
        Vector3[] spawnPositions = GetOpenRoadSpawnPositions();
        Quaternion spawnRotation = GetOpenRoadSpawnRotation();
        int count = Mathf.Min(vehicles.Count, spawnPositions.Length);

        for (int i = 0; i < count; i++)
        {
            Undo.RecordObject(vehicles[i].transform, "Move Demo City Vehicle Start");
            vehicles[i].transform.position = spawnPositions[i];
            vehicles[i].transform.rotation = spawnRotation;

            Rigidbody body = vehicles[i].GetComponent<Rigidbody>();
            if (body != null)
            {
#if UNITY_6000_0_OR_NEWER
                body.linearVelocity = Vector3.zero;
#else
                body.velocity = Vector3.zero;
#endif
                body.angularVelocity = Vector3.zero;
            }
        }
    }

    private static Vector3[] GetOpenRoadSpawnPositions()
    {
        return new[]
        {
            new Vector3(96f, 1.4f, 18f),
            new Vector3(108f, 1.4f, 18f),
            new Vector3(120f, 1.4f, 18f),
            new Vector3(132f, 1.4f, 18f),
            new Vector3(144f, 1.4f, 18f),
        };
    }

    private static Quaternion GetOpenRoadSpawnRotation()
    {
        return Quaternion.Euler(0f, 90f, 0f);
    }

    private static void ConvertMaterialToUrp(Material material)
    {
        Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
        Texture mainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
        Vector2 textureScale = material.HasProperty("_MainTex") ? material.GetTextureScale("_MainTex") : Vector2.one;
        Vector2 textureOffset = material.HasProperty("_MainTex") ? material.GetTextureOffset("_MainTex") : Vector2.zero;

        string lowerName = material.name.ToLowerInvariant();
        bool shouldUseUnlit = lowerName.Contains("light") || lowerName.Contains("window");
        Shader targetShader = Shader.Find(shouldUseUnlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");

        material.shader = targetShader;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (mainTexture != null && material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", mainTexture);
            material.SetTextureScale("_BaseMap", textureScale);
            material.SetTextureOffset("_BaseMap", textureOffset);
        }

        if (!shouldUseUnlit && material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.25f);
        }

        if (shouldUseUnlit && lowerName.Contains("light") && material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color * 1.35f);
        }

        EditorUtility.SetDirty(material);
    }
}

[Serializable]
public sealed class DemoCityVehicleStart
{
    public string vehicleName;
    public Vector3 position;
    public Quaternion rotation;
}

public sealed class DemoCityVehicleStartDatabase : ScriptableObject
{
    public const string AssetPath = "Assets/Editor/DemoCityVehicleStarts.asset";
    public List<DemoCityVehicleStart> starts = new List<DemoCityVehicleStart>();
}
