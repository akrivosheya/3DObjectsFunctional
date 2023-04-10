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
    [SerializeField] private int TimeForNoSlashing = 1;
    [SerializeField] private int MaxSlicesCount = 2;
    private readonly Color ColorForSection = Color.white;
    private readonly Color ColorForOuter = Color.black;
    private readonly string ChildMeshName = "Child";
    private readonly string BaseMeshName = "Base";
    private readonly int NoIndex = -1;
    private readonly int TasksCount = 4;
    private const int TriangleIndecesCount = 3;
    private GameObject _childObject = null;
    private Mesh _childMesh;
    private VerticesData _renderingVerticesData = new VerticesData();
    private VerticesData _colliderVerticesData = new VerticesData();
    private Task _unusedVerticesCollectingTask;
    private Task<(TaskData, TaskData)> _slashingTask;
    private List<int> _renderingTriangles;
    private List<int> _colliderTriangles;
    private List<int> _emptyList = new List<int>();
    private int _slicesCount;
    private bool _canSlash = false;
    private bool _isSlashing = false;
    private bool _isBase = true;
    private System.TimeSpan total = new System.TimeSpan(0);

    void Start()
    {
        if(_isBase)
        {
            _slicesCount = 0;
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

    void Update()
    {
        if(_isSlashing && _slashingTask.IsCompleted)
        {
            (TaskData renderingData, TaskData colliderData) = _slashingTask.Result;
            var childChangingMesh = _childObject.GetComponent<ChangingMesh>();
            SetDataToObject(_childObject, childChangingMesh._renderingVerticesData, renderingData.RightTriangles, renderingData.RightSection, false, ChildMeshName, this.transform);
            SetDataToObject(this.gameObject, _renderingVerticesData, renderingData.LeftTriangles, renderingData.LeftSection, false, BaseMeshName, this.transform);
            SetDataToObject(_childObject, childChangingMesh._colliderVerticesData, colliderData.RightTriangles, colliderData.RightSection, true, ChildMeshName, this.transform);
            SetDataToObject(this.gameObject, _colliderVerticesData, colliderData.LeftTriangles, colliderData.LeftSection, true, BaseMeshName, this.transform);
            _childObject.SetActive(true);
            _childObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;

            if(_slicesCount >= MaxSlicesCount)
            {
                Destroy(childChangingMesh);
                Destroy(this);
            }
            else
            {
                childChangingMesh._slicesCount = _slicesCount;
            }
            _isSlashing = false;
        }
    }

    public void SliceByPlane(float A, float B, float C, float D)
    {
        if(!_canSlash)
        {
            return;
        }
        ++_slicesCount;
        _canSlash = false;
        var childChangingMesh = _childObject.GetComponent<ChangingMesh>();
        _slashingTask = new Task<(TaskData, TaskData)>(() => 
        {
            var plane = new Plane { A=A, B=B, C=C, D=D };
            var time = System.DateTime.Now;
            var taskDataRendering = SliceByPlane(plane, false, _renderingTriangles, _renderingVerticesData, childChangingMesh._renderingVerticesData);
            total += (System.DateTime.Now - time);
            Debug.Log("Time: " + total);
            var taskDataCollider = SliceByPlane(plane, true, _colliderTriangles, _colliderVerticesData, childChangingMesh._colliderVerticesData);
            return (taskDataRendering, taskDataCollider);
        });
        _slashingTask.Start();
        _isSlashing = true;
    }

    private TaskData SliceByPlane(Plane plane, bool isCollider, List<int> triangles, VerticesData verticesData, VerticesData childVerticesData)
    {
        var sectionIndeces = new Dictionary<int, int>();
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
            tasks.Add(GetTaskToEvaluateDataByPlane(firstIndex, lastIndex, triangles, verticesData, plane));
            tasks[tasks.Count - 1].Start();
        }
        Task.WaitAll(tasks.ToArray());

        int offset = verticesData.Count;
        JoinFormedData(verticesData, childVerticesData, sectionIndeces, tasks, leftTriangles, rightTriangles, leftSection, rightSection);
        var mainSectionIndex = GetMainSectionIndex(sectionIndeces);
        var maxMinCoordinates = FindMaxMinCoordinates(verticesData, offset);
        SetIndecesForSections(sectionIndeces, mainSectionIndex, leftSection, rightSection);

        SetVerticesDataForSection(sectionIndeces, childVerticesData, new Vector3(-plane.A, -plane.B, -plane.C), maxMinCoordinates);
        SetVerticesDataForSection(sectionIndeces, verticesData, new Vector3(plane.A, plane.B, plane.C), maxMinCoordinates);

        return new TaskData{ LeftTriangles=leftTriangles, RightTriangles=rightTriangles, LeftSection=leftSection, RightSection=rightSection };
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

    private void DeleteUnusedVertices(VerticesData verticesData, List<int> triangles)
    {
        var mapOldIndecesToNew = new Dictionary<int,int>();
        var newVerticesData = new VerticesData();
        for(int i = 0; i < triangles.Count; ++i)
        {
            var vertixIndex = triangles[i];
            if(mapOldIndecesToNew.ContainsKey(vertixIndex))
            {
                triangles[i] = mapOldIndecesToNew[vertixIndex];
            }
            else
            {
                newVerticesData.AddFrom(verticesData, vertixIndex);
                mapOldIndecesToNew[triangles[i]] = newVerticesData.Count - 1;
                triangles[i] = newVerticesData.Count - 1;
            }
        }
        verticesData.Set(newVerticesData);
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

    private void SetVerticesData(Mesh mesh, VerticesData verticesData, List<int> triangles, List<int> sectionTriangles)
    {
        mesh.subMeshCount = 2;
        mesh.SetTriangles(_emptyList, 0, true, 0);
        mesh.SetTriangles(_emptyList, 1, true, 0);
        mesh.SetVertices(verticesData.vertices);
        mesh.SetTriangles(triangles, 0, true, 0);
        mesh.SetTriangles(sectionTriangles, 1, true, 0);
        triangles.AddRange(sectionTriangles);
        mesh.SetNormals(verticesData.normals);
        mesh.SetColors(verticesData.colors);
        mesh.SetUVs(0, verticesData.uvs);
    }

    //что-то сделать с асинхронностью
    private Task<TaskData> GetTaskToEvaluateDataByPlane(int beginningIndex, int lastIndex, 
                                            List<int> triangles, VerticesData verticesData, Plane plane)
    {
        return new Task<TaskData>(() => {
            var taskData = new TaskData();
            var localLeftIndeces = new List<int>();
            var localRightIndeces = new List<int>();
            //Основной цикл
            for(int i = beginningIndex; i < lastIndex; i += TriangleIndecesCount)
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
                    (Vector3, Vector3) newVertices = GetNewVertices(verticesData, localLeftIndeces, localRightIndeces, plane);
                    AddFormedTriangles(newVertices, localLeftIndeces, localRightIndeces,
                        verticesData, colorForNewVertices, taskData);
                    localLeftIndeces.Clear();
                    localRightIndeces.Clear();
                }
            }
            return taskData;
        });
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
            verticesData.vertices[indeces[0]],
            verticesData.vertices[indeces[1]],
            verticesData.vertices[indeces[2]]
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

    private (Vector3, Vector3) GetNewVertices(VerticesData verticesData, List<int> localLeftIndeces,
                                            List<int> localRightIndeces, Plane plane)
    {
        /*
            x = mt + x0
            y = nt + y0
            z = pt + z0

            t = -(Ax0 + By0 + Cz0 + D) / (Am + Bn + Cp)
        */
        (int firstIndex, int secondIndex, int aloneIndex) = (localLeftIndeces.Count > localRightIndeces.Count) ?
            (localLeftIndeces[0], localLeftIndeces[1], localRightIndeces[0]) :
            (localRightIndeces[0], localRightIndeces[1], localLeftIndeces[0]);
        var firstVertix = verticesData.vertices[firstIndex];
        var secondVertix = verticesData.vertices[secondIndex];
        var aloneVertix = verticesData.vertices[aloneIndex];

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

    private void SetNewUVs(List<int> twoIndeces, int aloneVertixIndex,
                            int firstNewIndex, int secondNewIndex,
                            VerticesData commonVerticesData, VerticesData newVerticesData)
    {
        var firstIndex = twoIndeces[0];
        var secondIndex = twoIndeces[1];
        var firstUV = commonVerticesData.uvs[firstIndex];
        var secondUV = commonVerticesData.uvs[secondIndex];
        var aloneUV = commonVerticesData.uvs[aloneVertixIndex];
        var firstVertix = commonVerticesData.vertices[firstIndex];
        var secondVertix = commonVerticesData.vertices[secondIndex];
        var aloneVertix = commonVerticesData.vertices[aloneVertixIndex];
        var firstNewVertix = newVerticesData.vertices[firstNewIndex];
        var secondNewVertix = newVerticesData.vertices[secondNewIndex];

        var magnitude = (firstNewVertix - firstVertix).magnitude / (aloneVertix - firstVertix).magnitude;
        var offset = (aloneUV - firstUV) * magnitude;
        newVerticesData.uvs[firstNewIndex] = firstUV + offset;

        magnitude = (secondNewVertix - secondVertix).magnitude / (aloneVertix - secondVertix).magnitude;
        offset = (aloneUV - secondUV) * magnitude;
        newVerticesData.uvs[secondNewIndex] = secondUV + offset;
    }

    private void AddFormedTriangles((Vector3, Vector3) newVertices,
                                    List<int> localLeftIndeces, List<int> localRightIndeces,
                                    VerticesData commonVerticesData, Color colorForNewVertices,
                                    TaskData taskData)
    {
        var newVerticesData = taskData.NewVerticesData;
        var localTwoIndeces = (localLeftIndeces.Count > localRightIndeces.Count) ? localLeftIndeces : localRightIndeces;
        var localAloneIndex = (localLeftIndeces.Count < localRightIndeces.Count) ? localLeftIndeces[0] : localRightIndeces[0];
        var leftHalfHasMoreVertices = localLeftIndeces.Count > localRightIndeces.Count;
        List<int> listForNewTwoTriangles, listForNewOneTriangle, listToModifyListForTwoNewTriangles, listToModifyListForNewOneTriangles;
        SetCorrectLists(taskData,out listForNewTwoTriangles, out listForNewOneTriangle,
            out listToModifyListForTwoNewTriangles, out listToModifyListForNewOneTriangles,
            leftHalfHasMoreVertices, colorForNewVertices.Equals(ColorForSection));

        newVerticesData.Add(newVertices.Item1, commonVerticesData.normals[localAloneIndex],
            colorForNewVertices, Vector2.zero);
        newVerticesData.Add(newVertices.Item2, commonVerticesData.normals[localAloneIndex],
            colorForNewVertices, Vector2.zero);
        
        SetNewUVs(localTwoIndeces, localAloneIndex, newVerticesData.Count - 2, newVerticesData.Count - 1,
            commonVerticesData, newVerticesData);

        AddFromedTriangles(listForNewTwoTriangles, listForNewOneTriangle, listToModifyListForTwoNewTriangles, listToModifyListForNewOneTriangles,
            localTwoIndeces, localAloneIndex, newVerticesData.Count - 2, newVerticesData.Count - 1);

        //нужны еще 2 вершины для двух сечений
        newVerticesData.Add(newVertices.Item1, Vector3.zero, ColorForSection, Vector2.zero);
        newVerticesData.Add(newVertices.Item2, Vector3.zero, ColorForSection, Vector2.zero);
        
        taskData.AddSectionsIndeces(taskData.NewVerticesData.Count - 1, taskData.NewVerticesData.Count - 2, leftHalfHasMoreVertices);
    }

    private void AddFromedTriangles(List<int> listForNewTwoTriangles, List<int> listForNewOneTriangle,
                                    List<int> listToModifyListForTwoNewTriangles, List<int> listToModifyListForNewOneTriangles,
                                    List<int> localTwoIndeces, int localAloneIndex,
                                    int firstNewIndex, int secondNewIndex)
    {
        listForNewTwoTriangles.Add(localTwoIndeces[0]);
        listForNewTwoTriangles.Add(firstNewIndex);
        listToModifyListForTwoNewTriangles.Add(listForNewTwoTriangles.Count - 1);
        listForNewTwoTriangles.Add(localTwoIndeces[1]);

        listForNewTwoTriangles.Add(localTwoIndeces[1]);
        listForNewTwoTriangles.Add(firstNewIndex);
        listToModifyListForTwoNewTriangles.Add(listForNewTwoTriangles.Count - 1);
        listForNewTwoTriangles.Add(secondNewIndex);
        listToModifyListForTwoNewTriangles.Add(listForNewTwoTriangles.Count - 1);

        listForNewOneTriangle.Add(firstNewIndex);
        listToModifyListForNewOneTriangles.Add(listForNewOneTriangle.Count - 1);
        listForNewOneTriangle.Add(localAloneIndex);
        listForNewOneTriangle.Add(secondNewIndex);
        listToModifyListForNewOneTriangles.Add(listForNewOneTriangle.Count - 1);
    }

    private void JoinFormedData(VerticesData verticesData, VerticesData childVerticesData,
                                Dictionary<int, int> sectionIndeces, List<Task<TaskData>> tasks,
                                List<int> leftTriangles, List<int> rightTriangles, List<int> leftSection, List<int> rightSection)
    {
        int offset = verticesData.Count;
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
            }
            verticesData.Add(taskData.NewVerticesData);
            childVerticesData.Add(taskData.NewVerticesData);
            offset += taskData.NewVerticesData.Count;
        }
    }

    private int GetMainSectionIndex(Dictionary<int, int> sectionIndeces)
    {
        var mainSectionIndex = NoIndex;
        foreach(var indeces in sectionIndeces)
        {
            if(mainSectionIndex == NoIndex)
            {
                mainSectionIndex = indeces.Key;
            }
        }
        return mainSectionIndex;
    }

    private MaxMinCoordinates FindMaxMinCoordinates(VerticesData verticesData, int offset)
    {
        float? nullableMaxX = null;
        float? nullableMaxY = null;
        float? nullableMaxZ = null;
        float? nullableMinX = null;
        float? nullableMinY = null;
        float? nullableMinZ = null;
        for(int i = offset; i < verticesData.Count; ++i)
        {
            var vertix = verticesData.vertices[i];
            nullableMaxX = Max(nullableMaxX, vertix.x);
            nullableMaxY = Max(nullableMaxY, vertix.y);
            nullableMaxZ = Max(nullableMaxZ, vertix.z);
            nullableMinX = Min(nullableMinX, vertix.x);
            nullableMinY = Min(nullableMinY, vertix.y);
            nullableMinZ = Min(nullableMinZ, vertix.z);
        }
        return new MaxMinCoordinates {
            MaxX = (float)nullableMaxX,
            MaxY = (float)nullableMaxY,
            MaxZ = (float)nullableMaxZ,
            MinX = (float)nullableMinX,
            MinY = (float)nullableMinY,
            MinZ = (float)nullableMinZ
        };
    }

    private void SetIndecesForSections(Dictionary<int, int> sectionIndeces, int mainSectionIndex,
                                        List<int> leftSection, List<int> rightSection)
    {
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
    }

    private void SetVerticesDataForSection(Dictionary<int, int> sectionIndeces, VerticesData verticesData, Vector3 commonNormal,
                                            MaxMinCoordinates coordinates)
    {
        foreach(var indeces in sectionIndeces)
        {
            verticesData.normals[indeces.Key] = commonNormal;
            verticesData.normals[indeces.Value] = commonNormal;
            verticesData.uvs[indeces.Key] = GetUvsForVertix(verticesData.vertices[indeces.Key], coordinates);
            verticesData.uvs[indeces.Value] = GetUvsForVertix(verticesData.vertices[indeces.Value], coordinates);
        }
    }

    private void SetDataToObject(GameObject obj, VerticesData verticesData, List<int> triangles, List<int> section,
                                bool isCollider, string MeshName, Transform correctTransform)
    {
        Mesh mesh;
        var changingMesh = obj.GetComponent<ChangingMesh>();
        var objectTransform = obj.transform;
        if(isCollider)
        {
            mesh = new Mesh();
            mesh.name = MeshName;
            var collider = obj.GetComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            SetVerticesData(mesh, verticesData, triangles, section);
            FixUnusedVertices(mesh, triangles);
            collider.convex = true;
            changingMesh._colliderTriangles = triangles;
            objectTransform.position = correctTransform.position;
            objectTransform.rotation = correctTransform.rotation;
            if(_slicesCount < MaxSlicesCount)
            {
                StartCoroutine(changingMesh.PrepareForSlashing());
            }
        }
        else
        {
            mesh = obj.GetComponent<MeshFilter>().mesh;
            mesh.name = MeshName;
            SetVerticesData(mesh, verticesData, triangles, section);
            changingMesh._renderingTriangles = triangles;
        }
    }

    private void SetCorrectLists(TaskData taskData, out List<int> listForNewTwoTriangles, out List<int> listForNewOneTriangle,
                                out List<int> listToModifyListForTwoNewTriangles, out List<int> listToModifyListForNewOneTriangles,
                                bool leftHalfHasMoreVertices, bool isSection)
    {
        if(leftHalfHasMoreVertices)
        {
            if(isSection)
            {
                listForNewTwoTriangles = taskData.LeftSection;
                listForNewOneTriangle = taskData.RightSection;
                listToModifyListForTwoNewTriangles = taskData.LeftSectionToModify;
                listToModifyListForNewOneTriangles = taskData.RightSectionToModify;
            }
            else
            {
                listForNewTwoTriangles = taskData.LeftTriangles;
                listForNewOneTriangle = taskData.RightTriangles;
                listToModifyListForTwoNewTriangles = taskData.LeftTrianglesToModify;
                listToModifyListForNewOneTriangles = taskData.RightTrianglesToModify;
            }
        }
        else
        {
            if(isSection)
            {
                listForNewTwoTriangles = taskData.RightSection;
                listForNewOneTriangle = taskData.LeftSection;
                listToModifyListForTwoNewTriangles = taskData.RightSectionToModify;
                listToModifyListForNewOneTriangles = taskData.LeftSectionToModify;
            }
            else
            {
                listForNewTwoTriangles = taskData.RightTriangles;
                listForNewOneTriangle = taskData.LeftTriangles;
                listToModifyListForTwoNewTriangles = taskData.RightTrianglesToModify;
                listToModifyListForNewOneTriangles = taskData.LeftTrianglesToModify;
            }
        }
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

    private Vector2 GetUvsForVertix(Vector3 vertix, MaxMinCoordinates coordinates)
    {
        float magnitudeX = coordinates.MaxX - coordinates.MinX;
        float magnitudeY = coordinates.MaxY - coordinates.MinY;
        float magnitudeZ = coordinates.MaxZ - coordinates.MinZ;
        float firstMagnitude, secondMagnitude, firstCoordinate, secondCoordinate, minMagnitude, minCoordinate;
        (firstMagnitude, firstCoordinate, minMagnitude, minCoordinate) = (magnitudeX > magnitudeY) ? 
            (magnitudeX, vertix.x - coordinates.MinX, magnitudeY, vertix.y - coordinates.MinY) :
            (magnitudeY, vertix.y - coordinates.MinY, magnitudeX, vertix.x - coordinates.MinX);
        (secondMagnitude, secondCoordinate) = (magnitudeZ > minMagnitude) ? 
            (magnitudeZ, vertix.z - coordinates.MinZ) : (minMagnitude, minCoordinate);
        return new Vector2(firstCoordinate, secondCoordinate);
    }

    private IEnumerator PrepareForSlashing()
    {
        yield return new WaitForSeconds(TimeForNoSlashing);

        _childObject = Instantiate(this.gameObject);
        _childObject.SetActive(false);
        var childChangingMesh = _childObject.GetComponent<ChangingMesh>();
        childChangingMesh._isBase = false;
        _childObject.transform.position = ChildPosition;
        _childObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        _childMesh = _childObject.GetComponent<MeshFilter>().mesh;

        _unusedVerticesCollectingTask = new Task(() => 
        {
            DeleteUnusedVertices(_renderingVerticesData, _renderingTriangles);
            DeleteUnusedVertices(_colliderVerticesData, _colliderTriangles);
            childChangingMesh._renderingVerticesData = _renderingVerticesData.Copy();
            childChangingMesh._colliderVerticesData = _colliderVerticesData.Copy();
            _canSlash = true;
        });
        _unusedVerticesCollectingTask.Start();
    }
}
