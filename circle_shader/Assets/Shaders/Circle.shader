Shader "Custom/Circle" {
    Properties {
        _Color("Color", Color) = (1,1,1,1)
        _Radius("Radius", Range(0, 1)) = 0.5
        _RadiusWidth("Thickness", float) = 0.0
    }

    SubShader{
        Tags { "Queue" = "Overlay" }
        Pass {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float _Radius, _RadiusWidth;
            half4 _Color;

            half4 frag(v2f i) : SV_Target{
                half4 c = _Color;

                float d = distance(float4(0,0,0,1), i.pos);
                float r = lerp(0, 0.5 - _RadiusWidth, _Radius / 1);
                if (d > r && d < r + _RadiusWidth) {
                    c.a = 1;
                } else {
                    c.a = 0;
                }

                clip(c.a - 0.5f);
                return c;
            }

            ENDCG
        }
    }

    FallBack "Diffuse"
}