using UnityEngine;
using System.Collections;

public class ObjectController : MonoBehaviour {

    private GameObject _selectedObject;
    private Vector3 _lastMouse;

    private float scaleParameter;
    private float rotationParameter;
    private float translationVelocity;

    // Use this for initialization
    void Start () {
        scaleParameter = 1.1f;
        rotationParameter = 5;
        translationVelocity = 0.1f;
    }
	
	// Update is called once per frame
	void Update () {

        raycastMouse();

        Vector3 actualMouse = Input.mousePosition;
        Vector3 mouseMovementDirection;
        mouseMovementDirection.x = (actualMouse.x > _lastMouse.x) ? 1 : ((actualMouse.x < _lastMouse.x) ? -1 : 0);
        mouseMovementDirection.y = (actualMouse.y > _lastMouse.y) ? 1 : ((actualMouse.y < _lastMouse.y) ? -1 : 0);
        mouseMovementDirection.z = (actualMouse.z > _lastMouse.z) ? 1 : ((actualMouse.z < _lastMouse.z) ? -1 : 0);


        if (_selectedObject)
        {

            if (Input.GetKey(KeyCode.Mouse0))
            {
                Vector3 rotation;
                rotation.x = rotationParameter * mouseMovementDirection.x;
                rotation.y = rotationParameter * mouseMovementDirection.y;
                rotation.z = rotationParameter * mouseMovementDirection.z;
                _selectedObject.transform.Rotate(rotation);
            }

            if (Input.GetKey(KeyCode.Mouse1))
            {
                Vector3 translation = new Vector3(0, 0, 0);
                translation.x += (actualMouse.x - _lastMouse.x)* Time.deltaTime;//translationVelocity * mouseMovementDirection.x;
                translation.y += (actualMouse.y - _lastMouse.y)* Time.deltaTime;//translationVelocity * mouseMovementDirection.y;
                translation.z = 0;
                _selectedObject.transform.Translate(translation, Space.World);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if(scroll > 0)
            {
                Vector3 scale = _selectedObject.transform.localScale;
                scale *= scaleParameter;
                _selectedObject.transform.localScale = scale;

            }
            else if(scroll < 0)
            {
                Vector3 scale = _selectedObject.transform.localScale;
                scale *= 1/scaleParameter;
                _selectedObject.transform.localScale = scale;

            }

        }

        _lastMouse = actualMouse;
    }

    void raycastMouse()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100))
                _selectedObject = hit.transform.gameObject;
        }
    }
}
