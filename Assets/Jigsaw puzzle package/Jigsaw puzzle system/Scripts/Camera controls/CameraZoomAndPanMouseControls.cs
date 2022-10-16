using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraZoomAndPanMouseControls : MonoBehaviour
{
    public enum PanMode
    {
        LeftMouseButton = 0,
        RightMouseButton = 1
    }

    [SerializeField] float m_zoomSensitivity = 100f;
    [SerializeField] float m_maxCentreChange = 1000f;
    [SerializeField] PanMode m_panMode = PanMode.LeftMouseButton;

    private CameraManager m_cameraManager;
    private Camera m_camera;
    private Vector3 m_previousMousePosition;
    private bool m_behindUI;
    private Vector3 m_previousCentre;
    private bool m_panning;


    private void Awake()
    {
        m_camera = Camera.main;

        m_cameraManager = m_camera.GetComponent<CameraManager>();
    }


    private void Update()
    {
        if (m_panMode == PanMode.RightMouseButton)
        {
            if (Input.GetMouseButtonDown(1))
                OnMouseDown();

            if (Input.GetMouseButton(1))
            {
                m_panning = true;
                OnMouseDrag();
                m_panning = false;
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Approximately(scroll, 0f) || Input.touchCount > 1)
            return;

        float sizeMultiplier = 1f - scroll * m_zoomSensitivity * Time.unscaledDeltaTime;

        Vector3 centre = m_camera.ScreenToWorldPoint(Input.mousePosition);

        float centreChange = (m_previousCentre - centre).magnitude;
        if (centreChange > m_maxCentreChange)
            centre = Vector3.Lerp(m_previousCentre, centre, m_maxCentreChange / centreChange);

        m_cameraManager.Zoom(centre, sizeMultiplier);

        m_previousCentre = centre;
    }


    private void OnMouseDown()
    {
        m_previousMousePosition = Input.mousePosition;
    }


    private void OnMouseDrag()
    {
        if (!(m_panning || m_panMode == PanMode.LeftMouseButton))
            return;

        m_behindUI = this.IsPointerOverGameObject();

        if (m_behindUI)
            return;

        if (Input.touchCount > 1)
        {
            m_previousMousePosition = Vector3.zero;
            return;
        }

        Vector3 mousePosition = Input.mousePosition;

        if (m_previousMousePosition == Vector3.zero)
            m_previousMousePosition = mousePosition;

        Vector3 position = m_camera.ScreenToViewportPoint(m_previousMousePosition - mousePosition);
        float speed = m_camera.orthographicSize * 2;
        Vector3 move = new Vector3(position.x * speed * m_camera.aspect, 0, position.y * speed);

        m_cameraManager.Pan(move);

        m_previousMousePosition = mousePosition;
    }


    /// <summary>
    /// Hook this up to a UI slider so the player can adjust the sensitivity
    /// </summary>
    /// <param name="sensitivity"></param>
    public void SetSensitivity(float sensitivity)
    {
        m_zoomSensitivity = sensitivity;
    }


    /// <summary>
    /// Hook this up to a UI dropdown so the player can change the pan mode
    /// </summary>
    /// <param name="panMode"></param>
    public void SetPanMode(PanMode panMode)
    {
        m_panMode = panMode;
    }
}
