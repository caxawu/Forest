using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardOrient : MonoBehaviour
{
    //static Transform tCam = null;
    public Transform camera;
    Mesh mesh;
    Vector3[] vertices;
    Vector2[] uvs;


    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = new Vector2[vertices.Length];

        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }
        mesh.uv = uvs;
    }

    void Update()
    {
        //if (!tcam)
        //{
        //    if (!camera.main)
        //    {
        //        return;
        //    }
        //    tcam = camera.main.transform;
        //}
        transform.LookAt(camera.position, Vector3.up);
    }
}
