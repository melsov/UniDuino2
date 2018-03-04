using UnityEngine;
using System.Collections;

public class FollowCamera : MonoBehaviour
{

    public float interpVelocity;
    public float minDistance;
    public float zoomWithVelocityMaxMultiplier = 1.8f;
    public float followDistance;
    public GameObject target;
    private Vector3 offset;
    Vector3 targetPos;
    Rigidbody tarb;
    float zoomMaxDist;
    [SerializeField]
    private float velMax = 40f;

    void Start() {
        tarb = target.GetComponent<Rigidbody>();
        targetPos = transform.position;
        offset = transform.position - target.transform.position;
        zoomMaxDist = offset.magnitude * zoomWithVelocityMaxMultiplier;
    }

    void FixedUpdate() {
        if (target) {
            print(tarb.velocity.magnitude);
            float mult = Mathf.Clamp(tarb.velocity.magnitude, 0, velMax) / velMax;
            Vector3 targ = target.transform.position + offset + offset.normalized * zoomWithVelocityMaxMultiplier * mult;
            transform.position = Vector3.Lerp(transform.position, targ, 0.25f);

            /*
            Vector3 posNoZ = transform.position;
            posNoZ.z = target.transform.position.z;

            Vector3 targetDirection = (target.transform.position - posNoZ);

            interpVelocity = targetDirection.magnitude * 5f;

            targetPos = transform.position + (targetDirection.normalized * interpVelocity * Time.deltaTime);

            transform.position = Vector3.Lerp(transform.position, targetPos + offset, 0.25f);
            */
        }
    }

}

