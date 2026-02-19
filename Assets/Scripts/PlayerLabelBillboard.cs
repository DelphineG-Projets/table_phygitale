using UnityEngine;

public class PlayerLabelBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main == null) return;

        // Face the camera
        transform.rotation = Camera.main.transform.rotation;
    }
}
