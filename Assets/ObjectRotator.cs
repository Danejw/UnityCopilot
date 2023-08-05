
using UnityEngine;

public class ObjectRotator : MonoBehaviour
{
    public float rotationSpeed = 10f; // Speed at which the object rotates

    void Update()
    {
        // Rotate the object around its Y-axis based on the rotation speed and deltaTime
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
