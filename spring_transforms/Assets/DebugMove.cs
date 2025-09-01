using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugMove : MonoBehaviour {
    public float Speed;
    
    private SpringTransform m_st;
    private Vector2 m_prevMousePos;

    private Vector3 m_currentDir;

    private void Awake() {
        m_st = GetComponent<SpringTransform>();
        m_currentDir = -transform.forward;
    }
    
    private void Update() {
        Vector2 mouseDelta = m_prevMousePos - (Vector2)Input.mousePosition;
        m_prevMousePos = Input.mousePosition;
        
        if (Input.GetMouseButton(0)) {
            if (mouseDelta.magnitude > 0.0f) {
                Vector3 cross = Vector3.Cross(Vector3.forward, mouseDelta);
                cross = Quaternion.Inverse(transform.rotation) * cross;
                Quaternion rot = Quaternion.AngleAxis(mouseDelta.magnitude * Time.deltaTime * Speed, cross);
                Quaternion newRot = m_st.m_target.m_localRotation * rot;
                m_st.SetTargetLocalRotation(newRot);
            }
        }
        //Debug.DrawLine(transform.position, transform.position + m_currentDir * 5.0f, Color.blue);

        if (Input.GetMouseButtonUp(0)) {
            m_st.SetTargetLocalRotation(Quaternion.identity);
        }
        
        if (Input.GetMouseButton(1)) {
            if (mouseDelta.magnitude > 0.0f) {
                if (Input.GetKey(KeyCode.LeftControl)) {
                    Vector3 scale = m_st.m_target.m_localScale;
                    scale -= new Vector3(mouseDelta.x, mouseDelta.y, 0.0f) * Time.deltaTime * Speed * 0.1f;
                    m_st.SetTargetLocalScale(scale);
                } else {
                    Vector3 pos = m_st.m_target.m_localPosition;
                    pos -= new Vector3(mouseDelta.x, mouseDelta.y, 0.0f) * Time.deltaTime * Speed * 0.1f;
                    m_st.SetTargetLocalPosition(pos);
                }
            }
        }

        if (Input.GetMouseButtonUp(1)) {
            m_st.SetTargetLocalPosition(Vector3.zero);
            m_st.SetTargetLocalScale(Vector3.one);
        }

        if (false && Input.GetKey(KeyCode.LeftShift)) {
            Vector3 pos = (Vector3)Input.mousePosition;
            pos.z = 10.0f;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(pos);
            m_st.SetTargetWorldPosition(worldPos);
        }
        
        //Debug.DrawLine(transform.position, transform.position + transform.forward * 3, Color.green);
        //Debug.DrawLine( m_st.targetTransform.position, m_st.targetTransform.position + (m_st.targetTransform.rotation * Vector3.forward), Color.red);
    }
}
