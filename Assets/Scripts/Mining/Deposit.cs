using System.Collections;
using UnityEngine;

public class Deposit : MonoBehaviour
{
    [SerializeField] private GameObject MineralPrefab;
    [SerializeField] private Vector3 MineralOffset;
    [SerializeField] private Vector3 NextMineralPosition;
    [SerializeField] private int SecondsBeforeInstantiating = 1;
    private GameObject _nextMineral;
    private Rigidbody _rigidbody;
    private Vector3 _minedMineralPosition;
    private ObjectPool _objectPool;
    private bool _canDrop;

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _minedMineralPosition = transform.position + MineralOffset;
        var _objectPool = new ObjectPool(MineralPrefab);
        StartCoroutine(InstantiateNextMineral());
    }

    void Update()
    {
    }

    public void DropNewMineral()
    {
        if(_canDrop)
        {
            _canDrop = false;
            _nextMineral.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            _nextMineral.transform.position = _minedMineralPosition;
            StartCoroutine(InstantiateNextMineral());
        }
    }

    private IEnumerator InstantiateNextMineral()
    {
        yield return new WaitForSeconds(SecondsBeforeInstantiating);

        _nextMineral = _objectPool.GetNewObject();//Instantiate(MineralPrefab);
        if(_nextMineral == null)
        {
            yield break;
        }
        _nextMineral.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        _nextMineral.transform.position = NextMineralPosition;
        _canDrop = true;
    }
}
