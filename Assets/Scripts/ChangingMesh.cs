using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ChangingMesh : MonoBehaviour
{
    /*
        Ax + By + Cz + D = 0
    */
    [SerializeField] public MeshFilter innerFilter;
    private Color ColorForSection = Color.white;
    private Color ColorForOuter = Color.black;
    private const int NoVertixIndex = -1;
    private MeshFilter _outerFilter;
    private MeshCollider _collider;
    private Rigidbody _rigidbody;
    private VerticesData _detailedVertices = new VerticesData();
    private VerticesData _innerVertices = new VerticesData();
    private List<int> _detailedTriangles;
    private List<int> _innerTriangles;
    private List<int> _emptyList = new List<int>();
    private bool _canSlash = false;

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _outerFilter = GetComponent<MeshFilter>();
        _collider = GetComponent<MeshCollider>();
        _detailedTriangles = new List<int>();
        _innerTriangles = new List<int>();
        Mesh mesh = _outerFilter.mesh;
        InitializeVerticesData(mesh, _detailedTriangles, _detailedVertices);
        mesh = innerFilter.mesh;
        InitializeVerticesData(mesh, _innerTriangles, _innerVertices);
        mesh.subMeshCount = 2;
        _collider.sharedMesh = mesh;
        StartCoroutine(AllowSlashing());
    }

    public void SliceByPlane(float A, float B, float C, float D)
    {
        if(!_canSlash)
        {
            return;
        }
        _canSlash = false;
        GameObject secondHalf = null;
        DivideByPlane(A, B, C, D, ref _detailedTriangles, _detailedVertices, ref secondHalf);
        SliceByPlane(A, B, C, D, ref _innerTriangles, _innerVertices, ref secondHalf);
        StartCoroutine(AllowSlashing());
    }

    public void DivideByPlane(float A, float B, float C, float D, ref List<int> triangles, VerticesData verticesData, ref GameObject secondHalf)
    {
        var leftTriangles = new List<int>(triangles.Count / 2);
        var rightTriangles = new List<int>(triangles.Count / 2);

        for(int i = 0; i <= triangles.Count - 3; i += 3)
        {
            var firstVertix = verticesData.vertices[triangles[i]];
            var secondVertix = verticesData.vertices[triangles[i + 1]];
            var thirdVertix = verticesData.vertices[triangles[i + 2]];
            if(IsToLeftOfPlane(firstVertix, A, B, C, D) && IsToLeftOfPlane(secondVertix, A, B, C, D) && IsToLeftOfPlane(thirdVertix, A, B, C, D))
            {
                leftTriangles.Add(triangles[i]);
                leftTriangles.Add(triangles[i + 1]);
                leftTriangles.Add(triangles[i + 2]);
            }
            else if(IsToRightOfPlane(firstVertix, A, B, C, D) && IsToRightOfPlane(secondVertix, A, B, C, D) && IsToRightOfPlane(thirdVertix, A, B, C, D))
            {
                rightTriangles.Add(triangles[i]);
                rightTriangles.Add(triangles[i + 1]);
                rightTriangles.Add(triangles[i + 2]);
            }
        }
        
        if(secondHalf == null)
        {
            secondHalf = Instantiate(this.gameObject);
        }
        secondHalf.GetComponent<MeshFilter>().mesh.SetTriangles(rightTriangles, 0);
        secondHalf.GetComponent<ChangingMesh>()._detailedTriangles = rightTriangles;

        GetComponent<MeshFilter>().mesh.SetTriangles(leftTriangles, 0);
        _detailedTriangles = leftTriangles;
    }

    public void SliceByPlane(float A, float B, float C, float D, ref List<int> triangles, VerticesData verticesData, ref GameObject secondHalf)
    {
        var leftTriangles = new List<int>(triangles.Count / 2);
        var rightTriangles = new List<int>(triangles.Count / 2);
        var leftSection = new List<int>(triangles.Count / 2);
        var rightSection = new List<int>(triangles.Count / 2);
        var localLeftVertices = new List<int>(2);
        var localRightVertices = new List<int>(2);
        var sectionVertices = new Dictionary<int, int>(triangles.Count / 2);
        var leftVertices = new VerticesData(verticesData.Count / 2);
        var rightVertices = new VerticesData(verticesData.Count / 2);
        var mainVertixIndex = NoVertixIndex;
        float maxX = 0;
        float maxY = 0;
        float maxZ = 0;
        float minX = 0;
        float minY = 0;
        float minZ = 0;

        //Основной цикл
        for(int i = 0; i <= triangles.Count - 3; i += 3)
        {
            var firstVertix = verticesData.vertices[triangles[i]];
            var secondVertix = verticesData.vertices[triangles[i + 1]];
            var thirdVertix = verticesData.vertices[triangles[i + 2]];
            maxX = Max(firstVertix.x, secondVertix.x, thirdVertix.x, maxX);
            minX = Min(firstVertix.x, secondVertix.x, thirdVertix.x, minX);
            minY = Min(firstVertix.y, secondVertix.y, thirdVertix.y, minY);
            maxY = Max(firstVertix.y, secondVertix.y, thirdVertix.y, maxY);
            minZ = Min(firstVertix.z, secondVertix.z, thirdVertix.z, minZ);
            maxZ = Max(firstVertix.z, secondVertix.z, thirdVertix.z, maxZ);
            if(IsToLeftOfPlane(firstVertix, A, B, C, D) && IsToLeftOfPlane(secondVertix, A, B, C, D) && IsToLeftOfPlane(thirdVertix, A, B, C, D))
            {
                AddNewVertices(leftVertices, verticesData, triangles[i]);
                AddNewVertices(leftVertices, verticesData, triangles[i + 1]);
                AddNewVertices(leftVertices, verticesData, triangles[i + 2]);
                if(verticesData.colors[triangles[i]].Equals(ColorForSection))
                {
                    leftSection.Add(leftVertices[triangles[i]]);
                    leftSection.Add(leftVertices[triangles[i + 1]]);
                    leftSection.Add(leftVertices[triangles[i + 2]]);
                }
                else
                {
                    leftTriangles.Add(leftVertices[triangles[i]]);
                    leftTriangles.Add(leftVertices[triangles[i + 1]]);
                    leftTriangles.Add(leftVertices[triangles[i + 2]]);
                }
            }
            else if(IsToRightOfPlane(firstVertix, A, B, C, D) && IsToRightOfPlane(secondVertix, A, B, C, D) && IsToRightOfPlane(thirdVertix, A, B, C, D))
            {
                AddNewVertices(rightVertices, verticesData, triangles[i]);
                AddNewVertices(rightVertices, verticesData, triangles[i + 1]);
                AddNewVertices(rightVertices, verticesData, triangles[i + 2]);
                if(verticesData.colors[triangles[i]].Equals(ColorForSection))
                {
                    rightSection.Add(rightVertices[triangles[i]]);
                    rightSection.Add(rightVertices[triangles[i + 1]]);
                    rightSection.Add(rightVertices[triangles[i + 2]]);
                }
                else
                {
                    rightTriangles.Add(rightVertices[triangles[i]]);
                    rightTriangles.Add(rightVertices[triangles[i + 1]]);
                    rightTriangles.Add(rightVertices[triangles[i + 2]]);
                }
            }
            else
            {
                //Обработка разреза
                SeparateVertices(localLeftVertices, localRightVertices, triangles, i, leftVertices, rightVertices, verticesData, A, B, C, D);
                var colorForNewVertices = verticesData.colors[localLeftVertices[0]];
                if(localLeftVertices.Count > localRightVertices.Count)
                {
                    var newVertices = GetNewVertices(verticesData.vertices[localLeftVertices[0]], verticesData.vertices[localLeftVertices[1]], verticesData.vertices[localRightVertices[0]],
                                    A, B, C, D);
                    if(colorForNewVertices.Equals(ColorForSection))
                    {
                        AddFormedTriangles(newVertices.Item1, newVertices.Item2, localLeftVertices, localRightVertices, leftSection, rightSection,
                                        leftVertices, rightVertices, verticesData, colorForNewVertices);
                    }
                    else
                    {
                        AddFormedTriangles(newVertices.Item1, newVertices.Item2, localLeftVertices, localRightVertices, leftTriangles, rightTriangles,
                                        leftVertices, rightVertices, verticesData, colorForNewVertices);
                    }
                    sectionVertices.Add(verticesData.Count - 1, verticesData.Count - 2);
                }
                else
                {
                    var newVertices = GetNewVertices(verticesData.vertices[localRightVertices[0]], verticesData.vertices[localRightVertices[1]], verticesData.vertices[localLeftVertices[0]],
                                    A, B, C, D);
                    if(colorForNewVertices.Equals(ColorForSection))
                    {
                        AddFormedTriangles(newVertices.Item1, newVertices.Item2, localRightVertices, localLeftVertices, rightSection, leftSection,
                                        rightVertices, leftVertices, verticesData, colorForNewVertices);
                    }
                    else
                    {
                        AddFormedTriangles(newVertices.Item1, newVertices.Item2, localRightVertices, localLeftVertices, rightTriangles, leftTriangles,
                                        rightVertices, leftVertices, verticesData, colorForNewVertices);
                    }
                    sectionVertices.Add(verticesData.Count - 2, verticesData.Count - 1);
                }
                if(mainVertixIndex == NoVertixIndex)
                {
                    mainVertixIndex = verticesData.Count - 1;
                }
                localLeftVertices.Clear();
                localRightVertices.Clear();
            }
        }
        var commonNormalForLeftSection = new Vector3(A, B, C);
        var commonNormalForRightSection = new Vector3(-A, -B, -C);
        AddNewVertices(leftVertices, verticesData, mainVertixIndex);
        AddNewVertices(rightVertices, verticesData, mainVertixIndex);
        leftVertices.normals[leftVertices.oldToNewVertices[mainVertixIndex]] = commonNormalForLeftSection;
        rightVertices.normals[rightVertices.oldToNewVertices[mainVertixIndex]] = commonNormalForRightSection;

        //Добавление треугольников для каждого сечения
        foreach(int vertixIndex in sectionVertices.Keys)
        {
            if(mainVertixIndex == vertixIndex)
            {
                continue;
            }
            AddNewVertices(leftVertices, verticesData, vertixIndex);
            AddNewVertices(leftVertices, verticesData, sectionVertices[vertixIndex]);

            leftSection.Add(leftVertices[mainVertixIndex]);
            leftSection.Add(leftVertices[vertixIndex]);
            leftSection.Add(leftVertices[sectionVertices[vertixIndex]]);

            AddNewVertices(rightVertices, verticesData, vertixIndex);
            AddNewVertices(rightVertices, verticesData, sectionVertices[vertixIndex]);
            rightSection.Add(rightVertices[mainVertixIndex]);
            rightSection.Add(rightVertices[sectionVertices[vertixIndex]]);
            rightSection.Add(rightVertices[vertixIndex]);
        }

        //Формирование нормалей и карты текстуры для правого объекта
        foreach(int vertixIndex in rightSection)
        {
            if(!rightVertices.normals[vertixIndex].Equals(Vector3.zero))
            {
                continue;
            }
            rightVertices.normals[vertixIndex] = commonNormalForRightSection;
            rightVertices.uvs[vertixIndex] = new Vector2((rightVertices.vertices[vertixIndex].z - minZ) / (maxZ - minZ), (rightVertices.vertices[vertixIndex].y - minY) / (maxY - minY));
        }

        //Создание дочернего обрубка
        if(secondHalf == null)
        {
            secondHalf = Instantiate(this.gameObject);
        }
        var mesh = new Mesh();
        mesh.name = "Inner";
        SetVerticesData(mesh, rightVertices, verticesData, ref triangles, rightTriangles, rightSection);
        secondHalf.GetComponent<MeshCollider>().sharedMesh = mesh;
        secondHalf.GetComponent<ChangingMesh>().innerFilter.mesh = mesh;

        //Формирование нормалей и карты текстуры для левого объекта
        foreach(int vertixIndex in leftSection)
        {
            if(!leftVertices.normals[vertixIndex].Equals(Vector3.zero))
            {
                continue;
            }
            leftVertices.normals[vertixIndex] = commonNormalForLeftSection;
            leftVertices.uvs[vertixIndex] = new Vector2((leftVertices.vertices[vertixIndex].z - minZ) / (maxX - minX), (leftVertices.vertices[vertixIndex].y - minY) / (maxY - minY));
        }

        //формирование меша для родительского объекта
        mesh = new Mesh();
        SetVerticesData(mesh, leftVertices, verticesData, ref triangles, leftTriangles, leftSection);
        _collider.sharedMesh = mesh;
        _collider.convex = true;
        SetVerticesData(innerFilter.mesh, leftVertices, verticesData, ref triangles, leftTriangles, leftSection);
    }

    private void InitializeVerticesData(Mesh mesh, List<int> triangles, VerticesData verticesData)
    {
        mesh.GetVertices(verticesData.vertices);
        mesh.GetNormals(verticesData.normals);
        mesh.GetUVs(0, verticesData.uvs);
        mesh.GetTriangles(triangles, 0);
        mesh.GetColors(verticesData.colors);
        for(int i = 0; i < verticesData.Count; ++i)
        {
            if(verticesData.colors.Count == i)
            {
                verticesData.colors.Add(ColorForOuter);
            }
        }
        for(int i = 0; i < verticesData.Count; ++i)
        {
            if(verticesData.uvs.Count == i)
            {
                verticesData.uvs.Add(Vector2.zero);
            }
        }
        mesh.SetColors(verticesData.colors);
    }

    private void SetVerticesData(Mesh mesh, VerticesData newVerticesData, VerticesData oldVerticesData, ref List<int> oldTriangles, List<int> triangles, List<int> sectionTriangles)
    {
        oldVerticesData.vertices = newVerticesData.vertices;
        oldVerticesData.normals = newVerticesData.normals;
        oldVerticesData.colors = newVerticesData.colors;
        oldVerticesData.uvs = newVerticesData.uvs;

        mesh.subMeshCount = 2;
        mesh.SetTriangles(_emptyList, 0, true, 0);
        mesh.SetTriangles(_emptyList, 1, true, 0);
        mesh.SetVertices(oldVerticesData.vertices);
        mesh.SetTriangles(triangles, 0, true, 0);
        mesh.SetTriangles(sectionTriangles, 1, true, 0);
        triangles.AddRange(sectionTriangles);
        oldTriangles = triangles;
        mesh.SetNormals(oldVerticesData.normals);
        mesh.SetColors(oldVerticesData.colors);
        mesh.SetUVs(0, oldVerticesData.uvs);
        mesh.SetUVs(1, oldVerticesData.uvs);
    }

    private void SetDetailedTriangles(List<int> newTriangles)
    {
        _detailedTriangles = newTriangles;
    }

    //добавление вершин
    private void SeparateVertices(List<int> localLeftVertices, List<int> localRightVertices, List<int> oldTriangles, int firstVertixOfTriangle,
                                VerticesData newLeftVertices, VerticesData newRightVertices, VerticesData oldVerticesData,
                                float A, float B, float C, float D)//много аргументов, if-ов
    {
        bool firstIsLeft;
        bool secondIsLeft;
        var firstVertix = oldVerticesData.vertices[oldTriangles[firstVertixOfTriangle]];
        var secondVertix = oldVerticesData.vertices[oldTriangles[firstVertixOfTriangle + 1]];
        var thirdVertix = oldVerticesData.vertices[oldTriangles[firstVertixOfTriangle + 2]];
        if(IsToLeftOfPlane(firstVertix, A, B, C, D))
        {
            AddNewVertices(newLeftVertices, oldVerticesData, oldTriangles[firstVertixOfTriangle]);
            localLeftVertices.Add(oldTriangles[firstVertixOfTriangle]);
            firstIsLeft = true;
        }
        else
        {
            AddNewVertices(newRightVertices, oldVerticesData, oldTriangles[firstVertixOfTriangle]);
            localRightVertices.Add(oldTriangles[firstVertixOfTriangle]);
            firstIsLeft = false;
        }
        if(IsToLeftOfPlane(secondVertix, A, B, C, D))
        {
            AddNewVertices(newLeftVertices, oldVerticesData, oldTriangles[firstVertixOfTriangle + 1]);
            localLeftVertices.Add(oldTriangles[firstVertixOfTriangle + 1]);
            secondIsLeft = true;
        }
        else
        {
            AddNewVertices(newRightVertices, oldVerticesData, oldTriangles[firstVertixOfTriangle + 1]);
            localRightVertices.Add(oldTriangles[firstVertixOfTriangle + 1]);
            secondIsLeft = false;
        }
        if(IsToLeftOfPlane(thirdVertix, A, B, C, D))
        {
            AddNewVertices(newLeftVertices, oldVerticesData, oldTriangles[firstVertixOfTriangle + 2]);
            if(firstIsLeft ^ secondIsLeft)
            {
                if(firstIsLeft)
                {
                    localLeftVertices.Add(oldTriangles[firstVertixOfTriangle + 2]);
                }
                else
                {
                    var secondLeftVertix = localLeftVertices[0];
                    localLeftVertices[0] = oldTriangles[firstVertixOfTriangle + 2];
                    localLeftVertices.Add(secondLeftVertix);
                }
            }
            else
            {
                localLeftVertices.Add(oldTriangles[firstVertixOfTriangle + 2]);
                var secondRightVertix = localRightVertices[0];
                localRightVertices[0] = localRightVertices[1];
                localRightVertices[1] = secondRightVertix;
            }
        }
        else
        {
            AddNewVertices(newRightVertices, oldVerticesData, oldTriangles[firstVertixOfTriangle + 2]);
            if(firstIsLeft ^ secondIsLeft)
            {
                if(!firstIsLeft)
                {
                    localRightVertices.Add(oldTriangles[firstVertixOfTriangle + 2]);
                }
                else
                {
                    var secondRightVertix = localRightVertices[0];
                    localRightVertices[0] = oldTriangles[firstVertixOfTriangle + 2];
                    localRightVertices.Add(secondRightVertix);
                }
            }
            else
            {
                localRightVertices.Add(oldTriangles[firstVertixOfTriangle + 2]);
                var secondLeftVertix = localLeftVertices[0];
                localLeftVertices[0] = localLeftVertices[1];
                localLeftVertices[1] = secondLeftVertix;
            }
        }
    }

    private (Vector3, Vector3) GetNewVertices(Vector3 firstVertix, Vector3 secondVertix, Vector3 aloneVertix,
                                float A, float B, float C, float D)//много аргументов
    {
        /*
            x = mt + x0
            y = nt + y0
            z = pt + z0

            t = -(Ax0 + By0 + Cz0 + D) / (Am + Bn + Cp)
        */
        var firstNewVertix = new Vector3();
        var secondNewVertix = new Vector3();
        float m = firstVertix.x - aloneVertix.x;
        float n = firstVertix.y - aloneVertix.y;
        float p = firstVertix.z - aloneVertix.z;
        float t = -(A * firstVertix.x + B * firstVertix.y + C * firstVertix.z + D) /
         (A * m + B * n + C * p);
        firstNewVertix.x = m * t + firstVertix.x;
        firstNewVertix.y = n * t + firstVertix.y;
        firstNewVertix.z = p * t + firstVertix.z;
        m = secondVertix.x - aloneVertix.x;
        n = secondVertix.y - aloneVertix.y;
        p = secondVertix.z - aloneVertix.z;
        t = -(A * secondVertix.x + B * secondVertix.y + C * secondVertix.z + D) /
         (A * m + B * n + C * p);
        secondNewVertix.x = m * t + secondVertix.x;
        secondNewVertix.y = n * t + secondVertix.y;
        secondNewVertix.z = p * t + secondVertix.z;
        return (firstNewVertix, secondNewVertix);
    }

    private void SetNewUVs(int firstVertixIndex, int secondVertixIndex, int aloneVertixIndex,
                            int firstNewVertixIndex, int secondNewVertixIndex,
                            VerticesData twoVertices, VerticesData aloneVertix)
    {
        var firstUV = twoVertices.uvs[firstVertixIndex];
        var secondUV = twoVertices.uvs[secondVertixIndex];
        var aloneUV = aloneVertix.uvs[aloneVertixIndex];
        var magnitude = (twoVertices.vertices[firstNewVertixIndex] - twoVertices.vertices[firstVertixIndex]).magnitude /
            (aloneVertix.vertices[aloneVertixIndex] - twoVertices.vertices[firstVertixIndex]).magnitude;
        var offset = (aloneUV - firstUV) * magnitude;
        twoVertices.uvs[twoVertices.uvs.Count - 2] = firstUV + offset;
        aloneVertix.uvs[aloneVertix.uvs.Count - 2] = firstUV + offset;
        magnitude = (twoVertices.vertices[secondNewVertixIndex] - twoVertices.vertices[secondVertixIndex]).magnitude /
            (aloneVertix.vertices[aloneVertixIndex] - twoVertices.vertices[secondVertixIndex]).magnitude;
        offset = (aloneUV - firstUV) * magnitude;
        twoVertices.uvs[twoVertices.uvs.Count - 1] = secondUV + offset;
        aloneVertix.uvs[aloneVertix.uvs.Count - 1] = secondUV + offset;
    }
    //добавление вершин
    private void AddFormedTriangles(Vector3 firstVertix, Vector3 secondVertix,//много аргументов
                                    List<int> newTwoTriangles, List<int> newTriangle,
                                    List<int> listForTwoTriangles, List<int> listForOneTriangle,
                                    VerticesData newVerticesForTwoTriangles, VerticesData newVerticesForOneTriangle, VerticesData oldVerticesData,
                                    Color colorForNewVertices)
    {
        oldVerticesData.vertices.Add(firstVertix);
        oldVerticesData.vertices.Add(secondVertix);
        oldVerticesData.normals.Add(newVerticesForOneTriangle.normals[newVerticesForOneTriangle[newTriangle[0]]]);
        oldVerticesData.normals.Add(newVerticesForOneTriangle.normals[newVerticesForOneTriangle[newTriangle[0]]]);
        oldVerticesData.colors.Add(colorForNewVertices);
        oldVerticesData.colors.Add(colorForNewVertices);
        oldVerticesData.uvs.Add(Vector2.zero);
        oldVerticesData.uvs.Add(Vector2.zero);

        AddNewVertices(newVerticesForTwoTriangles, oldVerticesData, oldVerticesData.Count - 2);
        AddNewVertices(newVerticesForTwoTriangles, oldVerticesData, oldVerticesData.Count - 1);
        AddNewVertices(newVerticesForOneTriangle, oldVerticesData, oldVerticesData.Count - 2);
        AddNewVertices(newVerticesForOneTriangle, oldVerticesData, oldVerticesData.Count - 1);
        SetNewUVs(newVerticesForTwoTriangles[newTwoTriangles[0]], newVerticesForTwoTriangles[newTwoTriangles[1]], newVerticesForOneTriangle[newTriangle[0]], 
                newVerticesForTwoTriangles.Count - 2, newVerticesForTwoTriangles.Count - 1,
                newVerticesForTwoTriangles, newVerticesForOneTriangle);

        listForTwoTriangles.Add(newVerticesForTwoTriangles[newTwoTriangles[0]]);
        listForTwoTriangles.Add(newVerticesForTwoTriangles[oldVerticesData.Count - 2]);
        listForTwoTriangles.Add(newVerticesForTwoTriangles[newTwoTriangles[1]]);

        listForTwoTriangles.Add(newVerticesForTwoTriangles[newTwoTriangles[1]]);
        listForTwoTriangles.Add(newVerticesForTwoTriangles[oldVerticesData.Count - 2]);
        listForTwoTriangles.Add(newVerticesForTwoTriangles[oldVerticesData.Count - 1]);

        listForOneTriangle.Add(newVerticesForOneTriangle[oldVerticesData.Count - 2]);
        listForOneTriangle.Add(newVerticesForOneTriangle[newTriangle[0]]);
        listForOneTriangle.Add(newVerticesForOneTriangle[oldVerticesData.Count - 1]);
                    
        oldVerticesData.vertices.Add(firstVertix);
        oldVerticesData.vertices.Add(secondVertix);
        oldVerticesData.normals.Add(Vector3.zero);
        oldVerticesData.normals.Add(Vector3.zero);
        oldVerticesData.colors.Add(ColorForSection);
        oldVerticesData.colors.Add(ColorForSection);
        oldVerticesData.uvs.Add(Vector2.zero);
        oldVerticesData.uvs.Add(Vector2.zero);
    }

    private void AddNewVertices(VerticesData newVertices, VerticesData oldVertices, int vertixIndex)
    {
        if(!newVertices.oldToNewVertices.ContainsKey(vertixIndex))
        {
            newVertices.vertices.Add(oldVertices.vertices[vertixIndex]);
            newVertices.colors.Add(oldVertices.colors[vertixIndex]);
            newVertices.normals.Add(oldVertices.normals[vertixIndex]);
            newVertices.uvs.Add(oldVertices.uvs[vertixIndex]);
            newVertices.oldToNewVertices.Add(vertixIndex, newVertices.Count - 1);
        }
    }

    private bool IsToLeftOfPlane(Vector3 point, float A, float B, float C, float D)
    {
        return A * point.x + B * point.y + C * point.z + D < 0 || IsOnPlane(point, A, B, C, D);
    }

    private bool IsToRightOfPlane(Vector3 point, float A, float B, float C, float D)
    {
        return A * point.x + B * point.y + C * point.z + D > 0;
    }

    private bool IsOnPlane(Vector3 point, float A, float B, float C, float D)
    {
        return A * point.x + B * point.y + C * point.z + D == 0;
    }

    private float Min(float a, float b, float c, float d)
    {
        float min = (a < b) ? a : b;
        min = (min < c) ? min : c;
        return (min < d) ? min : d;
    }

    private float Max(float a, float b, float c, float d)
    {
        float min = (a > b) ? a : b;
        min = (min > c) ? min : c;
        return (min > d) ? min : d;
    }

    private IEnumerator AllowSlashing()
    {
        yield return new WaitForSeconds(1);

        _canSlash = true;
    }
}