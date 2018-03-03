using UnityEngine;
using System.Collections;
using System.IO.Ports;
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

public class AccelerometerClient : MonoBehaviour {
    protected Rigidbody rb;

    [SerializeField]
    private bool[] wantAxesXYZ = new bool[] { true, false, false };

    [SerializeField]
    private bool useEuler;
    [SerializeField]
    private bool useTranMatrix;

	protected virtual void Start () {
        rb = GetComponent<Rigidbody>();
        FindObjectOfType<SerialConnection>().subscribe(updateRotation);
    }

    protected virtual void FixedUpdate() {
    }
 
    private void updateRotation(string s) {
        Vector3 pitchYawRoll = parseXYZ(s);
        handleSensor(pitchYawRoll);
    }

    protected virtual void handleSensor(Vector3 pitchYawRoll) {
        print(pitchYawRoll.ToString());
        if(useTranMatrix) {
            Matrix4x4 mat = getMatrix(pitchYawRoll);
            rb.MoveRotation(mat.rotation);
        }
        else if (useEuler) {
            Quaternion eul = Quaternion.Euler(pitchYawRoll);
            rb.MoveRotation(eul);
        }
        else {
            Quaternion yaw = Quaternion.AngleAxis(pitchYawRoll.x, Vector3.right);
            Quaternion pitch = Quaternion.AngleAxis(pitchYawRoll.y, Vector3.up);
            Quaternion roll = Quaternion.AngleAxis(pitchYawRoll.z, Vector3.forward);
            rb.MoveRotation(pitch * yaw * roll);
        }

    }

    private Matrix4x4 getMatrix(Vector3 pyr) {
        float c1 = Mathf.Cos(Mathf.Deg2Rad * pyr.z);
        float s1 = Mathf.Sin(Mathf.Deg2Rad * pyr.z);
        float c2 = Mathf.Cos(Mathf.Deg2Rad * pyr.x);
        float s2 = Mathf.Sin(Mathf.Deg2Rad * pyr.x);
        float c3 = Mathf.Cos(Mathf.Deg2Rad * pyr.y);
        float s3 = Mathf.Sin(Mathf.Deg2Rad * pyr.y);

        Vector4 col1 = new Vector4(c2 * c3, -s1, c2 * s3, 0);
        Vector4 col2 = new Vector4(s1 * s3 + c1 * c3 * s2, c1 * c2, c1 * s2 * s3 - c3 * s1, 0);
        Vector4 col3 = new Vector4(c3 * s1 * s2 - c1 * s3, c2 * s1, c1 * c3 + s1 * s2 * s3, 0);
        Vector4 col4 = new Vector4(0, 0, 0, 1);
        return new Matrix4x4(
            col1, col2, col3, col4
            );
    }

    //x->heading, y->pitch, z->roll
    protected Vector3 parseXYZ(string s) {
        Vector3 eul = Vector3.zero;
        string[] compos = s.Split(' ');
        if (!compos[0].StartsWith("Orientation")) {
            print("doesn't start with orientation");
            return eul;
        }
        //if(compos.Length != 4) {
        //    print(s);
        //    return Vector3.zero;
        //}
        //Assert.IsTrue(compos.Length == 4, "expecting four elements in serial string for euler vector");
        for (int i = 1; i < 4; ++i) {
            if (wantAxesXYZ[i - 1]) {
                eul[i - 1] = float.Parse(compos[i]);
            }
        }
        return eul;
    }

 


}
