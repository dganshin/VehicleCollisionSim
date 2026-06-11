using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class CrashableTree : MonoBehaviour
{
    public float triggerSpeed = 5f;
    public float mass = 120f;
    public float linearDamping = 1.5f;
    public float angularDamping = 6f;
    public float impulseScale = 22f;
    public float torqueScale = 14f;
    public float settleDelay = 1.1f;
    public float maxLinearSpeed = 8f;
    public float maxAngularSpeed = 3f;
    public bool freezeYAfterImpact = true;
    public bool freezeAfterSettle = true;

    private Rigidbody rb;
    private bool hasFallen;
    private float initialY;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        initialY = transform.position.y;
        ConfigureRigidbody(true);
    }

    private void OnValidate()
    {
        triggerSpeed = Mathf.Max(0.1f, triggerSpeed);
        mass = Mathf.Max(1f, mass);
        impulseScale = Mathf.Max(0f, impulseScale);
        torqueScale = Mathf.Max(0f, torqueScale);
        maxLinearSpeed = Mathf.Max(0.5f, maxLinearSpeed);
        maxAngularSpeed = Mathf.Max(0.5f, maxAngularSpeed);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasFallen || collision.relativeVelocity.magnitude < triggerSpeed || !IsVehicleCollision(collision.collider))
        {
            return;
        }

        KnockDown(collision);
    }

    private void KnockDown(Collision collision)
    {
        hasFallen = true;
        ConfigureRigidbody(false);

        Vector3 impactDirection = collision.relativeVelocity;
        impactDirection.y = 0f;
        if (impactDirection.sqrMagnitude < 0.01f)
        {
            impactDirection = transform.position - collision.transform.position;
            impactDirection.y = 0f;
        }

        impactDirection = impactDirection.sqrMagnitude > 0.01f ? impactDirection.normalized : transform.forward;
        Vector3 hitPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position + Vector3.up;
        float speedFactor = Mathf.Clamp(collision.relativeVelocity.magnitude / triggerSpeed, 1f, 3f);

        rb.AddForceAtPosition(impactDirection * impulseScale * speedFactor, hitPoint, ForceMode.Impulse);
        rb.AddTorque(Vector3.Cross(Vector3.up, impactDirection) * torqueScale * speedFactor, ForceMode.Impulse);
        ClampVelocity();

        StartCoroutine(SettleAfterImpact());
    }

    private void LateUpdate()
    {
        if (!hasFallen || rb == null || transform.position.y >= initialY - 0.15f)
        {
            return;
        }

        Vector3 position = transform.position;
        position.y = initialY;
        transform.position = position;

#if UNITY_6000_0_OR_NEWER
        Vector3 velocity = rb.linearVelocity;
        if (velocity.y < 0f)
        {
            velocity.y = 0f;
            rb.linearVelocity = velocity;
        }
#else
        Vector3 velocity = rb.velocity;
        if (velocity.y < 0f)
        {
            velocity.y = 0f;
            rb.velocity = velocity;
        }
#endif
    }

    private IEnumerator SettleAfterImpact()
    {
        yield return new WaitForSeconds(settleDelay);

        if (rb == null)
        {
            yield break;
        }

#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = Mathf.Max(linearDamping, 1.2f);
        rb.angularDamping = Mathf.Max(angularDamping, 3.5f);
        rb.linearVelocity *= 0.35f;
#else
        rb.drag = Mathf.Max(linearDamping, 1.2f);
        rb.angularDrag = Mathf.Max(angularDamping, 3.5f);
        rb.velocity *= 0.35f;
#endif
        rb.angularVelocity *= 0.25f;
        ClampVelocity();

        if (freezeAfterSettle)
        {
            yield return new WaitForSeconds(0.5f);
            rb.isKinematic = true;
        }
    }

    private void ConfigureRigidbody(bool kinematic)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        rb.mass = mass;
        rb.useGravity = true;
        rb.isKinematic = kinematic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = !kinematic && freezeYAfterImpact ? RigidbodyConstraints.FreezePositionY : RigidbodyConstraints.None;
        rb.maxAngularVelocity = maxAngularSpeed;

#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = linearDamping;
        rb.angularDamping = angularDamping;
#else
        rb.drag = linearDamping;
        rb.angularDrag = angularDamping;
#endif
    }

    private void ClampVelocity()
    {
        if (rb == null)
        {
            return;
        }

#if UNITY_6000_0_OR_NEWER
        if (rb.linearVelocity.magnitude > maxLinearSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxLinearSpeed;
        }
#else
        if (rb.velocity.magnitude > maxLinearSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxLinearSpeed;
        }
#endif

        if (rb.angularVelocity.magnitude > maxAngularSpeed)
        {
            rb.angularVelocity = rb.angularVelocity.normalized * maxAngularSpeed;
        }
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
