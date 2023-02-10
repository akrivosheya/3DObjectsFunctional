using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChangingMesh : MonoBehaviour
{
    /*
        Ax + By + Cz + D = 0
    */
    [SerializeField] private Vector3 ChildPosition = new Vector3(4f, 0f, 4f);
    private readonly Color ColorForSection = Color.white;
    private readonly Color ColorForOuter = Color.black;
    private readonly int NoIndex = -1;
    private readonly int TasksCount = 4;
    private const int TriangleIndecesCount = 3;
    private GameObject _childObject = null;
    private Mesh _childMesh;
    private VerticesData _renderingVerticesData = new VerticesData();
    private VerticesData _colliderVerticesData = new VerticesData();
    private List<int> _renderingTriangles;
    private List<int> _colliderTriangles;
    private List<int> _emptyList = new List<int>();
    private bool _canSlash = false;
    private bool _isBase = true;
    private System.TimeSpan total = new System.TimeSpan(0);

    void Start()
    {
        if(_isBase)
        {
            _renderingTriangles = new List<int>();
            Mesh mesh = GetComponent<MeshFilter>().mesh;
            InitializeData(mesh, _renderingTriangles, _renderingVerticesData);
            mesh.subMeshCount = 2;
            
            _colliderTriangles = new List<int>();
            mesh = GetComponent<MeshCollider>().sharedMesh;
            InitializeData(mesh, _colliderTriangles, _colliderVerticesData);

            StartCoroutine(PrepareForSlashing());
        }
    }

    public void SliceByPlane(float A, float B, float C, float D)
    {
        if(!_canSlash)
        {
            return;
        }
        _canSlash = false;
        var plane = new Plane { A=A, B=B, C=C, D=D };
        var time = System.DateTime.Now;
        SliceByPlane(plane, false, _renderingTriangles, _renderingVerticesData);
        total += (System.DateTime.Now - time);
        Debug.Log("Time: " + total);
        SliceByPlane(plane, true, _colliderTriangles, _colliderVerticesData);
        StartCoroutine(PrepareForSlashing());
    }

    private void SliceByPlane(Plane plane, bool isCollider, List<int> triangles, VerticesData verticesData)
    {
        var sectionIndeces = new Dictionary<int, int>();
        var mainSectionIndex = NoIndex;
        var leftTriangles = new List<int>();
        var rightTriangles = new List<int>();
        var leftSection = new List<int>();
        var rightSection = new List<int>();

        List<Task<TaskData>> tasks = new List<Task<TaskData>>();
        int indecesCountForOneTask = (triangles.Count / TasksCount) + 
        (TriangleIndecesCount - triangles.Count / TasksCount % TriangleIndecesCount);
        for(int t = 0; t < TasksCount; ++t)
        {
            int firstIndex = t * indecesCountForOneTask;
            int lastIndex = (t == TasksCount - 1) ? triangles.Count : indecesCountForOneTask + firstIndex;
            tasks.Add(new Task<TaskData>(() => {
                var taskData = new TaskData();
                var localLeftIndeces = new List<int>();
                var localRightIndeces = new List<int>();
                //Основной цикл
                for(int i = firstIndex; i < lastIndex; i += TriangleIndecesCount)
                {
                    var firstIndex = triangles[i];
                    var secondIndex = triangles[i + 1];
                    var thirdIndex = triangles[i + 2];
                    var firstVertix = verticesData.vertices[firstIndex];
                    var secondVertix = verticesData.vertices[secondIndex];
                    var thirdVertix = verticesData.vertices[thirdIndex];
                    if(VertixIsToLeftOfPlane(firstVertix, plane) &&
                        VertixIsToLeftOfPlane(secondVertix, plane) &&
                        VertixIsToLeftOfPlane(thirdVertix, plane))
                    {
                        taskData.AddToLeftHalf(verticesData.colors[firstIndex].Equals(ColorForSection),
                            firstIndex, secondIndex, thirdIndex);
                    }
                    else if(IsToRightOfPlane(firstVertix, plane) &&
                        IsToRightOfPlane(secondVertix, plane) &&
                        IsToRightOfPlane(thirdVertix, plane))
                    {
                        taskData.AddToRightHalf(verticesData.colors[firstIndex].Equals(ColorForSection),
                            firstIndex, secondIndex, thirdIndex);
                    }
                    else
                    {
                        //Обработка разреза
                        SeparateVertices(localLeftIndeces, localRightIndeces, triangles, i, verticesData, plane);
                        var colorForNewVertices = verticesData.colors[localLeftIndeces[0]];
                        if(localLeftIndeces.Count > localRightIndeces.Count)
                        {
                            var newVertices = GetNewVertices(
                                verticesData.vertices[localLeftIndeces[0]],
                                verticesData.vertices[localLeftIndeces[1]],
                                verticesData.vertices[localRightIndeces[0]],
                                plane);
                            if(colorForNewVertices.Equals(ColorForSection))
                            {
                                AddFormedTriangles(newVertices, localLeftIndeces, localRightIndeces[0],
                                    taskData.LeftSection, taskData.RightSection, taskData.LeftSectionToModify, taskData.RightSectionToModify,
                                    verticesData, taskData.NewVerticesData, colorForNewVertices);
                            }
                            else
                            {
                                AddFormedTriangles(newVertices, localLeftIndeces, localRightIndeces[0],
                                    taskData.LeftTriangles, taskData.RightTriangles, taskData.LeftTrianglesToModify, taskData.RightTrianglesToModify,
                                    verticesData, taskData.NewVerticesData, colorForNewVertices);
                            }
                            taskData.SectionIndeces.Add(taskData.NewVerticesData.Count - 1, taskData.NewVerticesData.Count - 2);
                        }
                        else
                        {
                            var newVertices = GetNewVertices(
                                verticesData.vertices[localRightIndeces[0]],
                                verticesData.vertices[localRightIndeces[1]],
                                verticesData.vertices[localLeftIndeces[0]],
                                plane);
                            if(colorForNewVertices.Equals(ColorForSection))
                            {
                                AddFormedTriangles(newVertices, localRightIndeces, localLeftIndeces[0],
                                    taskData.RightSection, taskData.LeftSection, taskData.RightSectionToModify, taskData.LeftSectionToModify,
                                    verticesData, taskData.NewVerticesData, colorForNewVertices);
                            }
                            else
                            {
                                AddFormedTriangles(newVertices, localRightIndeces, localLeftIndeces[0],
                                    taskData.RightTriangles, taskData.LeftTriangles, taskData.RightTrianglesToModify, taskData.LeftTrianglesToModify,
                                    verticesData,taskData.NewVerticesData, colorForNewVertices);
                            }
                            taskData.SectionIndeces.Add(taskData.NewVerticesData.Count - 2, taskData.NewVerticesData.Count - 1);
                        }
                        localLeftIndeces.Clear();
                        localRightIndeces.Clear();
                    }
                }
                return taskData;
            }));
            tasks[tasks.Count - 1].Start();
        }
        Task.WaitAll(tasks.ToArray());
        int offset = verticesData.Count;
        float? maxX = null;
        float? maxY = null;
        float? maxZ = null;
        float? minX = null;
        float? minY = null;
        float? minZ = null;
        foreach(var task in tasks)
        {
            var taskData = task.Result;
            taskData.ModifyLists(offset);
            leftTriangles.AddRange(taskData.LeftTriangles);
            rightTriangles.AddRange(taskData.RightTriangles);
            leftSection.AddRange(taskData.LeftSection);
            rightSection.AddRange(taskData.RightSection);
            foreach(var indeces in taskData.SectionIndeces)
            {
                sectionIndeces.Add(indeces.Key + offset, indeces.Value + offset);
                if(mainSectionIndex == NoIndex)
                {
                    mainSectionIndex = indeces.Key + offset;
                }
            }
            foreach(var vertix in taskData.NewVerticesData.vertices)
            {
                maxX = Max(maxX, vertix.x);
                maxY = Max(maxY, vertix.y);
                maxZ = Max(maxZ, vertix.z);
                minX = Min(minX, vertix.x);
                minY = Min(minY, vertix.y);
                minZ = Min(minZ, vertix.z);
            }
            verticesData.Add(taskData.NewVerticesData);
            offset += taskData.NewVerticesData.Count;
        }

        var commonNormalForLeftSection = new Vector3(plane.A, plane.B, plane.C);
        var commonNormalForRightSection = new Vector3(-plane.A, -plane.B, -plane.C);
        

        //Добавление треугольников для каждого сечения
        foreach(int vertixIndex in sectionIndeces.Keys)
        {
            if(mainSectionIndex == vertixIndex)
            {
                continue;
            }
            leftSection.Add(mainSectionIndex);
            leftSection.Add(vertixIndex);
            leftSection.Add(sectionIndeces[vertixIndex]);

            rightSection.Add(mainSectionIndex);
            rightSection.Add(sectionIndeces[vertixIndex]);
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
            verticesData.uvs[vertixIndex] = GetUvsForVertix(verticesData.vertices[vertixIndex], 
                (float)maxX, (float)maxY, (float)maxZ, (float)minX, (float)minY, (float)minZ);
        }

        //Создание дочернего обрубка
        var mesh = new Mesh();
        mesh.name = "Child";
        //неявно определяется первое создание
        if(isCollider)
        {
            SetVerticesData(mesh, verticesData, rightTriangles, rightSection);
            FixUnusedVertices(mesh, rightTriangles);
            _childObject.GetComponent<MeshCollider>().sharedMesh = mesh;
            _childObject.GetComponent<ChangingMesh>()._colliderTriangles = rightTriangles;
            _childObject.GetComponent<ChangingMesh>()._colliderVerticesData = verticesData;
            _childObject.transform.position = this.transform.position;
            _childObject.transform.rotation = this.transform.rotation;
            StartCoroutine(_childObject.GetComponent<ChangingMesh>().PrepareForSlashing());
        }
        else
        {
            mesh = _childObject.GetComponent<MeshFilter>().mesh;
            SetVerticesData(mesh, verticesData, rightTriangles, rightSection);
            _childObject.GetComponent<ChangingMesh>()._renderingTriangles = rightTriangles;
            _childObject.GetComponent<ChangingMesh>()._renderingVerticesData = verticesData;
        }

        //Формирование нормалей и карты текстуры для левого объекта
        foreach(int vertixIndex in leftSection)
        {
            if(!verticesData.normals[vertixIndex].Equals(Vector3.zero))
            {
                continue;
            }
            verticesData.normals[vertixIndex] = commonNormalForLeftSection;
            verticesData.uvs[vertixIndex] = GetUvsForVertix(verticesData.vertices[vertixIndex], 
                (float)maxX, (float)maxY, (float)maxZ, (float)minX, (float)minY, (float)minZ);
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
            _renderingTriangles = leftTriangles;
        }
    }

    //лучше использовать другой подход
    private void FixUnusedVertices(Mesh mesh, List<int> triangles)
    {
        var vertices = new List<Vector3>();
        mesh.GetVertices(vertices);
        var mainSectionIndex = triangles[0];
        for(int i = 0; i < vertices.Count; ++i)
        {
            if(!triangles.Contains(i))
            {
                vertices[i] = vertices[mainSectionIndex];
            }
        }
        mesh.SetVertices(vertices);
    }

    private void InitializeData(Mesh mesh, List<int> triangles, VerticesData verticesData)
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
    private void SeparateVertices(List<int> localLeftIndeces, List<int> localRightIndeces, List<int> triangles,
                                int firstIndexOfTriangle, VerticesData verticesData, Plane plane)
    {
        var indeces = new int[TriangleIndecesCount] {
            triangles[firstIndexOfTriangle],
            triangles[firstIndexOfTriangle + 1],
            triangles[firstIndexOfTriangle + 2]
        };
        var vertices = new Vector3[TriangleIndecesCount] {
            verticesData.vertices[triangles[firstIndexOfTriangle]],
            verticesData.vertices[triangles[firstIndexOfTriangle + 1]],
            verticesData.vertices[triangles[firstIndexOfTriangle + 2]]
        };
        var verticesAreToLeftOfPlane = new bool[TriangleIndecesCount] {
            VertixIsToLeftOfPlane(vertices[0], plane),
            VertixIsToLeftOfPlane(vertices[1], plane),
            VertixIsToLeftOfPlane(vertices[2], plane),
        };
        while(!((verticesAreToLeftOfPlane[0] ^ verticesAreToLeftOfPlane[1]) &&
            (verticesAreToLeftOfPlane[1] ^ verticesAreToLeftOfPlane[2])))
        {
            ShiftArray(indeces);
            ShiftArray(vertices);
            ShiftArray(verticesAreToLeftOfPlane);
        }
        for(int i = 0; i < TriangleIndecesCount; ++i)
        {
            if(verticesAreToLeftOfPlane[i])
            {
                localLeftIndeces.Add(indeces[i]);
            }
            else
            {
                localRightIndeces.Add(indeces[i]);
            }
        }
    }

    private (Vector3, Vector3) GetNewVertices(Vector3 firstVertix, Vector3 secondVertix, Vector3 aloneVertix, Plane plane)
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
        float t = -(plane.A * firstVertix.x + plane.B * firstVertix.y + plane.C * firstVertix.z + plane.D) /
            (plane.A * m + plane.B * n + plane.C * p);
        firstNewVertix.x = m * t + firstVertix.x;
        firstNewVertix.y = n * t + firstVertix.y;
        firstNewVertix.z = p * t + firstVertix.z;

        m = secondVertix.x - aloneVertix.x;
        n = secondVertix.y - aloneVertix.y;
        p = secondVertix.z - aloneVertix.z;
        t = -(plane.A * secondVertix.x + plane.B * secondVertix.y + plane.C * secondVertix.z + plane.D) /
            (plane.A * m + plane.B * n + plane.C * p);
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
    private void AddFormedTriangles((Vector3, Vector3) newVertices,//много аргументов
                                    List<int> localTwoIndeces, int localAloneIndex,
                                    List<int> listForNewTwoTriangles, List<int> ListForNewOneTriangle,
                                    List<int> listToModifyListForTwoNewTriangles, List<int> listToModifyListForNewOneTriangles,
                                    VerticesData commonVerticesData, VerticesData newVerticesData,
                                    Color colorForNewVertices)
    {
        newVerticesData.vertices.Add(newVertices.Item1);
        newVerticesData.vertices.Add(newVertices.Item2);
        newVerticesData.normals.Add(commonVerticesData.normals[localAloneIndex]);
        newVerticesData.normals.Add(commonVerticesData.normals[localAloneIndex]);
        newVerticesData.colors.Add(colorForNewVertices);
        newVerticesData.colors.Add(colorForNewVertices);
        newVerticesData.uvs.Add(Vector2.zero);
        newVerticesData.uvs.Add(Vector2.zero);
        
        //можно избавиться от лишних параметров
        SetNewUVs(localTwoIndeces[0], localTwoIndeces[1], localAloneIndex, 
                newVerticesData.Count - 2, newVerticesData.Count - 1,
                commonVerticesData, newVerticesData);

        listForNewTwoTriangles.Add(localTwoIndeces[0]);
        listForNewTwoTriangles.Add(newVerticesData.Count - 2);
        listToModifyListForTwoNewTriangles.Add(listForNewTwoTriangles.Count - 1);
        listForNewTwoTriangles.Add(localTwoIndeces[1]);

        listForNewTwoTriangles.Add(localTwoIndeces[1]);
        listForNewTwoTriangles.Add(newVerticesData.Count - 2);
        listToModifyListForTwoNewTriangles.Add(listForNewTwoTriangles.Count - 1);
        listForNewTwoTriangles.Add(newVerticesData.Count - 1);
        listToModifyListForTwoNewTriangles.Add(listForNewTwoTriangles.Count - 1);

        ListForNewOneTriangle.Add(newVerticesData.Count - 2);
        listToModifyListForNewOneTriangles.Add(ListForNewOneTriangle.Count - 1);
        ListForNewOneTriangle.Add(localAloneIndex);
        ListForNewOneTriangle.Add(newVerticesData.Count - 1);
        listToModifyListForNewOneTriangles.Add(ListForNewOneTriangle.Count - 1);

        //непонятный элемент     
        //нужны еще 2 вершины для двух сечений  
        newVerticesData.vertices.Add(newVertices.Item1);
        newVerticesData.vertices.Add(newVertices.Item2);
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
    private bool VertixIsToLeftOfPlane(Vector3 point, Plane plane)
    {
        return plane.A * point.x + plane.B * point.y + plane.C * point.z + plane.D < 0 || IsOnPlane(point, plane);
    }

    private bool IsToRightOfPlane(Vector3 point, Plane plane)
    {
        return plane.A * point.x + plane.B * point.y + plane.C * point.z + plane.D > 0;
    }

    private bool IsOnPlane(Vector3 point, Plane plane)
    {
        return plane.A * point.x + plane.B * point.y + plane.C * point.z + plane.D == 0;
    }

    private float Min(float? min, float newNumb)
    {
        if(min == null)
        {
            return newNumb;
        }
        else
        {
            return (min <= newNumb) ? (float)min : newNumb;
        }
    }

    private float Max(float? max, float newNumb)
    {
        if(max == null)
        {
            return newNumb;
        }
        else
        {
            return (max >= newNumb) ? (float)max : newNumb;
        }
    }

    private void ShiftArray<T>(T[] array)
    {
        var tmp = array[0];
        array[0] = array[1];
        array[1] = array[2];
        array[2] = tmp;
    }

    private Vector2 GetUvsForVertix(Vector3 vertix, 
                                    float maxX, float maxY, float maxZ,
                                    float minX, float minY, float minZ)
    {
        float magnitudeX = maxX - minX;
        float magnitudeY = maxY - minY;
        float magnitudeZ = maxZ - minZ;
        float firstMagnitude, secondMagnitude, firstCoordinate, secondCoordinate, minMagnitude, minCoordinate;
        (firstMagnitude, firstCoordinate, minMagnitude, minCoordinate) = (magnitudeX > magnitudeY) ? 
            (magnitudeX, vertix.x - minX, magnitudeY, vertix.y - minY) :
            (magnitudeY, vertix.y - minY, magnitudeX, vertix.x - minX);
        (secondMagnitude, secondCoordinate) = (magnitudeZ > minMagnitude) ? 
            (magnitudeZ, vertix.z - minZ) : (minMagnitude, minCoordinate);
        return new Vector2(firstCoordinate, secondCoordinate);
    }

    private IEnumerator PrepareForSlashing()
    {
        yield return new WaitForSeconds(2);

        _childObject = Instantiate(this.gameObject);
        _childObject.GetComponent<ChangingMesh>()._isBase = false;
        _childObject.transform.position = ChildPosition;
        _childMesh = _childObject.GetComponent<MeshFilter>().mesh;
        _canSlash = true;
    }
}
