using UnityEngine;

public class CollisionObjectLabel : MonoBehaviour
{
    public string displayName; // 碰撞日志里显示的对象名称。
    public string category; // 物体类别，例如 Wall / Building / Tree / Barrier。
    public bool logVehicleCollisions = true; // 是否在车辆撞到该对象时打印日志。
    public float minimumLogRelativeSpeed = 0.5f; // 低于该速度的轻微接触不打印，避免刷屏。

    public string ResolvedName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;

    private void Reset()
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = gameObject.name;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!logVehicleCollisions || collision == null || collision.relativeVelocity.magnitude < minimumLogRelativeSpeed)
        {
            return;
        }

        SimpleCarController controller = collision.transform.GetComponentInParent<SimpleCarController>();
        if (controller == null)
        {
            return;
        }

        string objectCategory = string.IsNullOrWhiteSpace(category) ? "Environment" : category;
        Debug.Log($"[CollisionObject] {controller.name} hit {ResolvedName} ({objectCategory}) at {collision.relativeVelocity.magnitude:F1} m/s", this);
    }
}
