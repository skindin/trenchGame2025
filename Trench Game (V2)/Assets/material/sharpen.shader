Shader "Custom/Sharpen"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _BlurAmount ("Blur Amount", Range(0, 5)) = 2.0
        _SharpnessAmount ("Sharpness Amount", Range(0, 5)) = 2.0
        _ColorRamp ("Color Ramp", 2D) = "black" { }
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Declare properties
            sampler2D _MainTex;
            float _BlurAmount;
            float _SharpnessAmount;
            sampler2D _ColorRamp;
            float2 _MainTex_TexelSize;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Apply a simple Gaussian blur
            float4 ApplyGaussianBlur(float2 uv)
            {
                float4 sum = tex2D(_MainTex, uv);
                float2 texOffset = _BlurAmount * _MainTex_TexelSize;

                sum += tex2D(_MainTex, uv + texOffset);
                sum += tex2D(_MainTex, uv - texOffset);
                sum += tex2D(_MainTex, uv + texOffset * 2.0);
                sum += tex2D(_MainTex, uv - texOffset * 2.0);

                return sum / 5.0; // Blur average
            }

            // Sharpen with a simple color ramp
            float3 ApplySharpness(float3 color)
            {
                return tex2D(_ColorRamp, float2(0.5, color.r)).rgb * _SharpnessAmount;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Sample the texture and preserve alpha
                float4 texColor = tex2D(_MainTex, i.uv);
                float alpha = texColor.a;

                // Apply Gaussian blur
                texColor = ApplyGaussianBlur(i.uv);

                // Apply color ramp sharpening to the RGB values
                texColor.rgb = ApplySharpness(texColor.rgb);

                // Restore alpha
                texColor.a = alpha;

                return texColor;
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
