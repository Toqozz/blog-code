using System;
using UnityEngine;
using UnityEngine.Profiling;

public struct Frame {
    public Vector3 m_localPosition;
    public Quaternion m_localRotation;
    public Vector3 m_localScale;

    public static Frame Lerp(in Frame frame1, in Frame frame2, float t) {
        Frame result;
        result.m_localPosition = Vector3.Lerp(frame1.m_localPosition, frame2.m_localPosition, t);
        result.m_localRotation = Quaternion.Slerp(frame1.m_localRotation, frame2.m_localRotation, t);
        result.m_localScale = Vector3.Lerp(frame1.m_localScale, frame2.m_localScale, t);
        return result;
    }
}

public static class SpringUtil {
    [System.Serializable]
    public class Coefficients {
        public float posPosCoef;
        public float posVelCoef;
        public float velPosCoef;
        public float velVelCoef;
    };

    public static (float, float) StepSpring(float current, float target, float oldVel, in Coefficients co) {
        float dist = current - target;
        float pos = target + dist * co.posPosCoef + oldVel;
        float vel = dist * co.velPosCoef * co.posVelCoef + oldVel * co.velVelCoef;
        return (pos, vel);
    }

    public static (Vector3, Vector3) StepSpring(Vector3 current, Vector3 target, Vector3 oldVel, Coefficients co) {
        Vector3 p;
        Vector3 v;
        (p.x, v.x) = StepSpring(current.x, target.x, oldVel.x, co);
        (p.y, v.y) = StepSpring(current.y, target.y, oldVel.y, co);
        (p.z, v.z) = StepSpring(current.z, target.z, oldVel.z, co);
        
        return (p, v);
    }

    public static (Vector4, Vector4) StepSpring(Vector4 current, Vector4 target, Vector4 oldVel, Coefficients co) {
        Vector4 p;
        Vector4 v;
        (p.x, v.x) = StepSpring(current.x, target.x, oldVel.x, co);
        (p.y, v.y) = StepSpring(current.y, target.y, oldVel.y, co);
        (p.z, v.z) = StepSpring(current.z, target.z, oldVel.z, co);
        (p.w, v.w) = StepSpring(current.w, target.w, oldVel.w, co);
        
        return (p, v);
    }

    public static (Quaternion, Quaternion) StepSpring(Quaternion current, Quaternion target, Quaternion oldVelocity, Coefficients co) {
        // Velocity calculation.
        Quaternion fromTo = ShortestRotation(current, target);

        Quaternion vq1 = Quaternion.SlerpUnclamped(Quaternion.identity, fromTo, co.velPosCoef * co.posVelCoef);
        Quaternion vq2 = Quaternion.SlerpUnclamped(Quaternion.identity, oldVelocity, co.velVelCoef);

        // Pos calculation.
        Quaternion pq1 = Quaternion.SlerpUnclamped(Quaternion.identity, fromTo, co.posPosCoef);
        Quaternion pos =  pq1 * (oldVelocity * target);
        
        Quaternion newVelocity = vq1 * vq2;
        
        return (pos, newVelocity);
    }

    // `in` to pass these fields in by const ref rather than by value.
    public static void Step(in SpringUtil.Coefficients coefficients, in Frame target, ref Frame current, ref Frame velocity) {
        // Position
        {
            (Vector3 newPos, Vector3 newVel) = SpringUtil.StepSpring(current.m_localPosition, target.m_localPosition, velocity.m_localPosition, coefficients);
            velocity.m_localPosition = newVel;
            current.m_localPosition = newPos;
        }

        // Scale
        {
            (Vector3 newScale, Vector3 newVel) = SpringUtil.StepSpring(current.m_localScale, target.m_localScale, velocity.m_localScale, coefficients);
            velocity.m_localScale = newVel;
            current.m_localScale = newScale;
        }

        // Rotation
        {
            // New method -- the right way.
            (Quaternion newPos, Quaternion newVel) = SpringUtil.StepSpring(current.m_localRotation, target.m_localRotation, velocity.m_localRotation, coefficients);
            velocity.m_localRotation = newVel;
            current.m_localRotation = newPos;
        }
    }

