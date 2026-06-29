Shader "Unlit/StaticNoise"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float _SystemStability;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float4 color = tex2D(_MainTex, uv);
                float screenHeight = 200.0;

                //non uniform horizontal bands
                float  bandY        = floor(uv.y * screenHeight / 1.0);  // 1px bands
                float  bandHash     = frac(sin(bandY * 127.1 + floor(_Time.y * 8.0) * 311.7) * 43758.5);
                
                //horizontal shift + luminance spike within a dropout ban
                float  shiftAmount  = (frac(sin(bandY * 91.3) * 29183.1) * 2.0 - 1.0)
                                    * _SystemStability * 0.02;  // max 2% screen width shift
                float2 shiftedUV    = uv + float2(shiftAmount, 0.0);
                float3 shiftedColor = tex2D(_MainTex, shiftedUV).rgb;//screenTex.Sample(s, shiftedUV).rgb;

                //dropout bands briefly overbright then dark for signal spiking
                float  dropoutT     = step(lerp(0.95, 0.3, _SystemStability), bandHash);  //some bands are heavily affected, others clean
                float  lumaMod      = lerp(1.0, bandHash > 0.5 ? 1.4 : 0.3, dropoutT * _SystemStability);

                color.rgb           = lerp(color.rgb, shiftedColor * lumaMod, dropoutT);
                
                return color;
            }
            ENDCG
        }
    }
}
