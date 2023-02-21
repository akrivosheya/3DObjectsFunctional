using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Miner : MonoBehaviour
{
    [SerializeField] private float minVelocityToMine = 1;
    private Rigidbody _rigidbody;
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if(_rigidbody.velocity.magnitude < minVelocityToMine)
        {
            return;
        }
        var deposit = collision.gameObject.GetComponent<Deposit>();
        if(deposit != null)
        {
            deposit.DropNewMineral();
        }
    }
}
