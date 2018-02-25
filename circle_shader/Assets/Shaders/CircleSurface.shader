Shader "Custom/Circle Surf" {
    Properties{
        _MainTex("Main Texture", 2D) = "white" {}
    _Color("Color", Color) = (1,1,1,1)
        _Distort("Distort", vector) = (0.5, 0.5, 1.0, 1.0)
        _OuterRadius("Outer Radius", float) = 0.5
        _InnerRadius("Inner Radius", float) = -0.5
        _Hardness("Hardness", float) = 1.0
        _OffsetX("offsetx", float) = 0.0
        _OffsetY("offsetx", float) = 0.0
    }

        SubShader{
        Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" "AllowProjectors" = "False" }

        blend SrcAlpha OneMinusSrcAlpha
        //blend One One

        CGPROGRAM
#pragma surface surf NoLighting vertex:vert

        fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
    {
        return fixed4(s.Albedo, s.Alpha);
    }

    sampler2D _MainTex;
    float _OffsetX;
    float _OffsetY;

    struct Input
    {
        float2 uv_MainTex;
        float3 localPos;
    };

    float4 _Color, _Distort;
    float _OuterRadius, _InnerRadius, _Hardness;

    void vert(inout appdata_full v, out Input o) {
        UNITY_INITIALIZE_OUTPUT(Input, o)
        o.localPos = v.vertex.xyz;
    }

    void surf(Input IN, inout SurfaceOutput o)
    {
       float dist = length(float3(IN.localPos.xyz));

       half4 c = tex2D(_MainTex, float2(dist + _OffsetX, IN.uv_MainTex.y + _OffsetY));
        o.Albedo = c.rgb;
        if (c.a == 0.0f)
            o.Albedo = float3(0, 0, 0);
        o.Alpha = c.a;
    }
    ENDCG
    }
        FallBack "Diffuse"
}