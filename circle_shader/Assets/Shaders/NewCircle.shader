Shader "Custom/NewCircle" {
    Properties {
        _MainTex("Main Texture", 2D) = "white" {}
        _OffsetY("Offset Y", float) = 0.0
    }

    SubShader{
        Tags { "Queue" = "Overlay" }
        Pass {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 pos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float _Radius, _OffsetY;
            sampler2D _MainTex;

            half4 frag(v2f i) : SV_Target{
                float offset = lerp(1, 0.5, _Radius / 1);
                float dist = length(float3(i.pos.xyz));

                half4 c = tex2D(_MainTex, float2(dist + offset, i.uv.y + _OffsetY));
                clip(c.a - 0.5f);
                return c;
            }

            ENDCG
        }
    }

    FallBack "Diffuse"
}