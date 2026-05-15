using UnityEngine;

public class DestructibleObstacle : MonoBehaviour
{
    public float breakThreshold = 9f; // 超过该相对速度后触发轻量破坏反馈。
    public bool spawnDebrisOnBreak = false; // 是否生成简单碎块；默认关闭以保持稳定。
    public Color brokenColor = new Color(0.75f, 0.12f, 0.12f); // 破坏后颜色。
    public float shrinkScale = 0.82f; // 破坏后缩小比例。

    private bool isBroken;

    private void OnCollisionEnter(Collision collision)
    {
        if (isBroken || collision == null)
        {
            return;
        }

        if (collision.relativeVelocity.magnitude < breakThreshold)
        {
            return;
        }

        isBroken = true;
        ApplyBrokenLook();

        if (spawnDebrisOnBreak)
        {
            SpawnDebris();
        }
    }

    private void ApplyBrokenLook()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            Material material = renderers[i].material;
            material.color = brokenColor;
        }

        transform.localScale *= shrinkScale;
    }

    private void SpawnDebris()
    {
        Vector3 center = transform.position + Vector3.up * 0.3f;
        for (int i = 0; i < 3; i++)
        {
            GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.name = gameObject.name + "_Debris_" + (i + 1);
            debris.transform.position = center + new Vector3((i - 1) * 0.25f, 0.15f, i * 0.12f);
            debris.transform.localScale = Vector3.one * 0.22f;

            Renderer renderer = debris.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = brokenColor;
            }

            Rigidbody rb = debris.AddComponent<Rigidbody>();
            rb.mass = 1f;
#if UNITY_6000_0_OR_NEWER
            rb.linearDamping = 0.12f;
            rb.angularDamping = 0.2f;
#else
            rb.drag = 0.12f;
            rb.angularDrag = 0.2f;
#endif
            rb.AddExplosionForce(70f, center, 1.5f, 0.15f, ForceMode.Impulse);
            Destroy(debris, 6f);
        }
    }
}
