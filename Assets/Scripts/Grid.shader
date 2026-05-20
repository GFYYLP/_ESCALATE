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
                //world.y += _WorldSpaceCameraPos.y;
                
                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, world);
                o.worldPos = world.xy;
                
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.worldPos;
                float4 tint = float4(1.0, 1.0, 1.0, 1.0);
                float4 scanlineColor = float4(0, 0, 0, 0);

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
                            break;
                        }
                    case 1: //directional ripple (follow along high-velocity objects)
                        {
                            float  along    = dot(toRipple, dir);         // signed projection onto travel axis
                            float  perp     = dot(toRipple, float2(-dir.y, dir.x));  //perpendicular distance from axis
                            
                            // falloff that elongates behind the object, sharp ahead
                            float  axialFalloff = exp(-max(along, 0.0) * 1.5) *  // weak ahead
                                                  exp(-max(-along, 0.0) * 0.3) *  // long tail behind
                                                  exp(-abs(perp) * 3.0)         *  // narrow band
                                                  exp(-age * 2.0);
                            
                            // shear perpendicular to travel, dragging grid lines
                            displacement += dir * along * strength * axialFalloff * 0.3;
                            
                            //apply some perpendicular displacement too
                            //we multiply by perp here to get a stronger effect further from the center, which emphasizes the shearing
                            displacement += float2(-dir.y, dir.x) * perp * strength * axialFalloff * 0.15;
                            break;
                        }
                    case 2:  //scanline ripple
                        {
                            float2 toImpact     = uv - _Ripples[r].position;
                            float  proximity    = 1.0 - age;
                            
                            float  blockHalfWidth = 1.0;
                            float  lineWidth    = 0.04;  
                            float  scanSpacing  = 0.05; 
                            
                            float  inColumn     = step(abs(toImpact.x), blockHalfWidth);
                            float  spreadHeight = lerp(200.0, 10.0, proximity);
                            float  inSpread     = step(abs(toImpact.y), spreadHeight);
                            float  spreadT      = saturate(abs(toImpact.y) / max(spreadHeight, 0.001));  // 0 at center, 1 at edge of spread
                            
                            // which scanline index is this pixel on
                            float  scanIndex    = floor(uv.x / scanSpacing);
                            float  scanLocal    = fmod(abs(uv.x), scanSpacing);  // position within one scanline period
                            float  onScanline   = step(scanLocal, lineWidth);     // 1 if on a line, 0 if in gap
                            
                            // per-line color: same hash but now per thin-line index, not per grid column
                            float  scanHash  = frac(sin(scanIndex * 127.1) * 43758.5);
                            float  shuffled  = fmod(abs(floor(scanHash * 6.0) + scanIndex * 1.618), 6.0);

                            float3 scanColor;
                            if      (shuffled < 1.0) scanColor = float3(1.0, 0.0, 0.2);
                            else if (shuffled < 2.0) scanColor = float3(1.0, 0.3, 0.0);
                            else if (shuffled < 3.0) scanColor = float3(0.0, 1.0, 0.2);
                            else if (shuffled < 4.0) scanColor = float3(0.0, 0.8, 1.0);
                            else if (shuffled < 5.0) scanColor = float3(0.6, 0.0, 1.0);
                            else                      scanColor = float3(1.0, 0.0, 0.8);

                            float  withinSilhouette = step(abs(toImpact.x), blockHalfWidth * proximity);
                            float  scanOpacity  = proximity * (1.0 - spreadT * 0.7) 
                                                * inSpread * inColumn * onScanline
                                                * lerp(1.0, withinSilhouette, proximity);
                                                    
                            float  flicker  = frac(sin(scanIndex * 91.3 + floor(_Time.y * 24.0) * 127.1) * 43758.5);
                            float  onOff    = step(0.35, flicker);   // ~65% of lines visible at any frame, different each frame

                            scanOpacity *= onOff;

                            scanlineColor = float4(scanColor * scanOpacity, 0.0);
                            
                            break;
                        }

                    }
                    
                }

                //displace grid sampling position
                float2 gridUV = uv + displacement;

                // after accumulating displacement:
                float  dispMag    = length(displacement);
                float2 dispDir    = dispMag > 0.001 ? displacement / dispMag : float2(0, 0);

                // aberration follows displacement direction, not perpendicular to it
                float  aberrStr   = smoothstep(0.08, 0.5, dispMag) * 0.05;

                // stronger separation on the dominant axis
                float2 aberrVec   = dispDir * aberrStr;
                // extra: vertical hits get stronger vertical chromatic sep
                aberrVec.y       *= 1.5;

                float2 uvR = gridUV + aberrVec;
                float2 uvG = gridUV;
                float2 uvB = gridUV - aberrVec;

                #define GRID_LINE(guv) saturate( \
                    step(min(fmod(abs(guv), _GridSpacing), \
                             _GridSpacing - fmod(abs(guv), _GridSpacing)).x, _LineWidth) + \
                    step(min(fmod(abs(guv), _GridSpacing), \
                             _GridSpacing - fmod(abs(guv), _GridSpacing)).y, _LineWidth))

                float4 col = _BgColor * tint;
                col.r = lerp(col.r, _GridColor.r, GRID_LINE(uvR));
                col.g = lerp(col.g, _GridColor.g, GRID_LINE(uvG));
                col.b = lerp(col.b, _GridColor.b, GRID_LINE(uvB));

                return col + scanlineColor;
                
            }
            ENDHLSL
        }
    }
}