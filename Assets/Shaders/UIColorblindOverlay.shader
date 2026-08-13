Shader "UI/ColorblindOverlay"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Overlay+1000"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always

        GrabPass
        {
            "_ColorblindGrabTex"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float4 grabPos  : TEXCOORD0;
            };

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.color = IN.color;
                OUT.grabPos = ComputeGrabScreenPos(OUT.vertex);
                return OUT;
            }

            sampler2D _ColorblindGrabTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 col = tex2Dproj(_ColorblindGrabTex, UNITY_PROJ_COORD(IN.grabPos));
                float r = col.r;
                float g = col.g;
                float b = col.b;

                // Matriz de daltonización Protanopia/Deuteranopia
                float r_corr = 0.56667 * r + 0.43333 * g;
                float g_corr = 0.55833 * r + 0.44167 * g;
                float b_corr = 0.24167 * g + 0.75833 * b;

                float err_r = r - r_corr;
                float err_g = g - g_corr;

                float r_final = r_corr;
                float g_final = g_corr + 0.7 * err_r + 0.3 * err_g;
                float b_final = b_corr + 0.7 * err_r;

                return fixed4(saturate(r_final), saturate(g_final), saturate(b_final), 1.0);
            }
            ENDCG
        }
    }
}