    // - An angular frequency is given to control how fast the spring oscillates.
    // - A damping ratio is given to control how fast the motion decays.
    //     damping ratio > 1: over damped
    //     damping ratio = 1: critically damped
    //     damping ratio < 1: under damped
    public static Coefficients CalculateCoefficients(float timestep, float dampingRatio, float angularFreq) {
        // TODO: should this be here or outside the function?
        dampingRatio = dampingRatio < 0.0f ? 0.0f : dampingRatio;
        angularFreq = angularFreq < 0.0f ? 0.0f : angularFreq;

        SpringUtil.Coefficients coefficients = new SpringUtil.Coefficients();   // TODO: can we avoid garbage collection here?
        
        if (angularFreq < float.Epsilon) {
            coefficients.posPosCoef = 1.0f;
            coefficients.posVelCoef = 0.0f;
            coefficients.velPosCoef = 0.0f;
            coefficients.velVelCoef = 1.0f;
        } else if (dampingRatio > 1.0f + float.Epsilon) { // over-damped
            float za = -angularFreq * dampingRatio;
            float zb = angularFreq * Mathf.Sqrt(dampingRatio * dampingRatio - 1.0f);
            float z1 = za - zb;
            float z2 = za + zb;
            float e1 = Mathf.Exp(z1 * timestep);
            float e2 = Mathf.Exp(z2 * timestep);
            float inv_two_zb = 1.0f / (2.0f * zb);
            float e1_over_twozb = e1 * inv_two_zb;
            float e2_over_twozb = e2 * inv_two_zb;
            float z1e1_over_twozb = z1 * e1_over_twozb;
            float z2e2_over_twozb = z2 * e2_over_twozb;

            coefficients.posPosCoef = e1_over_twozb * z2 - z2e2_over_twozb + e2;
            coefficients.posVelCoef = -e1_over_twozb + e2_over_twozb;
            coefficients.velPosCoef = (z1e1_over_twozb - z2e2_over_twozb + e2) * z2;
            coefficients.velVelCoef = -z1e1_over_twozb + z2e2_over_twozb;
        } else if (dampingRatio < 1.0f - float.Epsilon) { // under-damped
            float omega_zeta = angularFreq * dampingRatio;
            float alpha = angularFreq * Mathf.Sqrt(1.0f - dampingRatio * dampingRatio);
            float exp_term = Mathf.Exp(-omega_zeta * timestep);
            float cos_term = Mathf.Cos(alpha * timestep);
            float sin_term = Mathf.Sin(alpha * timestep);
            float inv_alpha = 1.0f / alpha;
            float exp_sin = exp_term * sin_term;
            float exp_cos = exp_term * cos_term;
            float exp_omega_zeta_sin_over_alpha = exp_term * omega_zeta * sin_term * inv_alpha;

            coefficients.posPosCoef = exp_cos + exp_omega_zeta_sin_over_alpha;
            coefficients.posVelCoef = exp_sin * inv_alpha;
            coefficients.velPosCoef = -exp_sin * alpha - omega_zeta * exp_omega_zeta_sin_over_alpha;
            coefficients.velVelCoef = exp_cos - exp_omega_zeta_sin_over_alpha;
        } else { // critically damped
            float exp_term = Mathf.Exp(-angularFreq * timestep);
            float time_exp = timestep * exp_term;
            float time_exp_freq = time_exp * angularFreq;

            coefficients.posPosCoef = time_exp_freq + exp_term;
            coefficients.posVelCoef = time_exp;
            coefficients.velPosCoef = -angularFreq * time_exp_freq;
            coefficients.velVelCoef = -time_exp_freq + exp_term;
        }

        return coefficients;
    }

    // Math util.
    public static Quaternion ShortestRotation(Quaternion a, Quaternion b) {
        if (Quaternion.Dot(a, b) < 0.0f) {
            return a * Quaternion.Inverse(Multiply(b, -1.0f));
        } else {
            return a * Quaternion.Inverse(b);
        }
    }

    public static Quaternion Multiply(Quaternion input, float scalar) {
        return new Quaternion(input.x * scalar, input.y * scalar, input.z * scalar, input.w * scalar);
    }
}

