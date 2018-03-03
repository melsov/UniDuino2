using System;
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

    private void OnEnable() {
        rb = GetComponent<Rigidbody>();
        resetPosition();
    }

    public void resetPosition() {
        rb.MovePosition(startPos.transform.position);
    }

    private void FixedUpdate() {
        if(!worldBounds.bounds.Contains(transform.position)) {
            resetPosition();
        }
        if(rb.velocity.sqrMagnitude < Mathf.Epsilon) {
            rb.AddForce((startPos.position - rb.position) * .5f * rb.mass);
        }
    }

}
