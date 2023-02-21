using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool 
{
    [SerializeField] private Vector3 FirstObjectPosition = new Vector3(0, -2, 0);
    [SerializeField] private int ObjectsCount = 3;
    [SerializeField] private float ObjectsOffset = 2;
    private GameObject _prefab;
    private List<GameObject> _objects;
    private int _firstUnusedObjectIndex = 0;

    public ObjectPool(GameObject prefab)
    {
        _prefab = prefab;
        _objects = new List<GameObject>();
        for(int i = 0; i < ObjectsCount; ++i)
        {
            var newObject = GameObject.Instantiate(prefab);
            var rigidbody = newObject.GetComponent<Rigidbody>();
            if(rigidbody != null)
            {
                rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            }
            newObject.transform.position = new Vector3(FirstObjectPosition.x + ObjectsOffset * i,
                FirstObjectPosition.y, FirstObjectPosition.z);
            _objects.Add(newObject);
        }
    }

    public GameObject GetNewObject()
    {
        if(_firstUnusedObjectIndex >= _objects.Count)
        {
            return null;
        }
        return _objects[_firstUnusedObjectIndex++];
    }
}
