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
                        case 2: // inversion artifact — falling block warning
                        {
                            float2 toImpact = uv - _Ripples[r].position;
                            
                            // --- core: which pixel columns are "infected" ---
                            // age drives spread radius outward from impact X
                            // strength encodes how far the block still is (CPU sets 1.0 = just spawned, 0.0 = arrived)
                            float  spreadWidth  = (1.0 - strength) * 12.0 * _GridSpacing; // widens as block descends
                            float  inSpread     = step(abs(toImpact.x), spreadWidth);
                            
                            // column identity for chessboard / inversion pattern
                            float  colIndex     = floor(uv.x / _GridSpacing);
                            float  rowIndex     = floor(uv.y / _GridSpacing);
                            
                            // --- chessboard inversion: alternating cells flip color channels ---
                            float  chess        = fmod(abs(colIndex + rowIndex), 2.0); // 0 or 1, alternating
                            
                            // inversion strength fades with distance from impact X, and with age jitter
                            float  distFalloff  = exp(-abs(toImpact.x) / max(spreadWidth, 0.001));
                            float  invStrength  = inSpread * distFalloff * exp(-age * 0.4); // slow decay — it lingers
                            
                            // --- voltage inversion: UV rows get flipped within their cell ---
                            // this is the actual LCD artifact — pixel rows read inverted charge
                            float  cellY        = fmod(abs(uv.y), _GridSpacing);
                            float  invertedCellY = _GridSpacing - cellY;
                            float  corruptY     = lerp(cellY, invertedCellY, chess * invStrength);
                            
                            // reconstruct displaced UV — only Y rows invert, X stays (inversion is row-based)
                            float  baseY        = floor(abs(uv.y) / _GridSpacing) * _GridSpacing;
                            displacement.y     += (corruptY - cellY) * inSpread * distFalloff;
                            
                            // --- scanline: the leading edge column tears hardest ---
                            // sharp vertical line at the current spread boundary
                            float  edgeDist     = abs(abs(toImpact.x) - spreadWidth);
                            float  edgeLine     = exp(-edgeDist * 8.0 / _GridSpacing);       // tight falloff at boundary
                            displacement.x     += edgeLine * invStrength * _GridSpacing * 0.6; // columns shear at the tear
                            
                            // --- chromatic: purple/green cast on inverted cells ---
                            // encode into tint — handled separately in the chromatic pass below
                            // store invStrength * chess into a signal the color pass can read
                            // since we can't return two values, bake it into tint here
                            float  greenBias    = chess * invStrength * inSpread;
                            float  purpleBias   = (1.0 - chess) * invStrength * inSpread;
                            
                            tint.r += purpleBias * 0.6;
                            tint.g += greenBias  * 0.5;
                            tint.b += purpleBias * 0.6;
                            
                            // --- proximity flash: as block approaches (strength -> 0), edge brightens ---
                            float  proximityPulse = (1.0 - strength) * edgeLine * 1.5;
                            tint.xyz             += proximityPulse;
                            
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

return col;
                
            }
            ENDHLSL
        }
    }
}