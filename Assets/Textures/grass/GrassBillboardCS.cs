using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        // Всегда смотрит на камеру — как в PS1
        transform.LookAt(transform.position + cam.forward);
    }
}