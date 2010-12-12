////////////////////////////////////////////////////////////////////////////////
//                     S H A D O W   M A P   S H A D E R                      //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

// General values
//float4x4 World;               // model -> world (currently unused)
float4x4 View;                // world -> view (currently unused)
//float4x4 Projection;          // view -> projection (currently unused)
float4x4 WorldViewProjection; // model -> world -> view -> projection

float ImageBlurStep; // = 1 / shadow map texture width and height

texture ImageTexture;
sampler Image = sampler_state
{
	Texture = (ImageTexture);
	MagFilter = Linear;
	MinFilter = Anisotropic;
	MipFilter = Linear;
	MaxAnisotropy = 16;
};
sampler ImagePoint = sampler_state
{
	Texture = (ImageTexture);
	MagFilter = Point;
	MinFilter = Point;
	MipFilter = Point;
};

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float4 Position : POSITION;
	float2 TexCoord : TEXCOORD0;
	float3 Normal   : NORMAL;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position       : POSITION;
	float3 TexCoord_Depth : TEXCOORD0;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

VERTEX_OUTPUT VSShadowMap(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	Out.Position = mul(In.Position, WorldViewProjection);
	Out.Position.z = saturate(Out.Position.z);
	Out.TexCoord_Depth.xy = In.TexCoord;
	Out.TexCoord_Depth.z = Out.Position.z;

	return Out;
}

VERTEX_OUTPUT VSShadowMapForest(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	// Start with the three vectors of the view.
	float3 eyeVector = normalize(View._m02_m12_m22);
	float3 upVector = float3(0, -1, 0);
	float3 sideVector = normalize(cross(eyeVector, upVector));

	// Move the vertex left/right/up/down based on the normal values (tree size).
	float3 newPosition = In.Position;
	newPosition += (In.TexCoord.x - 0.5f) * sideVector * In.Normal.x;
	newPosition += (In.TexCoord.y - 1.0f) * upVector * In.Normal.y;
	In.Position = float4(newPosition, 1);

	// Project vertex with fixed w=1 and normal=eye.
	Out.Position = mul(In.Position, WorldViewProjection);
	Out.Position.z = saturate(Out.Position.z);
	Out.TexCoord_Depth.xy = In.TexCoord;
	Out.TexCoord_Depth.z = Out.Position.z;

	return Out;
}

VERTEX_OUTPUT VSShadowMapBlur(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	Out.Position = mul(In.Position, WorldViewProjection);
	Out.TexCoord_Depth.xy = In.TexCoord;

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

float4 PSShadowMap(in VERTEX_OUTPUT In) : COLOR0
{
	if (tex2D(Image, In.TexCoord_Depth.xy).a < 0.25)
		discard;
	return float4(In.TexCoord_Depth.z, In.TexCoord_Depth.z * In.TexCoord_Depth.z, 0, 0);
}

float4 PSShadowMapBlocker(in VERTEX_OUTPUT In) : COLOR0
{
	return 0;
}

float4 PSShadowMapBlurX(in VERTEX_OUTPUT In) : COLOR0
{
	float4 Color = 0;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy + float2(4, 0)) / ImageBlurStep) * 0.02;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy + float2(3, 0)) / ImageBlurStep) * 0.05;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy + float2(2, 0)) / ImageBlurStep) * 0.12;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy + float2(1, 0)) / ImageBlurStep) * 0.19;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy + float2(0, 0)) / ImageBlurStep) * 0.24;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy - float2(1, 0)) / ImageBlurStep) * 0.19;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy - float2(2, 0)) / ImageBlurStep) * 0.12;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy - float2(3, 0)) / ImageBlurStep) * 0.05;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy - float2(4, 0)) / ImageBlurStep) * 0.02;
	return Color;
}

float4 PSShadowMapBlurY(in VERTEX_OUTPUT In) : COLOR0
{
	float4 Color = 0;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy + float2(0, 4)) / ImageBlurStep) * 0.02;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy + float2(0, 3)) / ImageBlurStep) * 0.05;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy + float2(0, 2)) / ImageBlurStep) * 0.12;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy + float2(0, 1)) / ImageBlurStep) * 0.19;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy + float2(0, 0)) / ImageBlurStep) * 0.24;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy - float2(0, 1)) / ImageBlurStep) * 0.19;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy - float2(0, 2)) / ImageBlurStep) * 0.12;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy - float2(0, 3)) / ImageBlurStep) * 0.05;
	Color += tex2D(ImagePoint, (In.TexCoord_Depth.xy - float2(0, 4)) / ImageBlurStep) * 0.02;
	return Color;
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique ShadowMap {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSShadowMap();
		PixelShader = compile ps_2_0 PSShadowMap();
	}
}

technique ShadowMapForest {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSShadowMapForest();
		PixelShader = compile ps_2_0 PSShadowMap();
	}
}

technique ShadowMapBlocker {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSShadowMap();
		PixelShader = compile ps_2_0 PSShadowMapBlocker();
	}
}

technique ShadowMapBlur {
	pass Blur_X {
		VertexShader = compile vs_2_0 VSShadowMapBlur();
		PixelShader = compile ps_2_0 PSShadowMapBlurX();
	}
	pass Blur_Y {
		VertexShader = compile vs_2_0 VSShadowMapBlur();
		PixelShader = compile ps_2_0 PSShadowMapBlurY();
	}
}
