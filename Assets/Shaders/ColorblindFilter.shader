Shader "Hidden/ColorblindFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vp (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            fixed4 fp (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float r = col.r;
                float g = col.g;
                float b = col.b;

                // Matriz de daltonización (Protanopia/Deuteranopia - Corrección de tonos rojo/verde)
                float r_corr = 0.56667 * r + 0.43333 * g;
                float g_corr = 0.55833 * r + 0.44167 * g;
                float b_corr = 0.24167 * g + 0.75833 * b;

                // Diferencia de error cromático
                float err_r = r - r_corr;
                float err_g = g - g_corr;
                float err_b = b - b_corr;

                // Desplazamiento a canales visibles (amarillos/azules de alto contraste)
                float r_final = r_corr;
                float g_final = g_corr + 0.7 * err_r + 0.3 * err_g;
                float b_final = b_corr + 0.7 * err_r + 0.3 * err_b;

                return fixed4(saturate(r_final), saturate(g_final), saturate(b_final), col.a);
            }
            ENDCG
        }
    }
}