public class SpringTransform : MonoBehaviour {
    [Header("Spring Parameters")]
    [SerializeField] private float m_maxTimestep = 0.1f; // Don't drop below 10fps.

    // Setting these will cause coefficients to be recomputed.
    [SerializeField] private float m_wantTimestep = 1.0f / 60.0f;
    [SerializeField] private float m_wantAngularFreq = 15.0f;
    [SerializeField] private float m_wantDampingRatio = 0.05f;

    // TODO: do we actually want these to be public or not? probably not right?
    // We're fairly conservative with these defaults.
    [Header("Resting Thresholds")]
    [SerializeField] private float m_restingClosenessThresholdPos = 0.001f;   // Within 1mm of target.
    [SerializeField] private float m_restingClosenessThresholdAngle = 0.001f; // 0.001 degrees.
    [SerializeField] private float m_restingVelocityThresholdPos = 0.0001f;   // Moving slower than 0.1mm/frame.
    [SerializeField] private float m_restingVelocityThresholdAngle = 0.0001f; // Rotating slower than 0.0001deg/frame -- nothing!

    [Header("Interpolation")]
    [SerializeField] private bool m_interpolate = true;

    // Real params that are used at runtime.
    private float m_timestep = 1.0f / 60.0f;
    private float m_angularFreq = 15.0f;
    private float m_dampingRatio = 0.05f;
    private SpringUtil.Coefficients m_coefficients;

    private float m_timeAccum;

    public Frame m_current;
    public Frame m_target;
    public Frame m_velocity;
    
    // Debug, read by `SpringTransformEditor.cs`.
    public Vector3 CurrentDir { get; set; }
    public Vector3 TargetDir { get; set; }
    public Vector3 VelocityAngleAxis { get; set; }

    private void Awake() {
        // Initially, make the target the same as the current transform, because we haven't moved yet.
        m_target = new Frame {
            m_localPosition = transform.localPosition,
            m_localRotation = transform.localRotation,
            m_localScale = transform.localScale
        };

        m_coefficients = SpringUtil.CalculateCoefficients(m_timestep, m_dampingRatio, m_angularFreq);
    }

    private void Update() {
        bool dirty = m_timestep != m_wantTimestep || m_angularFreq != m_wantAngularFreq || m_dampingRatio != m_wantDampingRatio;

        m_timestep = Mathf.Max(m_wantTimestep, 0.001f);
        m_angularFreq = m_wantAngularFreq;
        m_dampingRatio = m_wantDampingRatio;

        if (dirty) {
            // Parameters changed -- recalculate the coefficients.
            m_coefficients = SpringUtil.CalculateCoefficients(m_timestep, m_dampingRatio, m_angularFreq);
        }

        // Fixed timestep.
        m_timeAccum += Time.deltaTime;
        m_timeAccum = Mathf.Min(m_timeAccum, m_maxTimestep);
        while (m_timeAccum >= m_timestep) {
            SpringUtil.Step(m_coefficients, m_target, ref m_current, ref m_velocity);
            m_timeAccum -= m_timestep;
        }

        if (m_interpolate) {
            // Tentatively step once more, and interpolate the results.
            // We don't actually need to do anything to velocity here, it's just thrown away.
            Frame tmpCurrent = m_current;
            Frame _tmpVelocity = m_velocity;
            SpringUtil.Step(m_coefficients, m_target, ref tmpCurrent, ref _tmpVelocity);

            float interpolator = m_timeAccum / m_timestep;
            Frame result = Frame.Lerp(m_current, tmpCurrent, interpolator);
            transform.localPosition = result.m_localPosition;
            transform.localRotation = result.m_localRotation;
            transform.localScale = result.m_localScale;
        } else {
            transform.localPosition = m_current.m_localPosition;
            transform.localRotation = m_current.m_localRotation;
            transform.localScale = m_current.m_localScale;
        }

        // Debug
        {
            CurrentDir = transform.localRotation * Vector3.forward;
            TargetDir = m_target.m_localRotation * Vector3.forward;
            
            float angle;
            Vector3 axis;
            m_velocity.m_localRotation.ToAngleAxis(out angle, out axis);

            VelocityAngleAxis = axis * angle;
        }
    }

    // -----------------------------------------------

