using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraManager : MonoBehaviour
{
    [Tooltip("The camera size when fully zoomed in")]
    public float m_zoomSize = 0.5f;

    private Camera m_camera;
    private bool m_initialised;
    private float m_initialSize;
    private Vector3 m_initialPosition;


    public void Init()
    {
        m_camera = GetComponent<Camera>();
        m_initialSize = m_camera.orthographicSize;
        m_initialPosition = m_camera.transform.position;

        m_initialised = true;
    }


    private void Start()
    {
        if (!m_initialised)
            Init();
    }


    public void Pan(Vector3 move)
    {
        m_camera.transform.Translate(move, Space.World);

        CheckBoundsAreWithinOriginalCameraView();
    }


    public void Zoom(Vector3 centre, float sizeMultiplier)
    {
        float previousSize = m_camera.orthographicSize;
        float size = previousSize * sizeMultiplier;

        Vector3 cameraCentre = m_camera.transform.position;
        centre.y = cameraCentre.y;
        Vector3 diff = cameraCentre - centre;

        size = Mathf.Clamp(size, m_zoomSize, m_initialSize);
        m_camera.orthographicSize = size;

        sizeMultiplier = size / previousSize;
        
        m_camera.transform.position = centre + diff * sizeMultiplier;

        CheckBoundsAreWithinOriginalCameraView();
    }


    private void CheckBoundsAreWithinOriginalCameraView()
    {
        var offset = Vector3.zero;

        float cameraHalfHeight = m_camera.orthographicSize;
        float cameraHalfWidth = cameraHalfHeight * m_camera.aspect;

        var cameraCentre = m_camera.transform.position;

        var cameraMin = new Vector3(cameraCentre.x - cameraHalfWidth, 0, cameraCentre.z - cameraHalfHeight);
        var cameraMax = new Vector3(cameraCentre.x + cameraHalfWidth, 0, cameraCentre.z + cameraHalfHeight);

        var initialBounds = new Bounds(m_initialPosition, new Vector3(m_camera.aspect * m_initialSize * 2, 0f, m_initialSize * 2));

        if (cameraMin.x < initialBounds.min.x)
            offset.x = initialBounds.min.x - cameraMin.x;
        else if (cameraMax.x > initialBounds.max.x)
            offset.x = initialBounds.max.x - cameraMax.x;

        if (cameraMin.z < initialBounds.min.z)
            offset.z = initialBounds.min.z - cameraMin.z;
        else if (cameraMax.z > initialBounds.max.z)
            offset.z = initialBounds.max.z - cameraMax.z;

        transform.position = cameraCentre + offset;
    }
}
