Shader "Unlit/Grid"
{
    Properties
    {
        _GridSpacing ("Grid Spacing", Float) = 1.0
        _LineWidth   ("Line Width",   Float) = 0.02
        _GridColor   ("Grid Color",   Color) = (0.2, 0.2, 0.8, 1.0)
        _BgColor     ("Background",   Color) = (0.0, 0.0, 0.05, 1.0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float  _GridSpacing;
            float  _LineWidth;
            float4 _GridColor;
            float4 _BgColor;
            
            struct Ripple
            {
                float2 position;
                float2 dir;
                float strength;
                float age;
                int type;
        
                float padding;
            };
            StructuredBuffer<Ripple> _Ripples;


            int    _RippleCount;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 worldPos : TEXCOORD0; };

            v2f vert(appdata v)
            {
                float4 world = mul(unity_ObjectToWorld, v.vertex);
                world.y += _WorldSpaceCameraPos.y;
                
                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, world);
                o.worldPos = world.xy;
                
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.worldPos;
                float4 tint = float4(1.0, 1.0, 1.0, 1.0);

                //accumulate displacement from all ripples
                float2 displacement = float2(0, 0);
                for (int r = 0; r < _RippleCount; r++)
                {
                    float2 toRipple = uv - _Ripples[r].position;
                    float2 dir = _Ripples[r].dir;
                    
                    float  dist     = length(toRipple);
                    float  age      = _Ripples[r].age;
                    float  falloff  = exp(-dist * 2.0) * exp(-age * 3.0); 
                    float  strength = _Ripples[r].strength;
                    
                    switch (_Ripples[r].type)
                    {
                    case 0: //point ripple (on objects abrupt speedup)
                        {
                            displacement   += normalize(toRipple) * strength * falloff;
                            
                            tint.xyz *= 2.0;
                            break;
                        }
                    case 1: //directional ripple (follow along high-velocity objects)
                        {
                            float  along    = dot(toRipple, dir);         // signed projection onto travel axis
                            float  perp     = dot(toRipple, float2(-dir.y, dir.x));
                            
                            // falloff that elongates behind the object, sharp ahead
                            float  axialFalloff = exp(-max(along, 0.0) * 1.5) *  // weak ahead
                                                  exp(-max(-along, 0.0) * 0.3) *  // long tail behind
                                                  exp(-abs(perp) * 3.0)         *  // narrow band
                                                  exp(-age * 2.0);
                            
                            // shear perpendicular to travel — grid lines "drag"
                            displacement += dir * along * strength * axialFalloff * 0.3;
                            displacement += float2(-dir.y, dir.x) * perp * strength * axialFalloff * 0.15;
                            break;
                        }

                    }
                    
                }

                //displace grid sampling position
                float2 gridUV = uv + displacement;

                //grid lines distance
                float2 cell     = fmod(abs(gridUV), _GridSpacing);
                float2 lineDist = min(cell, _GridSpacing - cell);
                
                float  onLine   = step(lineDist.x, _LineWidth) + 
                                  step(lineDist.y, _LineWidth);
                
                return lerp(_BgColor * tint, _GridColor, saturate(onLine)); //lerp as a binary check to avoid branching
                
                
                //TODO: apply chromatic seperation vfx on the rippling grid lines on strong ripples
                
                
            }
            ENDHLSL
        }
    }
}