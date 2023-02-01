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
    public Dictionary<int, int> oldToNewVertices { get; set; }

    public VerticesData()
    {
        vertices = new List<Vector3>();
        colors = new List<Color>();
        normals = new List<Vector3>();
        uvs = new List<Vector2>();
        oldToNewVertices = new Dictionary<int, int>();
    }

    public VerticesData(int count)
    {
        vertices = new List<Vector3>(count);
        colors = new List<Color>(count);
        normals = new List<Vector3>(count);
        uvs = new List<Vector2>(count);
        oldToNewVertices = new Dictionary<int, int>(count);
    }

    public int this[int index]
    {
        get
        {
            return oldToNewVertices[index];
        }
    }
}
