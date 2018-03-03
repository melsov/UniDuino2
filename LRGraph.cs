using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

public class LRGraph : MonoBehaviour
{
    LineRenderer lr;

    [SerializeField]
    private int size;

    [HideInInspector]
    public Vector2 dimensions;
    private Vector3 graphForward;
    private Vector3 graphUp;
    private Vector3 origin;

    [SerializeField]
    Color color;
    private int cursor;

    private void OnEnable() {
        lr = GetComponent<LineRenderer>();
        if(!lr) {
            lr = gameObject.AddComponent<LineRenderer>();
        }
        lr.positionCount = size;
        dimensionsWithCamera((transform.position - Camera.main.transform.position).magnitude);
        lr.material.color = color;

    }

    private void dimensionsWithCamera(float distance) {
        var frustumHeight = 2.0f * distance * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
        dimensions.y = frustumHeight / 3f;
        var frustumWidth = frustumHeight * Camera.main.aspect;
        dimensions.x = frustumWidth / 3f;
        graphForward = Camera.main.transform.right;
        graphUp = Camera.main.transform.up;
        origin = Camera.main.transform.position + new Vector3(frustumWidth / -2f + frustumWidth / 3f, dimensions.y / -2f, distance);

        print("Dims: " + dimensions.ToString());
    }

    public void setNextPosition(float data) {
        setData(cursor++, data);
    }

    public void setData(int index, float data) {
        Vector3 fward = graphForward * ((index % lr.positionCount) / (float)size) * dimensions.x;
        Vector3 up = graphUp * (dimensions.y / 2f * data);
        Vector3 pos = origin + fward + up; // graphUp * (dimensions.y / 2 + data * 0.0001f);
        //print(pos.ToString());
        lr.SetPosition(index % lr.positionCount, pos);
            //transform.position + new Vector3(((index % lr.positionCount) / (float)size) * dimensions.x, data + dimensions.y / 2f, 0f));
    }
}
