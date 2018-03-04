using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/*
 * BNO055 sensor data via serial 
 */
public struct BNOData
{
    public Quaternion quat;

    // Expects 4 floats separated by spaces
    public static BNOData FromString(string s) {
        Quaternion q = Quaternion.identity;
        try {
            string[] compos = s.Split(' ');
            if (compos.Length != 4) {
                if (!s.StartsWith("calibrating")) {
                    Debug.LogWarning("bad quaternion string: " + s);
                }
                return new BNOData() { quat = q };
            }
            for (int i = 0; i < 4; ++i) {
                q[i] = float.Parse(compos[i]);
            }
        } catch(Exception e) {
            Debug.LogWarning(e.ToString());
            return new BNOData() { quat = Quaternion.identity };
        }

        return new BNOData() { quat = q };
    }
}

public class BNOClient : MonoBehaviour
{
    protected Rigidbody rb;

    protected virtual void Start() {
        rb = GetComponent<Rigidbody>();
        FindObjectOfType<SerialConnection>().subscribe(handleSerial);
    }
   
    protected virtual void handleSerial(string incoming) {
        handleBNOData(BNOData.FromString(incoming));
    }

    protected virtual void handleBNOData(BNOData bNOData) {
        rb.MoveRotation(isolateAxisRotation(bNOData.quat, Vector3.up)); // Vector3.right, .2f));
    }

    protected Quaternion isolateAxisRotation(Quaternion qin, Vector3 referenceAxis, float sensitivity = 1f) {
        Vector3 axis; float degrees;
        qin.ToAngleAxis(out degrees, out axis);
        float dot = Vector3.Dot(axis, referenceAxis);
        print(string.Format("{0} | dot: {1}", axis.ToString(), dot));
        return Quaternion.AngleAxis(dot * dot * Mathf.Sign(dot) * degrees * sensitivity, referenceAxis); 
    }
}
