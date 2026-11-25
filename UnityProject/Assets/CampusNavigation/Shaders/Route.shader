Shader "CampusNavigation/Route" {
Properties { _Color ("Color", Color) = (0.02,0.4,1,1) }
SubShader { Tags { "RenderType"="Opaque" "Queue"="Geometry+10" } Cull Off
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct a {float4 vertex:POSITION;}; struct v {float4 vertex:SV_POSITION;}; fixed4 _Color;
v vert(a i){v o;o.vertex=UnityObjectToClipPos(i.vertex);return o;}
fixed4 frag(v i):SV_Target{return _Color;}
ENDCG }
} }
