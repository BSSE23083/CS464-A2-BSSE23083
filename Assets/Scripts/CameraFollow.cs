using UnityEngine;

// Part C: frame-rate independent follow with look-ahead, in LateUpdate.
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;                          // empty: found in Awake
    [SerializeField] Vector3 offset = new Vector3(0f, 5f, -7f); // world-space offset from the aim point
    [SerializeField] float sharpness = 5f;                      // k in blend = 1 - e^(-k dt)
    [SerializeField] float lookAhead = 0.35f;                   // seconds of player velocity

    Rigidbody targetRb;
    Vector3 aim;                                                // smoothed point we look at

    void Awake()
    {
        if (target == null)
        {
            var p = FindAnyObjectByType<PlayerController>();
            if (p != null) target = p.transform;
        }
        if (target != null) targetRb = target.GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (target == null) return;
        aim = target.position;
        transform.position = aim + offset;
        transform.LookAt(aim);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Look-ahead: flat velocity x lookAhead.
        Vector3 vel = targetRb != null ? targetRb.linearVelocity : Vector3.zero;
        vel.y = 0f;
        Vector3 wantedAim = target.position + vel * lookAhead;

        // Exact smoothing: same curve at 15, 60 or 144 fps.
        float blend = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
        aim = Vector3.Lerp(aim, wantedAim, blend);

        transform.position = aim + offset;
        transform.LookAt(aim);                                  // look at the aim point, not the player
    }
}
