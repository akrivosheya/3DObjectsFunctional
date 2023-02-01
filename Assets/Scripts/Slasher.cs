using UnityEngine;

public class Slasher : MonoBehaviour
{
    [SerializeField] private Vector3 MainAxis = Vector3.up;
    [SerializeField] private float minVelocityToSlash = 1;
    private Rigidbody _rigidbody;
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if(_rigidbody.velocity.magnitude < minVelocityToSlash)
        {
            return;
        }
        var changingMesh = collision.gameObject.GetComponent<ChangingMesh>();
        if(changingMesh != null)
        {
            var otherTransform = collision.transform;
            var point = otherTransform.InverseTransformPoint(collision.contacts[0].point);
            var velocityAxis = transform.InverseTransformDirection(_rigidbody.velocity);
            var normal = otherTransform.InverseTransformDirection(transform.TransformDirection(Vector3.Cross(MainAxis, velocityAxis))).normalized;
            float D = -(normal.x * point.x) - (normal.y * point.y) - (normal.z * point.z);
            changingMesh.SliceByPlane(normal.x, normal.y, normal.z, D);
        }
    }
}
