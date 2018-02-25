Shader "Custom/Circle" {
    Properties {
        _MainTex("Main Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Radius("Radius", Range(0, 1)) = 0.5
        _RadiusWidth("Thickness", float) = 0.0
        _OffsetY("Offset Y", float) = 0.0
    }

    SubShader{
        Tags { "Queue" = "Overlay" }//"Queue" = "Transparent" "AllowProjectors" = "False" }
        //Tags { /*"RenderType" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
        //ZWrite Off
        //blend One One
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
                //o.vertex = v.vertex;
                //o.uv = mul(unity_ObjectToWorld, v.vertex).xy;
                o.uv = v.uv;
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
                /*
                float offset = lerp(1, 0.5, _Radius / 1);
                float dist = length(float3(i.objPos.xyz));

                if (i.uv )

                half4 c = tex2D(_MainTex, float2(dist + offset, i.uv.y + _OffsetY));
                clip(c.a - 0.5f);
                //if (c.a == 0.0f)
                    //c = float4(0, 0, 0, 0.5);
                return c;
                */
            }

            ENDCG
        }
    }

    FallBack "Diffuse"
}

            /*
        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
	    // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
	    // #pragma instancing_options assumeuniformscaling
	    UNITY_INSTANCING_CBUFFER_START(Props)
	    // put more per-instance properties here
	    UNITY_INSTANCING_CBUFFER_END

        float4 _Color, _Distort;
        float _OffsetX, _OffsetY;

        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.objPos = v.vertex;
        }

        void surf(Input IN, inout SurfaceOutput o) {
            // Get color at normal texture coordinates.
            //half4 c = tex2D(_MainTex, IN.uv_MainTex);



            //o.Albedo = _Color.rgb * colorPt.rgb;
            //o.Alpha = (1.0f - pow(circleTest, _Hardness)) * _Color.a * colorPt.a;
        }
        */



