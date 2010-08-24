////////////////////////////////////////////////////////////////////////////////
//                 S C E N E R Y   O B J E C T   S H A D E R                  //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

// General values
float4x4 World;                // model -> world
float4x4 View;                 // world -> view
//float4x4 Projection;           // view -> projection (currently unused)
float4x4 WorldViewProjection;  // model -> world -> view -> projection

// Shadow map values
float4x4 LightViewProjectionShadowProjection;  // world -> light view -> light projection -> shadow map projection
texture  ShadowMapTexture;

// Fog values
float4 Fog;  // rgb = color of fog; a = distance from camera, everything is
             // normal color; FogDepth = FogStart, i.e. FogEnd = 2 * FogStart.

// Z-bias value
float ZBias;

float3 LightVector;  // Direction vector to sun

// Headlight values
float4 HeadlightPosition;   // xyz = position; w = lighting scaling.
float3 HeadlightDirection;  // xyz = direction.

float  overcast;       // Lower saturation & brightness when overcast
float3 viewerPos;      // Viewer's world coordinates.
bool   isNight_Tex;    // Using night texture

texture imageMap_Tex;
sampler imageMap = sampler_state
{
	Texture = (imageMap_Tex);
	MagFilter = Linear;
	MinFilter = Anisotropic;
	MipFilter = Linear;
	MaxAnisotropy = 16;
	//AddressU = Wrap;  set in the Materials class
	//AddressV = Wrap;
};

texture normalMap_Tex;
sampler normalMap = sampler_state
{
	Texture = (normalMap_Tex);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
	MipMapLodBias = 0;
	AddressU = Wrap;
	AddressV = Wrap;
};

sampler ShadowMap = sampler_state
{
	Texture = (ShadowMapTexture);
	MagFilter = Linear;
	MinFilter = Anisotropic;
	MipFilter = Linear;
	MaxAnisotropy = 4;
	AddressU = Border;
	AddressV = Border;
};

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float4 Position  : POSITION;
	float2 TexCoords : TEXCOORD0;
	float3 Normal    : NORMAL;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position     : POSITION;
	float2 TexCoords    : TEXCOORD0;
	float4 Normal_Light : TEXCOORD1;
	float4 LightDir_Fog : TEXCOORD2;
	float4 Shadow       : TEXCOORD3;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

void _VSNormalProjection(in VERTEX_INPUT In, inout VERTEX_OUTPUT Out)
{
	// Project position, normal and copy texture coords
	Out.Position = mul(In.Position, WorldViewProjection);
	Out.TexCoords = In.TexCoords;
	Out.Normal_Light.xyz = normalize(mul(In.Normal, World).xyz);
}

void _VSLightsAndShadows(in VERTEX_INPUT In, inout VERTEX_OUTPUT Out)
{
	// Normal lighting
	Out.Normal_Light.w = dot(Out.Normal_Light.xyz, LightVector) * 0.5 + 0.5;

	// Headlight lighting
	Out.LightDir_Fog.xyz = mul(In.Position, World) - HeadlightPosition.xyz;

	// Fog fading
	Out.LightDir_Fog.w = saturate((length(Out.Position.xyz) - Fog.a) / Fog.a);

	// Shadow map
	Out.Shadow = mul(mul(In.Position, World), LightViewProjectionShadowProjection);
}

VERTEX_OUTPUT VSGeneral(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSNormalProjection(In, Out);
	_VSLightsAndShadows(In, Out);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	Out.Position.z -= ZBias * saturate(In.TexCoords.x * (1 - dot(In.Position.xyz, In.Normal.xyz))) / 1000;

	return Out;
}

VERTEX_OUTPUT VSTerrain(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSNormalProjection(In, Out);
	_VSLightsAndShadows(In, Out);
	return Out;
}

