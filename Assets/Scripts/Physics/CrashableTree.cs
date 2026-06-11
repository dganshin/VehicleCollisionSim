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
    public float trunkRadius = 0.95f;
    public float trunkHeight = 7f;
    public Vector3 trunkCenter = new Vector3(0f, -2f, 0f);
    public float fallenLinearDamping = 1.2f;
    public float fallenAngularDamping = 4f;
    public float fallImpactImpulse = 4f;

    private Rigidbody rb;
    private CapsuleCollider trunkCollider;
    private bool hasFallen;
    private Vector3 basePosition;
    private Quaternion uprightRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trunkCollider = GetComponent<CapsuleCollider>();
        basePosition = transform.position;
        uprightRotation = transform.rotation;
        ConfigureCollider();
        ConfigureRigidbody();
    }

    private void OnValidate()
    {
        triggerSpeed = Mathf.Max(0.1f, triggerSpeed);
        mass = Mathf.Max(1f, mass);
        fallAngle = Mathf.Clamp(fallAngle, 20f, 88f);
        fallDuration = Mathf.Max(0.05f, fallDuration);
        impactNudgeDistance = Mathf.Max(0f, impactNudgeDistance);
        trunkRadius = Mathf.Max(0.1f, trunkRadius);
        trunkHeight = Mathf.Max(trunkRadius * 2f, trunkHeight);
        fallenLinearDamping = Mathf.Max(0f, fallenLinearDamping);
        fallenAngularDamping = Mathf.Max(0f, fallenAngularDamping);
        fallImpactImpulse = Mathf.Max(0f, fallImpactImpulse);

        if (trunkCollider == null)
        {
            trunkCollider = GetComponent<CapsuleCollider>();
        }

        ConfigureCollider();
    }

    public void ResetTreePose(Vector3 position)
    {
        StopAllCoroutines();
        hasFallen = false;
        basePosition = position;
        uprightRotation = Quaternion.identity;
        transform.SetPositionAndRotation(basePosition, uprightRotation);
        ConfigureCollider();
        ConfigureRigidbody();

        if (rb != null)
        {
            rb.position = basePosition;
            rb.rotation = uprightRotation;
            rb.Sleep();
        }
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
            ConfigureFallenRigidbody(impactDirection);
        }
    }

    private void ConfigureCollider()
    {
        if (trunkCollider == null)
        {
            return;
        }

        trunkCollider.enabled = true;
        trunkCollider.isTrigger = false;
        trunkCollider.direction = 1;
        trunkCollider.radius = trunkRadius;
        trunkCollider.height = trunkHeight;
        trunkCollider.center = trunkCenter;
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

    private void ConfigureFallenRigidbody(Vector3 impactDirection)
    {
        if (rb == null)
        {
            return;
        }

        rb.mass = mass;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.None;
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = fallenLinearDamping;
        rb.angularDamping = fallenAngularDamping;
        rb.linearVelocity = Vector3.zero;
#else
        rb.drag = fallenLinearDamping;
        rb.angularDrag = fallenAngularDamping;
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;

        if (impactDirection.sqrMagnitude > 0.01f && fallImpactImpulse > 0f)
        {
            rb.AddForce(impactDirection.normalized * fallImpactImpulse, ForceMode.Impulse);
        }

        rb.WakeUp();
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
