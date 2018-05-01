using UnityEngine;

public class FreeCam : MonoBehaviour
{
    public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
    public RotationAxes axes = RotationAxes.MouseXAndY;
    public float sensitivityX = 15F;
    public float sensitivityY = 15F;

    public float minimumX = -360F;
    public float maximumX = 360F;

    public float minimumY = -60F;
    public float maximumY = 60F;

    public float moveSpeed = 1.0f;

    public bool lockHeight = false;

    float rotationY = 0F;

    void Update()
    {
        if (axes == RotationAxes.MouseXAndY)
        {
            float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;

            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

            transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
        }
        else if (axes == RotationAxes.MouseX)
        {
            transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
        }
        else
        {
            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

            transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
        }

        var xAxisValue = Input.GetAxis("Horizontal");
        var zAxisValue = Input.GetAxis("Vertical");
        if (lockHeight)
        {
            var dir = transform.TransformDirection(new Vector3(xAxisValue, 0.0f, zAxisValue) * moveSpeed);
            dir.y = 0.0f;
            transform.position += dir;
        }
        else
        {
            transform.Translate(new Vector3(xAxisValue, 0.0f, zAxisValue) * moveSpeed);
        }
    }
}
