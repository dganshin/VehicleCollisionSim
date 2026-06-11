using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class CrashableTree : MonoBehaviour
{
    public float triggerSpeed = 5f;
    public float mass = 120f;
    public float fallAngle = 78f;
    public float fallDuration = 0.8f;
    public float impactNudgeDistance = 0.35f;

    private Rigidbody rb;
    private bool hasFallen;
    private Vector3 basePosition;
    private Quaternion uprightRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        basePosition = transform.position;
        uprightRotation = transform.rotation;
        ConfigureRigidbody();
    }

    private void OnValidate()
    {
        triggerSpeed = Mathf.Max(0.1f, triggerSpeed);
        mass = Mathf.Max(1f, mass);
        fallAngle = Mathf.Clamp(fallAngle, 20f, 88f);
        fallDuration = Mathf.Max(0.05f, fallDuration);
        impactNudgeDistance = Mathf.Max(0f, impactNudgeDistance);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasFallen || collision.relativeVelocity.magnitude < triggerSpeed || !IsVehicleCollision(collision.collider))
        {
            return;
        }

        basePosition = transform.position;
        uprightRotation = transform.rotation;
        hasFallen = true;
        StartCoroutine(FallOver(collision));
    }

    private IEnumerator FallOver(Collision collision)
    {
        ConfigureRigidbody();

        Vector3 impactDirection = collision.relativeVelocity;
        impactDirection.y = 0f;
        if (impactDirection.sqrMagnitude < 0.01f)
        {
            impactDirection = transform.position - collision.transform.position;
            impactDirection.y = 0f;
        }

        impactDirection = impactDirection.sqrMagnitude > 0.01f ? impactDirection.normalized : transform.forward;
        Vector3 fallAxis = Vector3.Cross(Vector3.up, impactDirection).normalized;
        if (fallAxis.sqrMagnitude < 0.01f)
        {
            fallAxis = transform.right;
        }

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.AngleAxis(fallAngle, fallAxis) * uprightRotation;
        Vector3 targetPosition = basePosition + impactDirection * impactNudgeDistance;

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fallDuration));
            transform.SetPositionAndRotation(Vector3.Lerp(basePosition, targetPosition, t), Quaternion.Slerp(startRotation, targetRotation, t));
            yield return null;
        }

        transform.SetPositionAndRotation(targetPosition, targetRotation);
        if (rb != null)
        {
            rb.position = targetPosition;
            rb.rotation = targetRotation;
            rb.Sleep();
        }
    }

    private void ConfigureRigidbody()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        rb.mass = mass;
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.constraints = RigidbodyConstraints.None;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;
    }

    private static bool IsVehicleCollision(Collider other)
    {
        Transform current = other.transform;
        while (current != null)
        {
            if (current.tag == "Car" || current.tag == "Vehicle")
            {
                return true;
            }

            if (current.GetComponent<SimpleCarController>() != null)
            {
                return true;
            }

            Rigidbody body = current.GetComponent<Rigidbody>();
            if (body != null)
            {
                string lowerName = current.name.ToLowerInvariant();
                if (lowerName.Contains("car") || lowerName.Contains("vehicle"))
                {
                    return true;
                }
            }

            current = current.parent;
        }

        return false;
    }
}
