#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpringTransform))]
public class SpringTransformEditor : Editor {
    private void OnSceneGUI() {
        SpringTransform st = target as SpringTransform;
        if (st.CurrentDir == Vector3.zero || st.TargetDir == Vector3.zero) {
            //return;
        }

        Color arcCol = new Color(0.98f, 0.95f, 0.78f, 0.2f);
        Color currentCol = new Color(0.63f, 0.96f, 0.34f, 1f);
        Color targetCol = new Color(0.67f, 0.24f, 0.95f, 1f);
        Color velocityCol = new Color(0.88f, 0.37f, 0.24f, 1f);
        Color blue = new Color(69.0f/255.0f, 133.0f/255.0f, 136.0f/255.0f, 1f);
        Vector3 curPos = st.transform.position;
        
        Handles.color = currentCol;
        Handles.DrawLine(curPos, curPos + st.CurrentDir * 2.5f, 5.0f);

        float angle = Vector3.Angle(st.CurrentDir, st.TargetDir);
        Vector3 normal = Vector3.Cross(st.CurrentDir, st.TargetDir);
        Handles.DrawWireArc(curPos, normal, curPos + st.CurrentDir * 2.0f, angle, 1.5f, 1.0f);
        
        Handles.color = targetCol;
        Handles.DrawLine(curPos, curPos + st.TargetDir * 2.5f, 5.0f);

        Handles.color = velocityCol;
        Vector3 velocityAxis = st.VelocityAngleAxis.normalized;
        float velocityAngle = st.VelocityAngleAxis.magnitude;
        Vector3 angleDir = Vector3.ProjectOnPlane(st.CurrentDir, velocityAxis);
        angleDir.Normalize();
        
        Handles.DrawDottedLine(curPos, curPos + velocityAxis * 3.0f, 3.0f);
        Handles.DrawWireDisc(curPos + velocityAxis * 3.0f, velocityAxis, 0.5f, 1.0f);
        Handles.DrawSolidArc(curPos + velocityAxis * 3.0f, velocityAxis, curPos + angleDir, velocityAngle, 0.5f);

        Handles.DrawGizmos(Camera.main);
    }
}

#endif