VERTEX_OUTPUT VSForest(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	// Start with the three vectors of the view.
	float3 eyeVector = normalize(View._m02_m12_m22);
	float3 upVector = float3(0, -1, 0);
	float3 sideVector = normalize(cross(eyeVector, upVector));

	// Move the vertex left/right/up/down based on the normal values (tree size).
	float3 newPosition = In.Position;
	newPosition += (In.TexCoords.x - 0.5f) * sideVector * In.Normal.x;
	newPosition += (In.TexCoords.y - 1.0f) * upVector * In.Normal.y;
	In.Position = float4(newPosition, 1);

	// Project vertex with fixed w=1 and normal=eye.
	Out.Position = mul(In.Position, WorldViewProjection);
	Out.TexCoords = In.TexCoords;
	Out.Normal_Light.xyz = eyeVector;

	_VSLightsAndShadows(In, Out);

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

// Calculate the day2night fading position; 0 for night, 1 for day.
float _PSGetDay2Night(inout float4 Color)
{
	// The following constants define the beginning and the end conditions of
	// the day-night transition. Values refer to the Y postion of LightVector.
	const float startNightTrans = 0.1;
	const float finishNightTrans = -0.1;
	return saturate((LightVector.y - finishNightTrans) / (startNightTrans - finishNightTrans));
}

// Applies the Variance Shadow Map to the pixel.
void _PSApplyShadowMap(inout float4 Color, in VERTEX_OUTPUT In)
{
	float2 moments = tex2D(ShadowMap, In.Shadow.xy);
	bool outside_shadowmap = any(floor(In.Shadow.xy));
	bool not_shadowed = (In.Shadow.z <= moments.x);
	float E_x2 = moments.y;
	float Ex_2 = moments.x * moments.x;
	float variance = clamp(E_x2 - Ex_2, 0.00001, 1.0);
	float m_d = moments.x - In.Shadow.z;
	float p = variance / (variance + m_d * m_d);
	Color.rgb *= lerp(1.0, lerp(0.5, 1.0, saturate(outside_shadowmap + not_shadowed + p)), _PSGetDay2Night(Color));
}

// Apply lighting with brightness and ambient modifiers.
void _PSApplyBrightnessAndAmbient(inout float4 Color, in VERTEX_OUTPUT In)
{
	Color.rgb *= In.Normal_Light.w * 0.65 + 0.4;
}

// This function dims the lighting at night, with a transition period as the sun rises/sets.
void _PSApplyDay2Night(inout float4 Color)
{
	const float nightCoeff = 0.15;
	const float dayCoeff = 0.9;
	Color.rgb *= lerp(nightCoeff, dayCoeff, _PSGetDay2Night(Color));
}

// This function reduces color saturation and brightness as overcast increases.
// Adapted from an algorithm by Romain Dura aka Romz.
void _PSApplyOvercast(inout float4 Color)
{
	// Values used to determine equivalent grayscale color:
	const float3 LumCoeff = float3(0.2125, 0.7154, 0.0721);
	
	float sat = 1 - overcast;
	float intensityf = dot(Color, LumCoeff);
	Color.rgb = lerp(intensityf, Color.rgb, clamp(sat, 0.8, 1.0));
	
	// Reduce brightness slightly
	// Default overcast=0.2 and sat=1-0.2, so this equation yields a default brightness of 1.0 
	Color.rgb *= 0.6 * (0.867 + sat); 
}

// Applies the lighting effect of the train's headlights, including
// fade-in/fade-out animations.
void _PSApplyHeadlights(inout float4 Color, in float4 OriginalColor, in VERTEX_OUTPUT In)
{
	// Decides the width of the lit cone (larger number = wider lit cone).
	const float headlightWidth = 0.12;
	// Speed of fade at edge of lit cone (larger number = narrower fade at cone edge).
	const float headlightSideFade = 5;
	// Overall strength of headlights (larger number = brighter everywhere in lit cone).
	const float headlightStrength = 2.0;
	// Max distance of lit cone (larger number = longer, slower distance fade of lit cone).
	const float headlightDepth = 500;

	float3 surfaceNormal = normalize(In.Normal_Light.xyz);
	float3 headlightToSurface = normalize(In.LightDir_Fog.xyz);
	float coneDot = dot(headlightToSurface, normalize(HeadlightDirection));

	float shading = step(0, coneDot);
	shading *= step(0, dot(surfaceNormal, -headlightToSurface));
	shading *= saturate((coneDot - 1 + headlightWidth) * headlightSideFade);
	shading *= headlightStrength;
	shading *= saturate(1 - length(In.LightDir_Fog.xyz) / headlightDepth);
	shading *= HeadlightPosition.w;
	Color.rgb += OriginalColor.rgb * shading;
}

// Applies distance fog to the pixel.
void _PSApplyFog(inout float4 Color, in VERTEX_OUTPUT In)
{
	Color.rgb = lerp(Color.rgb, Fog.rgb, In.LightDir_Fog.w);
}

float4 PSImage(in VERTEX_OUTPUT In) : COLOR0
{
	float4 Color = tex2D(imageMap, In.TexCoords);
	// No shadows cast on dark side (side facing away from light) of objects.
	if (In.Normal_Light.w > 0.5)
		_PSApplyShadowMap(Color, In);
	_PSApplyBrightnessAndAmbient(Color, In);
	float4 OriginalColor = Color;
	// TODO: Specular lighting goes here.
	if (!isNight_Tex)
	{
		_PSApplyDay2Night(Color);
		_PSApplyOvercast(Color);
	}
	_PSApplyHeadlights(Color, OriginalColor, In);
	_PSApplyFog(Color, In);
	return Color;
}

float4 PSVegetation(in VERTEX_OUTPUT In) : COLOR0
{ 
	float4 Color = tex2D(imageMap, In.TexCoords);
	// No shadows cast on cruciform material (to prevent visibility of billboard panels).

	// TODO: What are these values for?
	Color.rgb *= 0.8;  
	Color.rgb += 0.03;

	float4 OriginalColor = Color;
	_PSApplyDay2Night(Color);
	_PSApplyOvercast(Color);
	_PSApplyHeadlights(Color, OriginalColor, In);
	_PSApplyFog(Color, In);
	return Color;
}

float4 PSTerrain(in VERTEX_OUTPUT In) : COLOR0
{ 
	float4 Color = tex2D(imageMap, In.TexCoords);
	_PSApplyShadowMap(Color, In);

	// TODO: What are these values for?
	float3 bump = tex2D(normalMap, In.TexCoords * 50);
	bump -= 0.5;
	Color.rgb +=  0.5 * bump;

	_PSApplyBrightnessAndAmbient(Color, In);
	float4 OriginalColor = Color;
	_PSApplyDay2Night(Color);
	_PSApplyOvercast(Color);
	_PSApplyHeadlights(Color, OriginalColor, In);
	_PSApplyFog(Color, In);
	return Color;
}

float4 PSDarkShade(in VERTEX_OUTPUT In) : COLOR0
{ 
	float4 Color = tex2D(imageMap, In.TexCoords);
	// No shadows cast on dark shade material - it is already dark.

	// TODO: What is this value for?
	Color.rgb *= 0.2;

	float4 OriginalColor = Color;
	_PSApplyDay2Night(Color);
	_PSApplyOvercast(Color);
	_PSApplyHeadlights(Color, OriginalColor, In);
	_PSApplyFog(Color, In);
	return Color;

}

float4 PSHalfBright(in VERTEX_OUTPUT In) : COLOR0
{ 
	float4 Color = tex2D(imageMap, In.TexCoords);
	// No shadows cast on light sources.

	// TODO: What is this value for?
	Color.rgb *= 0.55;

	_PSApplyHeadlights(Color, Color, In);
	_PSApplyFog(Color, In);
	return Color;	
}

float4 PSFullBright(in VERTEX_OUTPUT In) : COLOR0
{ 
	float4 Color = tex2D(imageMap, In.TexCoords);
	// No shadows cast on light sources.

	_PSApplyHeadlights(Color, Color, In);
	_PSApplyFog(Color, In);
	return Color;	
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique Image
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSGeneral ( );
      PixelShader = compile ps_2_0 PSImage ( );
   }
}

technique Forest
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSForest ( );
      PixelShader = compile ps_2_0 PSVegetation ( );
   }
}

technique Vegetation
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSGeneral ( );
      PixelShader = compile ps_2_0 PSVegetation ( );
   }
}

technique Terrain
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSTerrain ( );
      PixelShader = compile ps_2_0 PSTerrain ( );
   }
}

technique DarkShade
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSGeneral ( );
      PixelShader = compile ps_2_0 PSDarkShade ( );
   }
}

technique HalfBright
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSGeneral ( );
      PixelShader = compile ps_2_0 PSHalfBright ( );
   }
}

technique FullBright
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSGeneral ( );
      PixelShader = compile ps_2_0 PSFullBright ( );
   }
}
