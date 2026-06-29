Shader "Unlit/Grid"
{
    Properties
    {
        _GridSpacing ("Grid Spacing", Float) = 1.0
        _LineWidth   ("Line Width",   Float) = 0.02
        _GridColor   ("Grid Color",   Color) = (0.2, 0.2, 0.8, 1.0)
        _BgColor     ("Background",   Color) = (0.0, 0.0, 0.05, 1.0)
        _CorruptScore ("Corrupt Score", Float) = 1.0
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
            float _CorruptScore;
            
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
                float4 whiteBloom = float4(0, 0, 0, 0);
                float  pixelate   = 0.0;

                //accumulate displacement from all ripples
                float2 displacement = float2(0, 0);
                for (int r = 0; r < _RippleCount; r++)
                {
                    float2 toRipple = uv - _Ripples[r].position;
                    float2 dir = _Ripples[r].dir;
                    
                    float  dist     = length(toRipple);
                    float  age      = _Ripples[r].age;
                    float  strength = _Ripples[r].strength;
                    
                    switch (_Ripples[r].type)
                    {
                    case 0: //point ripple (on objects' abrupt speedup)
                        {
                            float radius = age * 0.8;
                            float ring   = exp(-pow(dist - radius, 2.0) * 20.0);
                            float falloff = ring * exp(-age * 3.0) * strength;
                            displacement += normalize(toRipple) * falloff;
                            break;
                        }
                    case 1: //directional ripple (follow along high-velocity objects)
                        {
                            float  along = dot(toRipple, dir);         
                            float  perp = dot(toRipple, float2(-dir.y, dir.x)); 
                            
                            //falloff that elongates behind the object, sharp ahead
                            float  s = max(_Ripples[r].strength, 0.01);
                            float  axialFalloff = exp(-max(along,  0.0) * (1.5 / s)) *
                                                  exp(-max(-along, 0.0) * (0.3 / s)) *
                                                  exp(-abs(perp)        * (3.0 / s)) *
                                                  exp(-age * 2.0);
                                                        
                            //dragging grid lines along the 2 axis
                            // displacement += dir * along * strength * axialFalloff * 0.3;
                            // displacement += float2(-dir.y, dir.x) * perp * strength * axialFalloff * 0.15;
                            displacement +=
                            (
                                dir      * along * 0.3 +
                                float2(-dir.y, dir.x) * perp  * 0.15
                            )
                            * strength
                            * axialFalloff;
                            
                            break;
                        }
                    case 2:  //scanline ripple (extreme impact)
                        {
                            float2 toImpact = uv - _Ripples[r].position;
                            float  proximity = 1.0 - age;
                            
                            float  blockHalfWidth = 1.0;
                            float  lineWidth    = 0.04;  
                            float  scanSpacing  = 0.05; 
                            
                            float  inColumn = step(abs(toImpact.x), blockHalfWidth);

                            //which scanline index is this pixel on
                            float  scanIndex = floor(uv.x / scanSpacing);
                            float  scanLocal = fmod(abs(uv.x), scanSpacing);  // position within one indexed scanline
                            float  onScanline = step(scanLocal, lineWidth);     // 1 if on a line, 0 if in gap
                            
                            // per-line color per index
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
                            float  scanOpacity  = proximity * inColumn * onScanline
                                                * lerp(1.0, withinSilhouette, proximity);
                                                    
                            float  flicker  = frac(sin(scanIndex * 91.3 + floor(_Time.y * 24.0) * 127.1) * 43758.5);
                            float  onOff    = step(0.35, flicker);   // ~65% of lines visible at any frame, different each frame
                            scanOpacity *= onOff;

                            scanlineColor = float4(scanColor * scanOpacity, 0.0);
                            
                            break;
                        }
                    case 3:  // dither bloom: mild collision
                        {
                        float2 toImpact = uv - _Ripples[r].position;
                        float  dist     = length(toImpact);

                        float  intensity = saturate(exp(-dist * 6.0) * exp(-age * 3.0) * strength);

                        // chunky pixel grid + per-cell dither threshold
                        float  cell  = 0.05;
                        float2 pix   = floor(uv / cell);
                        float  bayer = frac(sin(dot(pix, float2(12.9898, 78.233))) * 43758.5453);

                        // white pixels switch on where intensity beats the cell threshold
                        float  on    = step(bayer, intensity);
                        // a fraction of cells flicker per frame for the "unstable" look
                        float  flick = step(0.3, frac(sin(dot(pix, float2(45.2, 13.7))
                                           + floor(_Time.y * 30.0)) * 9182.7));

                        whiteBloom += float4(1, 1, 1, 1) * on * intensity * flick;

                        // mark this region for grid quantization
                        pixelate += intensity;
                        break;
                        }
                    }
                    
                }

                //displace grid sampling position
                float2 gridUV = uv + displacement;
                
                // snap the grid where the bloom is active so the lines go blocky:
                if (pixelate > 0.001)
                {
                    float  q       = saturate(pixelate);
                    float  qcell   = 0.08;
                    float2 snapped = floor(gridUV / qcell) * qcell + qcell * 0.5;
                    gridUV = lerp(gridUV, snapped, q);
                }
                
                float  dispMag    = length(displacement);
                float2 dispDir    = dispMag > 0.001 ? displacement / dispMag : float2(0, 0);

                // aberration follows displacement direction
                float  aberrStr   = smoothstep(0.08, 0.5, dispMag) * 0.05;
                float2 aberrVec   = dispDir * aberrStr;
                aberrVec.y       *= 1.5;   //vertical hits get stronger vertical chromatic sep

                // sample grid lines with chromatic aberration
                // with separate UV offsets for each channel
                float2 uvR = gridUV + aberrVec;
                float2 uvG = gridUV;
                float2 uvB = gridUV - aberrVec;
                
                #define GRID_LINE(guv) saturate( \
                    step(min(fmod(abs(guv), _GridSpacing), \
                             _GridSpacing - fmod(abs(guv), _GridSpacing)).x, _LineWidth) + \
                    step(min(fmod(abs(guv), _GridSpacing), \
                             _GridSpacing - fmod(abs(guv), _GridSpacing)).y, _LineWidth))

                //transition bg color to white as corruption increases
                float4 col = lerp(_BgColor, float4(1.0, 1.0, 1.0, 1.0), _CorruptScore);
                col *= tint;
                
                float4 finalGridColor = lerp(_GridColor, float4(0.0, 0.0, 0.0, 1.0), _CorruptScore);
                float  gridFade = 1.0 - saturate((_CorruptScore - 0.8) / 0.1); //grid dissipates nearing the end
                
                col.r = lerp(col.r, finalGridColor.r, GRID_LINE(uvR) * gridFade);
                col.g = lerp(col.g, finalGridColor.g, GRID_LINE(uvG) * gridFade);
                col.b = lerp(col.b, finalGridColor.b, GRID_LINE(uvB) * gridFade);

                return col + scanlineColor + saturate(whiteBloom);
                
            }
            ENDHLSL
        }
    }
}