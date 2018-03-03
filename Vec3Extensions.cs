using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class UDVec3Extensions
{
    public static Vector3 divideBy(this Vector3 v, Vector3 other) {
        return new Vector3(v.x / other.x, v.y / other.y, v.z / other.z);
    } 

    public static bool isNaN(this Vector3 v) {
        return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);
    }

    public static Vector3 mult(this Vector3 v, Vector3 other) {
        return new Vector3(v.x * other.x, v.y * other.y, v.z * other.z);
    }

    public static Vector3 componentsSquared(this Vector3 v) {
        return v.mult(v);
    }

    public static Vector3 componentsSqrt(this Vector3 v) {
        return new Vector3(Mathf.Sqrt(v.x), Mathf.Sqrt(v.y), Mathf.Sqrt(v.z));
    }
}
