using System.Collections.Generic;

public record TaskData
{
    public VerticesData NewVerticesData { get; } = new VerticesData();
    public Dictionary<int, int> SectionIndeces { get; } = new Dictionary<int, int>();
    public List<int> LeftTriangles { get; set; } = new List<int>();
    public List<int> RightTriangles { get; set; } = new List<int>();
    public List<int> LeftSection { get; set; } = new List<int>();
    public List<int> RightSection { get; set; } = new List<int>();
    public List<int> LeftTrianglesToModify { get; } = new List<int>();
    public List<int> RightTrianglesToModify { get; } = new List<int>();
    public List<int> LeftSectionToModify { get; } = new List<int>();
    public List<int> RightSectionToModify { get; } = new List<int>();

    public void AddToLeftHalf(bool isSection, int firstIndex, int secondIndex, int thirdIndex)
    {
        if(isSection)
        {
            LeftSection.Add(firstIndex);
            LeftSection.Add(secondIndex);
            LeftSection.Add(thirdIndex);
        }
        else
        {
            LeftTriangles.Add(firstIndex);
            LeftTriangles.Add(secondIndex);
            LeftTriangles.Add(thirdIndex);
        }
    }

    public void AddToRightHalf(bool isSection, int firstIndex, int secondIndex, int thirdIndex)
    {
        if(isSection)
        {
            RightSection.Add(firstIndex);
            RightSection.Add(secondIndex);
            RightSection.Add(thirdIndex);
        }
        else
        {
            RightTriangles.Add(firstIndex);
            RightTriangles.Add(secondIndex);
            RightTriangles.Add(thirdIndex);
        }
    }

    public void AddModifyingIndex(bool isLeft, bool isSection, int indexFromEnd)
    {
        if(isLeft)
        {
            if(isSection)
            {
                LeftSectionToModify.Add(LeftSectionToModify.Count - indexFromEnd);
            }
            else
            {
                LeftTrianglesToModify.Add(LeftTrianglesToModify.Count - indexFromEnd);
            }
        }
        else
        {
            if(isSection)
            {
                RightSectionToModify.Add(RightSectionToModify.Count - indexFromEnd);
            }
            else
            {
                RightTrianglesToModify.Add(RightTrianglesToModify.Count - indexFromEnd);
            }
        }
    }

    public void AddSectionsIndeces(int firstIndex, int secondIndex, bool firstIndexIsMain = true)
    {
        if(firstIndexIsMain)
        {
            SectionIndeces.Add(firstIndex, secondIndex);
        }
        else
        {
            SectionIndeces.Add(secondIndex, firstIndex);
        }
    }

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
