using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChangingMesh : MonoBehaviour
{
    /*
        Ax + By + Cz + D = 0
    */
    [SerializeField] private Vector3 _childPosition = new Vector3(4f, 0f, 4f);
    private readonly Color ColorForSection = Color.white;
    private readonly Color ColorForOuter = Color.black;
    private readonly int NoVertixIndex = -1;
    private readonly int TasksCount = 3;
    private readonly int TrianglesIndeces = 3;
    private GameObject _secondHalf = null;
    private Mesh _secondMesh;
    private MeshFilter _detailedFilter;
    private Rigidbody _rigidbody;
    private VerticesData _detailedVertices = new VerticesData();
    private VerticesData _colliderVertices = new VerticesData();
    private List<int> _detailedTriangles;
    private List<int> _colliderTriangles;
    private List<int> _emptyList = new List<int>();
    private bool _canSlash = false;
    private bool _isBase = true;
    private System.TimeSpan total = new System.TimeSpan(0);

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _detailedFilter = GetComponent<MeshFilter>();
        var collider = GetComponent<MeshCollider>();
        Mesh mesh;
        if(_isBase)
        {
            _detailedTriangles = new List<int>();
            mesh = _detailedFilter.mesh;
            InitializeVerticesData(mesh, _detailedTriangles, _detailedVertices);
            mesh.subMeshCount = 2;
            mesh = collider.sharedMesh;
            _colliderTriangles = new List<int>();
            InitializeVerticesData(mesh, _colliderTriangles, _colliderVertices);
            //отдельный метод
            _secondHalf = Instantiate(this.gameObject);
            _secondHalf.GetComponent<ChangingMesh>()._isBase = false;
            _secondHalf.transform.position = _childPosition;
            _secondMesh = _secondHalf.GetComponent<MeshFilter>().mesh;
            StartCoroutine(AllowSlashing());
        }
    }

    public void SliceByPlane(float A, float B, float C, float D)
    {
        if(!_canSlash)
        {
            return;
        }
        _canSlash = false;
        var time = System.DateTime.Now;
        SliceByPlane(A, B, C, D, false, _detailedTriangles, _detailedVertices, ref _secondHalf);
        total += (System.DateTime.Now - time);
        Debug.Log("Time: " + total);
        SliceByPlane(A, B, C, D, true, _colliderTriangles, _colliderVertices, ref _secondHalf);
        StartCoroutine(AllowSlashing());
    }

    private void SliceByPlane(float A, float B, float C, float D, bool isCollider, List<int> triangles, VerticesData verticesData, ref GameObject secondHalf)
    {
        var sectionVertices = new Dictionary<int, int>();
        var mainVertixIndex = NoVertixIndex;
        var leftTriangles = new List<int>();
        var rightTriangles = new List<int>();
        var leftSection = new List<int>();
        var rightSection = new List<int>();
        //нужно нормально пересчитать
        float maxX = 0;
        float maxY = 0;
        float maxZ = 0;
        float minX = 0;
        float minY = 0;
        float minZ = 0;

        List<Task<TaskData>> tasks = new List<Task<TaskData>>();
        //Основной цикл
        int indecesForOneTask = (triangles.Count / TasksCount) + (TrianglesIndeces - triangles.Count / TasksCount % TrianglesIndeces);
        for(int t = 0; t < TasksCount; ++t)
        {
            int firstIndex = t * indecesForOneTask;
            int lastIndex = (t == TasksCount - 1) ? triangles.Count : indecesForOneTask + firstIndex;
            tasks.Add(new Task<TaskData>(() => {
                var taskData = new TaskData();
                var localLeftVertices = new List<int>();
                var localRightVertices = new List<int>();
                for(int i = firstIndex; i < lastIndex; i += TrianglesIndeces)
                {
                    var firstVertix = verticesData.vertices[triangles[i]];
                    var secondVertix = verticesData.vertices[triangles[i + 1]];
                    var thirdVertix = verticesData.vertices[triangles[i + 2]];
                    //исправь максимумы
                    maxX = 1;
                    minX = 0;
                    minY = 0;
                    maxY = 1;
                    minZ = 0;
                    maxZ = 1;
                    if(IsToLeftOfPlane(firstVertix, A, B, C, D) && IsToLeftOfPlane(secondVertix, A, B, C, D) && IsToLeftOfPlane(thirdVertix, A, B, C, D))
                    {
                        if(verticesData.colors[triangles[i]].Equals(ColorForSection))
                        {
                            taskData.LeftSection.Add(triangles[i]);
                            taskData.LeftSection.Add(triangles[i + 1]);
                            taskData.LeftSection.Add(triangles[i + 2]);
                        }
                        else
                        {
                            taskData.LeftTriangles.Add(triangles[i]);
                            taskData.LeftTriangles.Add(triangles[i + 1]);
                            taskData.LeftTriangles.Add(triangles[i + 2]);
                        }
                    }
                    else if(IsToRightOfPlane(firstVertix, A, B, C, D) && IsToRightOfPlane(secondVertix, A, B, C, D) && IsToRightOfPlane(thirdVertix, A, B, C, D))
                    {
                        if(verticesData.colors[triangles[i]].Equals(ColorForSection))
                        {
                            taskData.RightSection.Add(triangles[i]);
                            taskData.RightSection.Add(triangles[i + 1]);
                            taskData.RightSection.Add(triangles[i + 2]);
                        }
                        else
                        {
                            taskData.RightTriangles.Add(triangles[i]);
                            taskData.RightTriangles.Add(triangles[i + 1]);
                            taskData.RightTriangles.Add(triangles[i + 2]);
                        }
                    }
                    else
                    {
                        //Обработка разреза
                        SeparateVertices(localLeftVertices, localRightVertices, triangles, i, verticesData, A, B, C, D);
                        var colorForNewVertices = verticesData.colors[localLeftVertices[0]];
                        if(localLeftVertices.Count > localRightVertices.Count)
                        {
                            //картеж непонятен
                            var newVertices = GetNewVertices(verticesData.vertices[localLeftVertices[0]], verticesData.vertices[localLeftVertices[1]], verticesData.vertices[localRightVertices[0]],
                                            A, B, C, D);
                            if(colorForNewVertices.Equals(ColorForSection))
                            {
                                AddFormedTriangles(newVertices.Item1, newVertices.Item2, localLeftVertices, localRightVertices,
                                                taskData.LeftSection, taskData.RightSection, taskData.LeftSectionToModify, taskData.RightSectionToModify,
                                                verticesData, taskData.NewVericesData, colorForNewVertices);
                            }
                            else
                            {
                                AddFormedTriangles(newVertices.Item1, newVertices.Item2, localLeftVertices, localRightVertices,
                                                taskData.LeftTriangles, taskData.RightTriangles, taskData.LeftTrianglesToModify, taskData.RightTrianglesToModify,
                                                verticesData, taskData.NewVericesData, colorForNewVertices);
                            }
                            taskData.SectionVertices.Add(taskData.NewVericesData.Count - 1, taskData.NewVericesData.Count - 2);
                        }
                        else
                        {
                            var newVertices = GetNewVertices(verticesData.vertices[localRightVertices[0]], verticesData.vertices[localRightVertices[1]], verticesData.vertices[localLeftVertices[0]],
                                            A, B, C, D);
                            if(colorForNewVertices.Equals(ColorForSection))
                            {
                                AddFormedTriangles(newVertices.Item1, newVertices.Item2, localRightVertices, localLeftVertices,
                                                taskData.RightSection, taskData.LeftSection, taskData.RightSectionToModify, taskData.LeftSectionToModify,
                                                verticesData, taskData.NewVericesData, colorForNewVertices);
                            }
                            else
                            {
                                AddFormedTriangles(newVertices.Item1, newVertices.Item2, localRightVertices, localLeftVertices,
                                                taskData.RightTriangles, taskData.LeftTriangles, taskData.RightTrianglesToModify, taskData.LeftTrianglesToModify,
                                                verticesData,taskData.NewVericesData, colorForNewVertices);
                            }
                            taskData.SectionVertices.Add(taskData.NewVericesData.Count - 2, taskData.NewVericesData.Count - 1);
                        }
                        localLeftVertices.Clear();
                        localRightVertices.Clear();
                    }
                }
                return taskData;
            }));
            tasks[tasks.Count - 1].Start();
        }
        Task.WaitAll(tasks.ToArray());
        int offset = verticesData.Count;
        foreach(var task in tasks)
        {
            var taskData = task.Result;
            taskData.ModifyLists(offset);
            leftTriangles.AddRange(taskData.LeftTriangles);
            rightTriangles.AddRange(taskData.RightTriangles);
            leftSection.AddRange(taskData.LeftSection);
            rightSection.AddRange(taskData.RightSection);
            foreach(var indeces in taskData.SectionVertices)
            {
                sectionVertices.Add(indeces.Key + offset, indeces.Value + offset);
                if(mainVertixIndex == NoVertixIndex)
                {
                    mainVertixIndex = indeces.Key + offset;
                }
            }
            verticesData.Add(taskData.NewVericesData);
            offset += taskData.NewVericesData.Count;
        }

        var commonNormalForLeftSection = new Vector3(A, B, C);
        var commonNormalForRightSection = new Vector3(-A, -B, -C);

        //нужна еще вершина
        verticesData.normals[mainVertixIndex] = commonNormalForLeftSection;
        verticesData.normals[mainVertixIndex] = commonNormalForRightSection;
        

        //Добавление треугольников для каждого сечения
        foreach(int vertixIndex in sectionVertices.Keys)
        {
            if(mainVertixIndex == vertixIndex)
            {
                continue;
            }
            leftSection.Add(mainVertixIndex);
            leftSection.Add(vertixIndex);
            leftSection.Add(sectionVertices[vertixIndex]);

            rightSection.Add(mainVertixIndex);
            rightSection.Add(sectionVertices[vertixIndex]);
            rightSection.Add(vertixIndex);
        }

        //нужны копии вершин
        //Формирование нормалей и карты текстуры для правого объекта
        foreach(int vertixIndex in rightSection)
        {
            if(!verticesData.normals[vertixIndex].Equals(Vector3.zero))
            {
                continue;
            }
            verticesData.normals[vertixIndex] = commonNormalForRightSection;
            verticesData.uvs[vertixIndex] = new Vector2((verticesData.vertices[vertixIndex].z - minZ) / (maxZ - minZ), (verticesData.vertices[vertixIndex].y - minY) / (maxY - minY));
        }

        //Создание дочернего обрубка
        var mesh = new Mesh();
        mesh.name = "Child";
        //неявно определяется первое создание
        if(isCollider)
        {
            SetVerticesData(mesh, verticesData, rightTriangles, rightSection);
            FixUnusedVertices(mesh, rightTriangles);
            secondHalf.GetComponent<MeshCollider>().sharedMesh = mesh;
            secondHalf.GetComponent<ChangingMesh>()._colliderTriangles = rightTriangles;
            secondHalf.GetComponent<ChangingMesh>()._colliderVertices = verticesData;
            secondHalf.transform.position = this.transform.position;
            secondHalf.transform.rotation = this.transform.rotation;
            StartCoroutine(secondHalf.GetComponent<ChangingMesh>().AllowSlashing());
        }
        else
        {
            mesh = secondHalf.GetComponent<MeshFilter>().mesh;
            SetVerticesData(mesh, verticesData, rightTriangles, rightSection);
            secondHalf.GetComponent<ChangingMesh>()._detailedTriangles = rightTriangles;
            secondHalf.GetComponent<ChangingMesh>()._detailedVertices = verticesData;
        }

        //Формирование нормалей и карты текстуры для левого объекта
        foreach(int vertixIndex in leftSection)
        {
            if(!verticesData.normals[vertixIndex].Equals(Vector3.zero))
            {
                continue;
            }
            verticesData.normals[vertixIndex] = commonNormalForLeftSection;
            verticesData.uvs[vertixIndex] = new Vector2((verticesData.vertices[vertixIndex].z - minZ) / (maxX - minX), (verticesData.vertices[vertixIndex].y - minY) / (maxY - minY));
        }
        //формирование меша для родительского объекта
        mesh = new Mesh();
        mesh.name = "Base";
        if(isCollider)
        {
            var collider = GetComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            SetVerticesData(collider.sharedMesh, verticesData, leftTriangles, leftSection);
            FixUnusedVertices(collider.sharedMesh, leftTriangles);
            collider.convex = true;
            _colliderTriangles = leftTriangles;
        }
        else
        {
            mesh = GetComponent<MeshFilter>().mesh;
            SetVerticesData(mesh, verticesData, leftTriangles, leftSection);
            _detailedTriangles = leftTriangles;
        }
    }

    //лучше использовать другой подход
    private void FixUnusedVertices(Mesh mesh, List<int> triangles)
    {
        var vertices = new List<Vector3>();
        mesh.GetVertices(vertices);
        var mainVertixIndex = triangles[0];
        for(int i = 0; i < vertices.Count; ++i)
        {
            if(!triangles.Contains(i))
            {
                vertices[i] = vertices[mainVertixIndex];
            }
        }
        mesh.SetVertices(vertices);
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

    private void SetVerticesData(Mesh mesh, VerticesData oldVerticesData, List<int> triangles, List<int> sectionTriangles)
    {
        mesh.subMeshCount = 2;
        mesh.SetTriangles(_emptyList, 0, true, 0);
        mesh.SetTriangles(_emptyList, 1, true, 0);
        mesh.SetVertices(oldVerticesData.vertices);
        mesh.SetTriangles(triangles, 0, true, 0);
        mesh.SetTriangles(sectionTriangles, 1, true, 0);
        triangles.AddRange(sectionTriangles);
        mesh.SetNormals(oldVerticesData.normals);
        mesh.SetColors(oldVerticesData.colors);
        mesh.SetUVs(0, oldVerticesData.uvs);
    }

    //добавление вершин
    private void SeparateVertices(List<int> localLeftVertices, List<int> localRightVertices, List<int> oldTriangles, int firstVertixOfTriangle,
                                VerticesData verticesData,
                                float A, float B, float C, float D)//много аргументов, if-ов
    {
        bool firstIsLeft;
        bool secondIsLeft;
        var firstVertix = verticesData.vertices[oldTriangles[firstVertixOfTriangle]];
        var secondVertix = verticesData.vertices[oldTriangles[firstVertixOfTriangle + 1]];
        var thirdVertix = verticesData.vertices[oldTriangles[firstVertixOfTriangle + 2]];
        if(IsToLeftOfPlane(firstVertix, A, B, C, D))
        {
            localLeftVertices.Add(oldTriangles[firstVertixOfTriangle]);
            firstIsLeft = true;
        }
        else
        {
            localRightVertices.Add(oldTriangles[firstVertixOfTriangle]);
            firstIsLeft = false;
        }
        if(IsToLeftOfPlane(secondVertix, A, B, C, D))
        {
            localLeftVertices.Add(oldTriangles[firstVertixOfTriangle + 1]);
            secondIsLeft = true;
        }
        else
        {
            localRightVertices.Add(oldTriangles[firstVertixOfTriangle + 1]);
            secondIsLeft = false;
        }
        if(IsToLeftOfPlane(thirdVertix, A, B, C, D))
        {
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
                            VerticesData commonVerticesData, VerticesData newVerticesData)
    {
        var firstUV = commonVerticesData.uvs[firstVertixIndex];
        var secondUV = commonVerticesData.uvs[secondVertixIndex];
        var aloneUV = commonVerticesData.uvs[aloneVertixIndex];
        var magnitude = (newVerticesData.vertices[firstNewVertixIndex] - commonVerticesData.vertices[firstVertixIndex]).magnitude /
            (commonVerticesData.vertices[aloneVertixIndex] - commonVerticesData.vertices[firstVertixIndex]).magnitude;
        var offset = (aloneUV - firstUV) * magnitude;
        newVerticesData.uvs[newVerticesData.uvs.Count - 2] = firstUV + offset;//можно проще индексы
        newVerticesData.uvs[newVerticesData.uvs.Count - 2] = firstUV + offset;
        magnitude = (newVerticesData.vertices[secondNewVertixIndex] - commonVerticesData.vertices[secondVertixIndex]).magnitude /
            (commonVerticesData.vertices[aloneVertixIndex] - commonVerticesData.vertices[secondVertixIndex]).magnitude;
        offset = (aloneUV - firstUV) * magnitude;
        newVerticesData.uvs[newVerticesData.uvs.Count - 1] = secondUV + offset;
        newVerticesData.uvs[newVerticesData.uvs.Count - 1] = secondUV + offset;
    }
    //добавление вершин
    private void AddFormedTriangles(Vector3 firstVertix, Vector3 secondVertix,//много аргументов
                                    List<int> newTwoTriangles, List<int> newTriangle,//непонятны названия
                                    List<int> listForTwoTriangles, List<int> listForOneTriangle,
                                    List<int> listToModifyForTwoTriangles, List<int> listToModifyForOneTriangles,
                                    VerticesData commonVerticesData, VerticesData newVerticesData,
                                    Color colorForNewVertices)
    {
        newVerticesData.vertices.Add(firstVertix);
        newVerticesData.vertices.Add(secondVertix);
        newVerticesData.normals.Add(commonVerticesData.normals[newTriangle[0]]);
        newVerticesData.normals.Add(commonVerticesData.normals[newTriangle[0]]);
        newVerticesData.colors.Add(colorForNewVertices);
        newVerticesData.colors.Add(colorForNewVertices);
        newVerticesData.uvs.Add(Vector2.zero);
        newVerticesData.uvs.Add(Vector2.zero);
        
        //можно избавиться от лишних параметров
        SetNewUVs(newTwoTriangles[0], newTwoTriangles[1], newTriangle[0], 
                newVerticesData.Count - 2, newVerticesData.Count - 1,
                commonVerticesData, newVerticesData);

        listForTwoTriangles.Add(newTwoTriangles[0]);
        listForTwoTriangles.Add(newVerticesData.Count - 2);
        listToModifyForTwoTriangles.Add(listForTwoTriangles.Count - 1);
        listForTwoTriangles.Add(newTwoTriangles[1]);

        listForTwoTriangles.Add(newTwoTriangles[1]);
        listForTwoTriangles.Add(newVerticesData.Count - 2);
        listToModifyForTwoTriangles.Add(listForTwoTriangles.Count - 1);
        listForTwoTriangles.Add(newVerticesData.Count - 1);
        listToModifyForTwoTriangles.Add(listForTwoTriangles.Count - 1);

        listForOneTriangle.Add(newVerticesData.Count - 2);
        listToModifyForOneTriangles.Add(listForOneTriangle.Count - 1);
        listForOneTriangle.Add(newTriangle[0]);
        listForOneTriangle.Add(newVerticesData.Count - 1);
        listToModifyForOneTriangles.Add(listForOneTriangle.Count - 1);

        //непонятный элемент     
        //нужны еще 2 вершины для двух сечений  
        newVerticesData.vertices.Add(firstVertix);
        newVerticesData.vertices.Add(secondVertix);
        newVerticesData.normals.Add(Vector3.zero);
        newVerticesData.normals.Add(Vector3.zero);
        newVerticesData.colors.Add(ColorForSection);
        newVerticesData.colors.Add(ColorForSection);
        newVerticesData.uvs.Add(Vector2.zero);
        newVerticesData.uvs.Add(Vector2.zero);
    }

    /*
        формула плоскости
    */
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
        yield return new WaitForSeconds(2);

        if(!_isBase)
        {
            _secondHalf = Instantiate(this.gameObject);
            _secondHalf.GetComponent<ChangingMesh>()._isBase = false;
            _secondHalf.transform.position = _childPosition;
            _secondMesh = _secondHalf.GetComponent<MeshFilter>().mesh;
        }
        _canSlash = true;
        _isBase = false;
    }
}
