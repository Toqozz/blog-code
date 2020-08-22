Shader "Custom/LineShader" {
    Properties {
        _Color("Color", Color) = (1, 1, 1, 1)
        _Width("Width", float) = 0.05
    }
    
    SubShader {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+1" }

        Pass {
            ZTest Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            
            #include "UnityCG.cginc"
            
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            static const uint MAX_NODE_COUNT = 256;
            static const uint VERTICES_PER_NODE = 8;
            
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                uint id : SV_VertexID;
            };
            
            struct v2f {
                float4 clipPos : SV_POSITION;
                float2 uv : TEXCOORD0;
                int endCap : TEXCOORD1;
                int rendered : TEXCOORD2;
            };

            float4 _Points[MAX_NODE_COUNT];
            float _Width;
            float4 _Color;

            v2f vert (appdata v) {
                v2f o;
                o.uv = float2(0, 0);
                o.rendered = 1;
                
                // The node that the current vertex is associated with.
                int idx = v.id / VERTICES_PER_NODE;
                // The next node, clamped to the max number of nodes.
                int next_idx = min(MAX_NODE_COUNT-1, idx+1);
                
                float4 p1 = _Points[idx];
                float4 p2 = _Points[next_idx];
                
                int id = v.id % VERTICES_PER_NODE;
                

                // If id >= 4, then this vertex relates to an endcap.
                o.endCap = step(4, id);
                
                // w of 0 means the node shouldn't be rendered.
                // If the first node should be rendered, but the second shouldn't, then we still
                // want to render the start cap.
                if (p1.w == 0 || (p2.w == 0 && id < 4)) {
                    o.clipPos = 0;
                    o.rendered = 0;
                    return o;
                }
                
                float thick1 = _Width / p1.z;
                float thick2 = _Width / p2.z;
                
                float2 dir = normalize(p2.xy - p1.xy);
                float2 perp = float2(-dir.y, dir.x);   // Counter-clockwise perpendicular to dir.
                
                // Currently, each line segment has 8 vertices -- 4 for the line and 4 for the start cap.
                // Comments are written as though p1 -> p2 is going left to right.
                float2 pos;
                if (id == 0) {          // Vertex 1, v1 top.
                    pos = p1.xy + perp * thick1;
                } else if (id == 1) {   // Vertex 2, v2 bottom.
                    pos = p2.xy - perp * thick2;
                } else if (id == 2) {   // Vertex 3, v1 bottom.
                    pos = p1.xy - perp * thick1;
                } else if (id == 3) {   // Vertex 4, v2 top.
                    pos = p2.xy + perp * thick2;
                    
                } else if (id == 4) {   // Vertex 5, cap tl.
                    pos = p1.xy + float2(-thick1, thick1);
                    o.uv = float2(0, 1);
                } else if (id == 5) {   // Vertex 6, cap tr.
                    pos = p1.xy + float2(thick1, thick1);
                    o.uv = float2(1, 1);
                } else if (id == 6) {   // Vertex 7, cap bl.
                    pos = p1.xy + float2(-thick1, -thick1);
                    o.uv = float2(0, 0);
                } else if (id == 7) {   // Vertex 8, cap br.
                    pos = p1.xy + float2(thick1, -thick1);
                    o.uv = float2(1, 0);
                }
                
                o.clipPos = mul(UNITY_MATRIX_VP, float4(pos, 0, 1));
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float4 col;
                // Could replace this `if` easily with lerp, but this is more readable for now.
                if (i.endCap) {
                    float dist = distance(i.uv, float2(0.5, 0.5));
                    col = _Color;
                    col.a = step(0.5, 1.0 - dist);
                } else {
                    col = _Color;
                }
                
                return col;
            }
            
            ENDCG
        }
    }
}
