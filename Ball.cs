using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Ball : MonoBehaviour
{

    Rigidbody rb;
    bool startedGame;

    [SerializeField]
    Collider worldBounds;
    [SerializeField]
    Transform startPos;
    [SerializeField]
    float slowVelocitySqrd = 20f;
    bool startedReturn;
    [SerializeField]
    float speedUp = .3f;

    private void OnEnable() {
        rb = GetComponent<Rigidbody>();
        resetPosition();
    }

    public void resetPosition() {
        rb.MovePosition(startPos.transform.position);
        rb.velocity = Vector3.zero;
        StartCoroutine(timeout());
    }

    private IEnumerator timeout() {
        startedReturn = true;
        rb.isKinematic = true;
        yield return new WaitForSeconds(1f);
        rb.isKinematic = false;
        startedReturn = false;
    }

    

    private void FixedUpdate() {
        print(rb.velocity.sqrMagnitude);
        if(!worldBounds.bounds.Contains(transform.position)) {
            resetPosition();
        } else if (!startedReturn && rb.velocity.sqrMagnitude < slowVelocitySqrd) {
            rb.AddForce(rb.velocity * speedUp / rb.velocity.sqrMagnitude);
        }

    }
}
