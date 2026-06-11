using UnityEngine;
using UnityEngine.SceneManagement;

public class TestFieldGenerator : MonoBehaviour
{
    private const string RootName = "TestFieldRoot";
    private const string ManagerName = "TestFieldManager";
    private const string AllowedSceneName = "CollisionTestScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != AllowedSceneName)
        {
            return;
        }

        if (FindFirstObjectByType<TestFieldGenerator>() != null)
        {
            return;
        }

        GameObject manager = new GameObject(ManagerName);
        manager.AddComponent<TestFieldGenerator>();
    }

    public bool generateOnStart = true; // 进入 Play 后是否自动生成测试场。
    public float fieldLength = 220f; // 测试场纵向长度，确保桥梁和后段障碍仍在边界墙内。
    public float fieldWidth = 26f; // 测试场横向宽度。
    public float roadWidth = 14f; // 道路宽度。
    public float roadThickness = 0.2f; // 道路厚度。
    public float baseOffsetZ = 72f; // 整个测试场相对原点向前偏移，避免压住车辆出生点。
    public float dynamicObstacleMass = 20f; // 动态箱子/路障默认质量。
    public float dynamicLinearDamping = 0.18f; // 动态障碍的线性阻尼。
    public float dynamicAngularDamping = 0.35f; // 动态障碍的角阻尼。
    public bool addDestructibleFeedback = true; // 是否给部分动态障碍挂轻量破坏反馈。

    private Transform root;
    private Material roadMaterial;
    private Material wallMaterial;
    private Material buildingMaterial;
    private Material bridgeMaterial;
    private Material trunkMaterial;
    private Material crownMaterial;
    private Material boxMaterial;
    private Material barrierMaterial;
    private Material coneMaterial;
    private Material dynamicTreeMaterial;

    private void Start()
    {
        if (!generateOnStart)
        {
            return;
        }

        GenerateField();
    }

    [ContextMenu("Generate Test Field")]
    public void GenerateField()
    {
        CleanupOldField();
        CreateMaterials();
        CreateRoot();
        GenerateGroundAndRoad();
        GenerateBoundaryWalls();
        GenerateBuildings();
        GenerateBridge();
        GenerateTrees();
        GenerateDynamicObstacles();
    }

    private void CleanupOldField()
    {
        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(existingRoot);
        }
        else
        {
            DestroyImmediate(existingRoot);
        }
    }

    private void CreateRoot()
    {
        GameObject rootObject = new GameObject(RootName);
        root = rootObject.transform;
    }

    private void CreateMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        roadMaterial = CreateMaterial(shader, new Color(0.18f, 0.18f, 0.2f));
        wallMaterial = CreateMaterial(shader, new Color(0.72f, 0.74f, 0.78f));
        buildingMaterial = CreateMaterial(shader, new Color(0.5f, 0.58f, 0.66f));
        bridgeMaterial = CreateMaterial(shader, new Color(0.28f, 0.3f, 0.34f));
        trunkMaterial = CreateMaterial(shader, new Color(0.42f, 0.26f, 0.12f));
        crownMaterial = CreateMaterial(shader, new Color(0.22f, 0.55f, 0.2f));
        boxMaterial = CreateMaterial(shader, new Color(0.92f, 0.62f, 0.18f));
        barrierMaterial = CreateMaterial(shader, new Color(0.83f, 0.31f, 0.18f));
        coneMaterial = CreateMaterial(shader, new Color(0.96f, 0.54f, 0.12f));
        dynamicTreeMaterial = CreateMaterial(shader, new Color(0.33f, 0.65f, 0.28f));
    }

    private static Material CreateMaterial(Shader shader, Color color)
    {
        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private void GenerateGroundAndRoad()
    {
        GameObject existingGround = GameObject.Find("Ground");
        if (existingGround == null)
        {
            GameObject ground = CreatePrimitive(
                PrimitiveType.Cube,
                "Ground",
                new Vector3(0f, -0.55f, baseOffsetZ),
                new Vector3(fieldWidth * 1.6f, 1f, fieldLength * 1.2f),
                roadMaterial,
                root);

            AttachLabel(ground, "Ground", "Ground");
        }

        GameObject road = CreatePrimitive(
            PrimitiveType.Cube,
            "Road",
            new Vector3(0f, roadThickness * 0.5f, baseOffsetZ),
            new Vector3(roadWidth, roadThickness, fieldLength),
            roadMaterial,
            root);

        AttachLabel(road, "Road", "Road");
    }

    private void GenerateBoundaryWalls()
    {
        float wallHeight = 4f;
        float wallThickness = 0.8f;

        CreateStaticWall("Wall_Left", new Vector3(-(fieldWidth * 0.5f), wallHeight * 0.5f, baseOffsetZ), new Vector3(wallThickness, wallHeight, fieldLength));
        CreateStaticWall("Wall_Right", new Vector3(fieldWidth * 0.5f, wallHeight * 0.5f, baseOffsetZ), new Vector3(wallThickness, wallHeight, fieldLength));
        CreateStaticWall("Wall_Front", new Vector3(0f, wallHeight * 0.5f, baseOffsetZ + fieldLength * 0.5f), new Vector3(fieldWidth + wallThickness, wallHeight, wallThickness));
        CreateStaticWall("Wall_Back", new Vector3(0f, wallHeight * 0.5f, baseOffsetZ - fieldLength * 0.5f), new Vector3(fieldWidth + wallThickness, wallHeight, wallThickness));
    }

    private void CreateStaticWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = CreatePrimitive(PrimitiveType.Cube, name, position, scale, wallMaterial, root);
        AttachLabel(wall, name, "Wall");
    }

    private void GenerateBuildings()
    {
        CreateBuilding("Building_1", new Vector3(-8.5f, 4f, baseOffsetZ + 18f), new Vector3(7f, 8f, 7f));
        CreateBuilding("Building_2", new Vector3(8.8f, 6f, baseOffsetZ + 40f), new Vector3(6f, 12f, 8f));
        CreateBuilding("Building_3", new Vector3(-9.2f, 5f, baseOffsetZ + 74f), new Vector3(8f, 10f, 6f));
    }

    private void CreateBuilding(string name, Vector3 position, Vector3 scale)
    {
        GameObject building = CreatePrimitive(PrimitiveType.Cube, name, position, scale, buildingMaterial, root);
        AttachLabel(building, name, "Building");
    }

    private void GenerateBridge()
    {
        GameObject bridgeRoot = new GameObject("Bridge");
        bridgeRoot.transform.SetParent(root, false);

        GameObject deck = CreatePrimitive(
            PrimitiveType.Cube,
            "BridgeDeck",
            new Vector3(0f, 2.8f, baseOffsetZ + 102f),
            new Vector3(fieldWidth - 2f, 0.8f, 8f),
            bridgeMaterial,
            bridgeRoot.transform);
        AttachLabel(deck, "BridgeDeck", "Bridge");

        CreateBridgePillar("BridgePillar_1", new Vector3(-6f, 1.2f, baseOffsetZ + 98.5f), bridgeRoot.transform);
        CreateBridgePillar("BridgePillar_2", new Vector3(6f, 1.2f, baseOffsetZ + 98.5f), bridgeRoot.transform);
        CreateBridgePillar("BridgePillar_3", new Vector3(-6f, 1.2f, baseOffsetZ + 105.5f), bridgeRoot.transform);
        CreateBridgePillar("BridgePillar_4", new Vector3(6f, 1.2f, baseOffsetZ + 105.5f), bridgeRoot.transform);
    }

    private void CreateBridgePillar(string name, Vector3 position, Transform parent)
    {
        GameObject pillar = CreatePrimitive(PrimitiveType.Cube, name, position, new Vector3(1.4f, 2.4f, 1.4f), bridgeMaterial, parent);
        AttachLabel(pillar, name, "Bridge");
    }

    private void GenerateTrees()
    {
        CreateStaticTree("Tree_1", new Vector3(-10f, 0f, baseOffsetZ + 10f));
        CreateStaticTree("Tree_2", new Vector3(10f, 0f, baseOffsetZ + 28f));
        CreateStaticTree("Tree_3", new Vector3(-10.5f, 0f, baseOffsetZ + 56f));
        CreateStaticTree("Tree_4", new Vector3(10.5f, 0f, baseOffsetZ + 88f));
        CreateDynamicTree("SmallTree_Dynamic_1", new Vector3(6f, 0f, baseOffsetZ + 18f));
    }

    private void CreateStaticTree(string name, Vector3 position)
    {
        GameObject treeRoot = new GameObject(name);
        treeRoot.transform.SetParent(root, false);
        treeRoot.transform.position = position;

        GameObject trunk = CreatePrimitive(PrimitiveType.Cylinder, name + "_Trunk", position + new Vector3(0f, 1f, 0f), new Vector3(0.55f, 1f, 0.55f), trunkMaterial, treeRoot.transform);
        trunk.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        AttachLabel(trunk, name + "_Trunk", "Tree");

        GameObject crown = CreatePrimitive(PrimitiveType.Sphere, name + "_Crown", position + new Vector3(0f, 3f, 0f), new Vector3(2.4f, 2.2f, 2.4f), crownMaterial, treeRoot.transform);
        AttachLabel(crown, name + "_Crown", "Tree");
    }

    private void CreateDynamicTree(string name, Vector3 position)
    {
        GameObject treeRoot = new GameObject(name);
        treeRoot.transform.SetParent(root, false);
        treeRoot.transform.position = position;

        Rigidbody rb = treeRoot.AddComponent<Rigidbody>();
        ConfigureDynamicRigidbody(rb, 14f);

        GameObject trunk = CreatePrimitive(PrimitiveType.Cylinder, name + "_Trunk", position + new Vector3(0f, 0.9f, 0f), new Vector3(0.4f, 0.9f, 0.4f), trunkMaterial, treeRoot.transform);
        trunk.transform.localRotation = Quaternion.identity;
        AttachLabel(trunk, name + "_Trunk", "DynamicTree");

        GameObject crown = CreatePrimitive(PrimitiveType.Sphere, name + "_Crown", position + new Vector3(0f, 2.4f, 0f), new Vector3(1.8f, 1.8f, 1.8f), dynamicTreeMaterial, treeRoot.transform);
        AttachLabel(crown, name + "_Crown", "DynamicTree");
    }

    private void GenerateDynamicObstacles()
    {
        CreateDynamicBox("DynamicBox_1", new Vector3(-2.5f, 0.55f, baseOffsetZ + 26f), new Vector3(1.2f, 1.2f, 1.2f), 14f);
        CreateDynamicBox("DynamicBox_2", new Vector3(2.8f, 0.7f, baseOffsetZ + 46f), new Vector3(1.4f, 1.4f, 1.4f), 18f);
        CreateDynamicBox("DynamicBox_3", new Vector3(0f, 0.6f, baseOffsetZ + 66f), new Vector3(1.3f, 1.3f, 1.3f), 22f);

        CreateBarrier("Barrier_1", new Vector3(-3.8f, 0.45f, baseOffsetZ + 34f), 18f);
        CreateBarrier("Barrier_2", new Vector3(3.6f, 0.45f, baseOffsetZ + 58f), 22f);
        CreateBarrier("Barrier_3", new Vector3(-1.8f, 0.45f, baseOffsetZ + 82f), 26f);

        CreateCone("Cone_1", new Vector3(1.5f, 0.45f, baseOffsetZ + 22f), 4f);
        CreateCone("Cone_2", new Vector3(-1.5f, 0.45f, baseOffsetZ + 50f), 5f);
        CreateCone("Cone_3", new Vector3(1.8f, 0.45f, baseOffsetZ + 90f), 6f);
    }

    private void CreateDynamicBox(string name, Vector3 position, Vector3 scale, float mass)
    {
        GameObject box = CreatePrimitive(PrimitiveType.Cube, name, position, scale, boxMaterial, root);
        Rigidbody rb = box.AddComponent<Rigidbody>();
        ConfigureDynamicRigidbody(rb, mass);
        AttachLabel(box, name, "DynamicBox");
        if (addDestructibleFeedback)
        {
            AttachDestructible(box, 9f, false);
        }
    }

    private void CreateBarrier(string name, Vector3 position, float mass)
    {
        GameObject barrierRoot = new GameObject(name);
        barrierRoot.transform.SetParent(root, false);
        barrierRoot.transform.position = position;

        Rigidbody rb = barrierRoot.AddComponent<Rigidbody>();
        ConfigureDynamicRigidbody(rb, mass);

        GameObject beam = CreatePrimitive(PrimitiveType.Cube, name + "_Body", position + new Vector3(0f, 0.25f, 0f), new Vector3(2.4f, 0.5f, 0.5f), barrierMaterial, barrierRoot.transform);
        AttachLabel(beam, name, "Barrier");

        GameObject leftFoot = CreatePrimitive(PrimitiveType.Cube, name + "_FootLeft", position + new Vector3(-0.8f, 0.15f, 0f), new Vector3(0.45f, 0.3f, 0.7f), barrierMaterial, barrierRoot.transform);
        AttachLabel(leftFoot, name, "Barrier");

        GameObject rightFoot = CreatePrimitive(PrimitiveType.Cube, name + "_FootRight", position + new Vector3(0.8f, 0.15f, 0f), new Vector3(0.45f, 0.3f, 0.7f), barrierMaterial, barrierRoot.transform);
        AttachLabel(rightFoot, name, "Barrier");

        if (addDestructibleFeedback)
        {
            AttachDestructible(barrierRoot, 8f, false);
        }
    }

    private void CreateCone(string name, Vector3 position, float mass)
    {
        GameObject cone = CreatePrimitive(PrimitiveType.Capsule, name, position, new Vector3(0.45f, 0.9f, 0.45f), coneMaterial, root);
        Rigidbody rb = cone.AddComponent<Rigidbody>();
        ConfigureDynamicRigidbody(rb, mass);
        AttachLabel(cone, name, "Cone");
    }

    private GameObject CreatePrimitive(
        PrimitiveType type,
        string name,
        Vector3 position,
        Vector3 scale,
        Material material,
        Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.position = position;
        obj.transform.localScale = scale;

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }

        return obj;
    }

    private void ConfigureDynamicRigidbody(Rigidbody rb, float mass)
    {
        rb.mass = mass;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = dynamicLinearDamping;
        rb.angularDamping = dynamicAngularDamping;
#else
        rb.drag = dynamicLinearDamping;
        rb.angularDrag = dynamicAngularDamping;
#endif
    }

    private static void AttachLabel(GameObject target, string displayName, string category)
    {
        CollisionObjectLabel label = target.GetComponent<CollisionObjectLabel>();
        if (label == null)
        {
            label = target.AddComponent<CollisionObjectLabel>();
        }

        label.displayName = displayName;
        label.category = category;
    }

    private static void AttachDestructible(GameObject target, float threshold, bool spawnDebris)
    {
        DestructibleObstacle destructible = target.GetComponent<DestructibleObstacle>();
        if (destructible == null)
        {
            destructible = target.AddComponent<DestructibleObstacle>();
        }

        destructible.breakThreshold = threshold;
        destructible.spawnDebrisOnBreak = spawnDebris;
    }
}
