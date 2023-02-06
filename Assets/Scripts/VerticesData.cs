using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerticesData
{
    public int Count { get => vertices.Count; }
    public List<Vector3> vertices { get; set; }
    public List<Color> colors { get; set; }
    public List<Vector3> normals { get; set; }
    public List<Vector2> uvs { get; set; }

    public VerticesData()
    {
        vertices = new List<Vector3>();
        colors = new List<Color>();
        normals = new List<Vector3>();
        uvs = new List<Vector2>();
    }

    public VerticesData(int count)
    {
        vertices = new List<Vector3>(count);
        colors = new List<Color>(count);
        normals = new List<Vector3>(count);
        uvs = new List<Vector2>(count);
    }

    public void Add(VerticesData newVerticesData)
    {
        vertices.AddRange(newVerticesData.vertices);
        colors.AddRange(newVerticesData.colors);
        normals.AddRange(newVerticesData.normals);
        uvs.AddRange(newVerticesData.uvs);
    }
}
