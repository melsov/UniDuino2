using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public class BallFollow : MonoBehaviour
{
    [SerializeField]
    public Transform paddle;

    Ball ball;
    Rigidbody rb;

    [SerializeField]
    private Vector3 followAxis = Vector3.right;
    private Vector3 altFollowAxis;

    private void OnEnable() {
        ball = FindObjectOfType<Ball>();
        rb = GetComponent<Rigidbody>();
        followAxis = followAxis.normalized;
        altFollowAxis = Vector3.one - followAxis;
    }

    private void FixedUpdate() {
        Vector3 pos = ball.transform.position;
        float xdif = transform.position.x - paddle.position.x;
        pos.x += xdif;
        pos.y = Mathf.Lerp(pos.y, transform.position.y, .8f);
        rb.MovePosition(new Vector3(pos.x, pos.y, rb.position.z));
    }



}
