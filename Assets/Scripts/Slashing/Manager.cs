using UnityEngine;

public class Manager : MonoBehaviour
{
    public static bool IsSecond { get; set; }
    [SerializeField] ChangingMesh obj2;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            //obj2.SliceByPlane();
        }
    }
}