    private bool IsRestingPos() {
        float dist2 = Vector3.SqrMagnitude(transform.localPosition - m_target.m_localPosition);
        float vel2 = m_velocity.m_localPosition.sqrMagnitude;
        float distThreshold2 = m_restingClosenessThresholdPos * m_restingClosenessThresholdPos; // We could precompute these constants for a hair of performance if we wanted.
        float velThreshold2 = m_restingVelocityThresholdPos * m_restingVelocityThresholdPos;
        if (dist2 < distThreshold2 && vel2 < velThreshold2) {
            return true;
        }

        return false;
    }
    
    private bool IsRestingRot() {
        // TODO: we want to make sure we use unity convention, which probably means getting rid of the m_...
        float angleOrientation = Quaternion.Angle(transform.localRotation, m_target.m_localRotation);
        if (angleOrientation < m_restingClosenessThresholdAngle) {
            // For velocity, we need a more precise way of grabbing the angle, else small timesteps have problems.
            // https://discussions.unity.com/t/quaternion-toangleaxis-is-unprecise/248021/2
            Quaternion rot = m_velocity.m_localRotation;
            Vector3 axis = new Vector3(rot.x, rot.y, rot.z);
            float len = axis.magnitude;
            float angleVelocity = Mathf.Asin(len) * Mathf.Rad2Deg * 2f * Mathf.Sign(rot.w);
            Debug.Assert(angleVelocity >= 0.0f);
            if (angleVelocity < m_restingVelocityThresholdAngle) {
                return true;
            }
        }

        return false;
    }

    private bool IsRestingScale() {
        float dist2 = Vector3.SqrMagnitude(transform.localScale - m_target.m_localScale);
        float vel2 = m_velocity.m_localScale.sqrMagnitude;
        float distThreshold2 = m_restingClosenessThresholdPos * m_restingClosenessThresholdPos;
        float velThreshold2 = m_restingVelocityThresholdPos * m_restingVelocityThresholdPos;
        if (dist2 < distThreshold2 && vel2 < velThreshold2) {
            return true;
        }

        return false;
    }

    public bool IsResting() {
        return IsRestingPos() && IsRestingRot() && IsRestingScale();
    }

    public void SetTargetWorldPosRotScale(Vector3 pos, Quaternion rot, Vector3 scale) {
        SetTargetWorldPosition(pos);
        SetTargetWorldRotation(rot);
        SetTargetWorldScale(scale);
    }

    public void SetTargetLocalPosRotScale(Vector3 pos, Quaternion rot, Vector3 scale) {
        SetTargetLocalPosition(pos);
        SetTargetLocalRotation(rot);
        SetTargetLocalScale(scale);
    }

    public void SetTargetLocalPosition(Vector3 pos) {
        m_target.m_localPosition = pos;
    }

    public void SetTargetLocalRotation(Quaternion rot) {
        m_target.m_localRotation = rot;
    }
    
    public void SetTargetLocalScale(Vector3 scale) {
        m_target.m_localScale = scale;
    }

    // We should have an enum that chooses which to set here, or something.  That's more in line with the normal unity api.
    public void SetTargetWorldPosition(Vector3 pos) {
        Vector3 val;

        if (transform.parent) {
            val = transform.parent.InverseTransformPoint(pos);
        } else {
            val = pos;
        }

        SetTargetLocalPosition(val);
    }
    
    public void SetTargetWorldRotation(Quaternion rot) {
        // TODO: decide if we need to handle the case where we detach from the parent
        Quaternion val;

        if (transform.parent) {
            val = Quaternion.Inverse(transform.parent.rotation) * rot;
        } else {
            val = rot;
        }

        SetTargetLocalRotation(val);
    }
    
    public void SetTargetWorldScale(Vector3 s) {
        Vector3 val;

        if (transform.parent) {
            val = transform.parent.InverseTransformVector(s);
        } else {
            val = s;
        }

        SetTargetLocalScale(val);
    }



    // TODO: call this something else.
    public void SetTargetToCurrent() {
        m_target.m_localPosition = transform.localPosition;
        m_target.m_localRotation = transform.localRotation;
        m_target.m_localScale = transform.localScale;
    }
}
