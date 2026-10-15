Shader "Darky/ScrollingPhaseBlend"
{
    Properties
    {
        [PerRendererData] _MainTex ("Light texture", 2D) = "white" {}
        _PhaseTex ("Dark texture", 2D) = "white" {}
        _PhaseBlend ("Phase blend", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;
            sampler2D _PhaseTex;
            float4 _MainTex_ST;
            float _PhaseBlend;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 light = tex2D(_MainTex, i.uv);
                fixed4 dark = tex2D(_PhaseTex, i.uv);
                // Premultiplied blending also preserves the transparent cloud borders.
                light.rgb *= light.a;
                dark.rgb *= dark.a;
                fixed4 color = lerp(light, dark, saturate(_PhaseBlend));
                color.rgb *= i.color.rgb * i.color.a;
                color.a *= i.color.a;
                return color;
            }
            ENDCG
        }
    }
}
