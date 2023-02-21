using System.Collections.Generic;
using UnityEngine;

public class VerticesData
{
    public int Count { get => vertices.Count; }
    public bool HasUnusedVertices { get; set; }
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

    public VerticesData(List<Vector3> initVertices, List<Color> initColors, List<Vector3> initNormals, List<Vector2> initUvs)
    {
        vertices = initVertices;
        colors = initColors;
        normals = initNormals;
        uvs = initUvs;
    }

    public void Add(VerticesData newVerticesData)
    {
        vertices.AddRange(newVerticesData.vertices);
        colors.AddRange(newVerticesData.colors);
        normals.AddRange(newVerticesData.normals);
        uvs.AddRange(newVerticesData.uvs);
    }

    public void Add(Vector3 vertix, Vector3 normal, Color color, Vector2 uv)
    {
        vertices.Add(vertix);
        normals.Add(normal);
        colors.Add(color);
        uvs.Add(uv);
    }

    public void AddFrom(VerticesData verticesData, int index)
    {
        //проверка
        vertices.Add(verticesData.vertices[index]);
        colors.Add(verticesData.colors[index]);
        normals.Add(verticesData.normals[index]);
        uvs.Add(verticesData.uvs[index]);
    }

    public void Set(VerticesData verticesData)
    {
        vertices = verticesData.vertices;
        colors = verticesData.colors;
        normals = verticesData.normals;
        uvs = verticesData.uvs;
    }

    public void CopyNormals(VerticesData anotherVerticesData)
    {
        var newNormals = new Vector3[anotherVerticesData.Count];
        anotherVerticesData.normals.CopyTo(newNormals);
        normals = new List<Vector3>(newNormals);
    }

    public VerticesData Copy()
    {
        var newVertices = new Vector3[Count];
        var newColors = new Color[Count];
        var newNormals = new Vector3[Count];
        var newUvs = new Vector2[Count];
        vertices.CopyTo(newVertices);
        colors.CopyTo(newColors);
        normals.CopyTo(newNormals);
        uvs.CopyTo(newUvs);
        return new VerticesData(new List<Vector3>(newVertices), new List<Color>(newColors), new List<Vector3>(newNormals), new List<Vector2>(newUvs));
    }

    public void RemoveAfter(int firstIndex)
    {
        vertices.RemoveRange(firstIndex, vertices.Count - firstIndex);
        colors.RemoveRange(firstIndex, colors.Count - firstIndex);
        normals.RemoveRange(firstIndex, normals.Count - firstIndex);
        uvs.RemoveRange(firstIndex, uvs.Count - firstIndex);
    }

    public void Swap(int i, int j)
    {
        Swap(vertices, i, j);
        Swap(colors, i, j);
        Swap(normals, i, j);
        Swap(uvs, i, j);
    }

    private void Swap<T>(List<T> list, int i, int j)
    {
        T tmp = list[i];
        list[i] = list[j];
        list[j] = tmp;
    }
}
