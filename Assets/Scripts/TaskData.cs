using System.Collections.Generic;

public record TaskData
{
    /*public List<int> CommonTriangles { get; }
    public VerticesData CommonVerticesData { get; }*/
    public VerticesData NewVericesData { get; } = new VerticesData();
    public Dictionary<int, int> SectionVertices { get; } = new Dictionary<int, int>();
    public List<int> LeftTriangles { get; } = new List<int>();
    public List<int> RightTriangles { get; } = new List<int>();
    public List<int> LeftSection { get; } = new List<int>();
    public List<int> RightSection { get; } = new List<int>();
    public List<int> LeftTrianglesToModify { get; } = new List<int>();
    public List<int> RightTrianglesToModify { get; } = new List<int>();
    public List<int> LeftSectionToModify { get; } = new List<int>();
    public List<int> RightSectionToModify { get; } = new List<int>();

    /*public TaskData(List<int> triangles, VerticesData verticesData)
    {
        CommonTriangles = triangles;
        CommonVerticesData = verticesData;
    }*/

    public void ModifyLists(int offset)
    {
        ModifyList(offset, LeftTriangles, LeftTrianglesToModify);
        ModifyList(offset, RightTriangles, RightTrianglesToModify);
        ModifyList(offset, LeftSection, LeftSectionToModify);
        ModifyList(offset, RightSection, RightSectionToModify);
    }

    private void ModifyList(int offset, List<int> list, List<int> listToModify)
    {
        foreach(int index in listToModify)
        {
            list[index] += offset;
        }
    }
}
