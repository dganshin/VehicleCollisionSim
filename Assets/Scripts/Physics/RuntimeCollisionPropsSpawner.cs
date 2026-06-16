using UnityEngine;
using UnityEngine.SceneManagement;

public class RuntimeCollisionPropsSpawner : MonoBehaviour
{
    private const string SpawnerName = "RuntimeCollisionPropsSpawner";
    private const string TrashCanNamePrefix = "CrashTestTrashCan_";
    private const string ConeNamePrefix = "CrashTestCone_";
    private static readonly Vector3[] FallbackTreePositions =
    {
        new Vector3(152f, 4.0f, 18f),
        new Vector3(172f, 4.8f, 18f),
        new Vector3(192f, 5.6f, 18f),
    };

    public float trashCanMass = 14f;
    public float spawnSideOffset = 2.2f;
    public float spawnForwardOffset = 1.4f;
    public float canHeight = 1.25f;
    public float canRadius = 0.42f;
    public int coneCount = 5;
    public float coneMass = 2.5f;
    public float coneHeight = 0.85f;
    public float coneRadius = 0.32f;
    public float coneSpacing = 1.25f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.name.Contains("CollisionTestScene"))
        {
            return;
        }

        if (GameObject.Find(SpawnerName) != null)
        {
            return;
        }

        GameObject spawner = new GameObject(SpawnerName);
        spawner.AddComponent<RuntimeCollisionPropsSpawner>();
    }

    private void Start()
    {
        SpawnTrashCans();
        SpawnTrafficCones();
    }

    private void SpawnTrashCans()
    {
        Transform parent = GetOrCreateParent();

        for (int i = 0; i < 3; i++)
        {
            string propName = TrashCanNamePrefix + (i + 1).ToString("00");
            if (GameObject.Find(propName) != null)
            {
                continue;
            }

            Vector3 treePosition = ResolveTreePosition(i);
            Vector3 spawnPosition = ResolveGroundedPosition(treePosition + new Vector3(spawnSideOffset, 0f, spawnForwardOffset));
            CreateTrashCan(propName, spawnPosition, parent);
        }
    }

    private void SpawnTrafficCones()
    {
        Transform parent = GetOrCreateParent();
        int count = Mathf.Clamp(coneCount, 1, 8);
        Vector3 center = ResolveTreePosition(1) + new Vector3(0f, 0f, -2.8f);
        Vector3 rowDirection = Vector3.right;
        float startOffset = -((count - 1) * coneSpacing) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            string propName = ConeNamePrefix + (i + 1).ToString("00");
            if (GameObject.Find(propName) != null)
            {
                continue;
            }

            Vector3 rowOffset = rowDirection * (startOffset + i * coneSpacing);
            Vector3 spawnPosition = ResolveGroundedPosition(center + rowOffset, coneHeight);
            CreateTrafficCone(propName, spawnPosition, parent);
        }
    }

    private static Transform GetOrCreateParent()
    {
        GameObject parent = GameObject.Find("CollisionTestObjects");
        if (parent == null)
        {
            parent = new GameObject("CollisionTestObjects");
        }

        return parent.transform;
    }

    private static Vector3 ResolveTreePosition(int index)
    {
        GameObject tree = GameObject.Find("CrashTestTree_" + (index + 1).ToString("00"));
        if (tree != null)
        {
            return tree.transform.position;
        }

        return FallbackTreePositions[Mathf.Clamp(index, 0, FallbackTreePositions.Length - 1)];
    }

    private Vector3 ResolveGroundedPosition(Vector3 origin)
    {
        return ResolveGroundedPosition(origin, canHeight);
    }

    private Vector3 ResolveGroundedPosition(Vector3 origin, float objectHeight)
    {
        Vector3 rayStart = origin + Vector3.up * 8f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * (objectHeight * 0.5f + 0.05f);
        }

        return origin + Vector3.up * (objectHeight * 0.5f + 0.05f);
    }

    private void CreateTrashCan(string propName, Vector3 position, Transform parent)
    {
        GameObject can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        can.name = propName;
        can.transform.SetParent(parent, true);
        can.transform.position = position;
        can.transform.localScale = new Vector3(canRadius * 2f, canHeight * 0.5f, canRadius * 2f);

        Renderer renderer = can.GetComponent<Renderer>();
        if (renderer != null)
        {
            SetRendererColor(renderer, new Color(0.12f, 0.34f, 0.58f, 1f));
        }

        AddVisualBand(can.transform, "Lid", new Vector3(0f, 1.03f, 0f), new Vector3(0.98f, 0.06f, 0.98f), new Color(0.05f, 0.06f, 0.07f, 1f));
        AddVisualBand(can.transform, "MiddleBand", new Vector3(0f, 0.18f, 0f), new Vector3(1.02f, 0.035f, 1.02f), new Color(0.78f, 0.82f, 0.85f, 1f));

        Rigidbody rb = can.AddComponent<Rigidbody>();
        rb.mass = trashCanMass;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = 0.08f;
        rb.angularDamping = 0.04f;
#else
        rb.drag = 0.08f;
        rb.angularDrag = 0.04f;
#endif

        CollisionObjectLabel label = can.AddComponent<CollisionObjectLabel>();
        label.displayName = "Crash test trash can";
        label.category = "DynamicObstacle";
        label.minimumLogRelativeSpeed = 0.4f;

        DestructibleObstacle obstacle = can.AddComponent<DestructibleObstacle>();
        obstacle.breakThreshold = 11f;
        obstacle.spawnDebrisOnBreak = false;
        obstacle.brokenColor = new Color(0.70f, 0.18f, 0.14f, 1f);
        obstacle.shrinkScale = 0.92f;
    }

    private void CreateTrafficCone(string propName, Vector3 position, Transform parent)
    {
        GameObject cone = new GameObject(propName);
        cone.transform.SetParent(parent, true);
        cone.transform.position = position;

        Mesh coneMesh = CreateConeMesh(coneHeight, coneRadius, 24);
        MeshFilter meshFilter = cone.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = coneMesh;

        MeshRenderer meshRenderer = cone.AddComponent<MeshRenderer>();
        SetRendererColor(meshRenderer, new Color(1.0f, 0.36f, 0.05f, 1f));

        MeshCollider meshCollider = cone.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = coneMesh;
        meshCollider.convex = true;

        AddConeBand(cone.transform, "WhiteBand", -0.10f, 0.11f, new Color(0.95f, 0.95f, 0.90f, 1f));
        AddConeBase(cone.transform);

        Rigidbody rb = cone.AddComponent<Rigidbody>();
        rb.mass = coneMass;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = 0.04f;
        rb.angularDamping = 0.03f;
#else
        rb.drag = 0.04f;
        rb.angularDrag = 0.03f;
#endif

        CollisionObjectLabel label = cone.AddComponent<CollisionObjectLabel>();
        label.displayName = "Crash test traffic cone";
        label.category = "DynamicObstacle";
        label.minimumLogRelativeSpeed = 0.3f;
    }

    private static Mesh CreateConeMesh(float height, float radius, int segments)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 6];

        vertices[0] = new Vector3(0f, height * 0.5f, 0f);
        vertices[1] = new Vector3(0f, -height * 0.5f, 0f);

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            vertices[i + 2] = new Vector3(Mathf.Cos(angle) * radius, -height * 0.5f, Mathf.Sin(angle) * radius);
        }

        int t = 0;
        for (int i = 0; i < segments; i++)
        {
            int current = i + 2;
            int next = ((i + 1) % segments) + 2;

            triangles[t++] = 0;
            triangles[t++] = current;
            triangles[t++] = next;

            triangles[t++] = 1;
            triangles[t++] = next;
            triangles[t++] = current;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddConeBand(Transform parent, string suffix, float centerY, float bandHeight, Color color)
    {
        RuntimeCollisionPropsSpawner spawner = parent.GetComponentInParent<RuntimeCollisionPropsSpawner>();
        float sourceHeight = spawner != null ? spawner.coneHeight : 0.85f;
        float sourceRadius = spawner != null ? spawner.coneRadius : 0.32f;
        float bottomY = Mathf.Clamp(centerY - bandHeight * 0.5f, -sourceHeight * 0.5f, sourceHeight * 0.5f);
        float topY = Mathf.Clamp(centerY + bandHeight * 0.5f, -sourceHeight * 0.5f, sourceHeight * 0.5f);
        float bottomRadius = ConeRadiusAtY(sourceHeight, sourceRadius, bottomY) + 0.012f;
        float topRadius = ConeRadiusAtY(sourceHeight, sourceRadius, topY) + 0.012f;

        GameObject band = new GameObject(parent.name + "_" + suffix);
        band.name = parent.name + "_" + suffix;
        band.transform.SetParent(parent, false);
        band.transform.localPosition = Vector3.zero;

        MeshFilter meshFilter = band.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateFrustumBandMesh(bottomY, topY, bottomRadius, topRadius, 24);

        MeshRenderer renderer = band.AddComponent<MeshRenderer>();
        SetRendererColor(renderer, color);
    }

    private static void AddConeBase(Transform parent)
    {
        GameObject basePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        basePlate.name = parent.name + "_BasePlate";
        basePlate.transform.SetParent(parent, false);
        basePlate.transform.localPosition = new Vector3(0f, -0.44f, 0f);
        basePlate.transform.localScale = new Vector3(0.78f, 0.06f, 0.78f);

        Collider collider = basePlate.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = basePlate.GetComponent<Renderer>();
        if (renderer != null)
        {
            SetRendererColor(renderer, new Color(0.08f, 0.08f, 0.08f, 1f));
        }
    }

    private static void AddVisualBand(Transform parent, string suffix, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        band.name = parent.name + "_" + suffix;
        band.transform.SetParent(parent, false);
        band.transform.localPosition = localPosition;
        band.transform.localScale = localScale;

        Collider collider = band.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = band.GetComponent<Renderer>();
        if (renderer != null)
        {
            SetRendererColor(renderer, color);
        }
    }

    private static float ConeRadiusAtY(float height, float radius, float y)
    {
        return radius * Mathf.Clamp01((height * 0.5f - y) / height);
    }

    private static Mesh CreateFrustumBandMesh(float bottomY, float topY, float bottomRadius, float topRadius, int segments)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments * 2];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            vertices[i] = new Vector3(x * bottomRadius, bottomY, z * bottomRadius);
            vertices[i + segments] = new Vector3(x * topRadius, topY, z * topRadius);
        }

        int t = 0;
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            int bottomCurrent = i;
            int bottomNext = next;
            int topCurrent = i + segments;
            int topNext = next + segments;

            triangles[t++] = bottomCurrent;
            triangles[t++] = topCurrent;
            triangles[t++] = topNext;

            triangles[t++] = bottomCurrent;
            triangles[t++] = topNext;
            triangles[t++] = bottomNext;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void SetRendererColor(Renderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            renderer.material.color = color;
            return;
        }

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        material.color = color;
        renderer.material = material;
    }
}
