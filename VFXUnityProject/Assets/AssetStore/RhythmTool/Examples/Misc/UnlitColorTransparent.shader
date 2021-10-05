Shader "Unlit/Color Transparent" {

	Properties
	{
		_Color("Color", Color) = (1,1,1)
	}

		SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }

		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		ZTest LEqual
		Color[_Color]

		Pass{}
	}
}