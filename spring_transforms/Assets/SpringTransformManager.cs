// 
// This file is left here as an example of a data oriented approach.
//
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//
//public class SpringTransformsBuffer {
//    public List<Vector3> m_velocityPos = new List<Vector3>();
//    public List<Quaternion> m_velocityRot = new List<Quaternion>();
//    public List<Vector3> m_velocityScale = new List<Vector3>();
//
//    public List<Vector3> m_targetPosition = new List<Vector3>();
//    public List<Quaternion> m_targetRotation = new List<Quaternion>();
//    public List<Vector3> m_targetScale = new List<Vector3>();
//
//    public List<Vector3> m_localPosition = new List<Vector3>();
//    public List<Quaternion> m_localRotation = new List<Quaternion>();
//    public List<Vector3> m_localScale = new List<Vector3>();
//
//    public List<SpringUtil.Coefficients> m_coefficients = new List<SpringUtil.Coefficients>();
//
//    public List<Transform> m_transform = new List<Transform>();
//
//    public int Count => m_localPosition.Count;
//
//    // TODO: need some removal?
//    public int Add(SpringUtil.Coefficients coefficients, Transform transform) {
//        int idx = Count; // The index that this ST will occupy.
//
//        m_localPosition.Add(transform.localPosition);
//        m_localRotation.Add(transform.localRotation);
//        m_localScale.Add(transform.localScale);
//
//        m_targetPosition.Add(transform.localPosition);
//        m_targetRotation.Add(transform.localRotation);
//        m_targetScale.Add(transform.localScale);
//
//        m_velocityPos.Add(Vector3.zero);
//        m_velocityRot.Add(Quaternion.identity);
//        m_velocityScale.Add(Vector3.zero);
//
//        m_coefficients.Add(coefficients);
//
//        m_transform.Add(transform);
//
//        return idx;
//    }
//
//    public void SetPosition(int handle, Vector3 val) {
//        m_targetPosition[handle] = val;
//    }
//
//    public void SetRotation(int handle, Quaternion val) {
//        m_targetRotation[handle] = val;
//    }
//
//    public void SetScale(int handle, Vector3 val) {
//        m_targetScale[handle] = val;
//    }
//
//    private bool IsRestingPos(int handle) {
//        float dist2 = Vector3.SqrMagnitude(transform.localPosition - m_targetTransform.m_localPosition);
//        float vel2 = m_velocityPos.sqrMagnitude;
//        float distThreshold2 = m_restingClosenessThresholdPos * m_restingClosenessThresholdPos; // TODO: have precomputed constants?
//        float velThreshold2 = m_restingVelocityThresholdPos * m_restingVelocityThresholdPos;
//        if (dist2 < distThreshold2 && vel2 < velThreshold2) {
//            return true;
//        }
//
//        return false;
//    }
//    
//    private bool IsRestingRot(int handle) {
//        float angleOrientation = Quaternion.Angle(transform.localRotation, m_targetTransform.m_localRotation);
//        if (angleOrientation < m_restingClosenessThresholdAngle) {
//            // For velocity, we need a more precise way of grabbing the angle, else small timesteps have problems.
//            // https://discussions.unity.com/t/quaternion-toangleaxis-is-unprecise/248021/2
//            Vector3 axis = new Vector3(m_velocityRot.x, m_velocityRot.y, m_velocityRot.z);
//            float len = axis.magnitude;
//            float angleVelocity = Mathf.Asin(len) * Mathf.Rad2Deg * 2f * Mathf.Sign(m_velocityRot.w);
//            Debug.Assert(angleVelocity >= 0.0f);
//            if (angleVelocity < m_restingVelocityThresholdAngle) {
//                return true;
//            }
//        }
//
//        return false;
//    }
//
//    private bool IsRestingScale(int handle) {
//        float dist2 = Vector3.SqrMagnitude(transform.localScale - m_targetTransform.m_localScale);
//        float vel2 = m_velocityScale.sqrMagnitude;
//        float distThreshold2 = m_restingClosenessThresholdPos * m_restingClosenessThresholdPos;
//        float velThreshold2 = m_restingVelocityThresholdPos * m_restingVelocityThresholdPos;
//        if (dist2 < distThreshold2 && vel2 < velThreshold2) {
//            return true;
//        }
//
//        return false;
//    }
//
//    // TODO: want to assign target pos to end position when finished.
//    public bool IsResting(int handle) {
//        return IsRestingPos(handle) && IsRestingRot(handle) && IsRestingScale(handle);
//    }
//
//
//}
//
//public class SpringTransformManager : MonoBehaviour {
//    [Header("Spring Parameters")]
//    [SerializeField]
//    public float m_maxTimestep = 0.01f;
//
//    [SerializeField]
//    private float m_wantTimestep = 0.01f; // 0.008
//
//    // We're fairly conservative with these defaults.
//    [Header("Resting Thresholds")]
//    public float m_restingClosenessThresholdPos = 0.001f;   // Within 1mm of target.
//    public float m_restingClosenessThresholdAngle = 0.001f; // 0.001 degrees.
//    public float m_restingVelocityThresholdPos = 0.0001f;   // Moving slower than 0.1mm/frame.
//    public float m_restingVelocityThresholdAngle = 0.0001f; // Rotating slower than 0.0001deg/frame -- nothing!
//
//    private float m_timestep = 0.01f;
//    private float m_timeAccum;
//
//    private SpringTransformsBuffer m_springTransforms = new SpringTransformsBuffer();
//
//    public int RegisterSpring(float dampingRatio, float angularFreq, Transform initial) {
//        SpringUtil.Coefficients coef = SpringUtil.CalculateCoefficients(m_timestep, dampingRatio, angularFreq);
//        return m_springTransforms.Add(coef, initial);
//    }
//
//    public void SetPosition(int handle, Vector3 val) {
//        m_springTransforms.SetPosition(handle, val);
//    }
//
//    public void SetRotation(int handle, Quaternion val) {
//        m_springTransforms.SetRotation(handle, val);
//    }
//
//    public void SetScale(int handle, Vector3 val) {
//        m_springTransforms.SetScale(handle, val);
//    }
//
//    private void Update() {
//        m_timestep = m_wantTimestep;
//
//        /*
//        if (dirty) {
//            // Parameters changed -- recalculate the coefficients.
//            m_coefficients = SpringUtil.CalculateCoefficients(m_timestep, m_dampingRatio, m_angularFreq);
//        }
//    */
//
//        // Fixed timestep.
//        m_timeAccum += Time.deltaTime;
//        m_timeAccum = Mathf.Min(m_timeAccum, m_timestep);
//        while (m_timeAccum >= m_timestep) {
//            Step();
//            m_timeAccum -= m_timestep;
//        }
//    }
//
//    private void Step() {
//        UnityEngine.Profiling.Profiler.BeginSample("Step");
//
//        UnityEngine.Profiling.Profiler.BeginSample("Position");
//        for (int i = 0; i < m_springTransforms.Count; i++) {
//            Vector3 curPos = m_springTransforms.m_localPosition[i];
//            Vector3 targetPos = m_springTransforms.m_targetPosition[i];
//            Vector3 velocity = m_springTransforms.m_velocityPos[i];
//            SpringUtil.Coefficients coef = m_springTransforms.m_coefficients[i];
//
//            (Vector3 newPos, Vector3 newVel) = SpringUtil.StepSpring(curPos, targetPos, velocity, coef);
//            m_springTransforms.m_localPosition[i] = newPos;
//            m_springTransforms.m_velocityPos[i] = newVel;
//        }
//        UnityEngine.Profiling.Profiler.EndSample();
//
//        UnityEngine.Profiling.Profiler.BeginSample("Rotation");
//        for (int i = 0; i < m_springTransforms.Count; i++) {
//            Quaternion curRot = m_springTransforms.m_localRotation[i];
//            Quaternion targetRot = m_springTransforms.m_targetRotation[i];
//            Quaternion velocity = m_springTransforms.m_velocityRot[i];
//            SpringUtil.Coefficients coef = m_springTransforms.m_coefficients[i];
//
//            (Quaternion newRot, Quaternion newVel) = SpringUtil.StepSpring(curRot, targetRot, velocity, coef);
//            m_springTransforms.m_localRotation[i] = newRot;
//            m_springTransforms.m_velocityRot[i] = newVel;
//        }
//        UnityEngine.Profiling.Profiler.EndSample();
//
//        UnityEngine.Profiling.Profiler.BeginSample("Scale");
//        for (int i = 0; i < m_springTransforms.Count; i++) {
//            Vector3 curScale = m_springTransforms.m_localScale[i];
//            Vector3 targetScale = m_springTransforms.m_targetScale[i];
//            Vector3 velocity = m_springTransforms.m_velocityScale[i];
//            SpringUtil.Coefficients coef = m_springTransforms.m_coefficients[i];
//
//            (Vector3 newScale, Vector3 newVel) = SpringUtil.StepSpring(curScale, targetScale, velocity, coef);
//            m_springTransforms.m_localScale[i] = newScale;
//            m_springTransforms.m_velocityScale[i] = newVel;
//        }
//        UnityEngine.Profiling.Profiler.EndSample();
//
//        // Assign all the transforms.
//        UnityEngine.Profiling.Profiler.BeginSample("Assign");
//        for (int i = 0; i < m_springTransforms.Count; i++) {
//            Vector3 curPos = m_springTransforms.m_localPosition[i];
//            Quaternion curRot = m_springTransforms.m_localRotation[i];
//            Vector3 curScale = m_springTransforms.m_localScale[i];
//
//            m_springTransforms.m_transform[i].SetLocalPositionAndRotation(curPos, curRot);
//            m_springTransforms.m_transform[i].localScale = curScale;
//        }
//        UnityEngine.Profiling.Profiler.EndSample();
//
//        UnityEngine.Profiling.Profiler.EndSample();
//    }
//}
