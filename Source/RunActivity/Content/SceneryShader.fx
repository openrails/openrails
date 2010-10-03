////////////////////////////////////////////////////////////////////////////////
//                 S C E N E R Y   O B J E C T   S H A D E R                  //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

// General values
float4x4 World;                // model -> world
float4x4 View;                 // world -> view
//float4x4 Projection;           // view -> projection (currently unused)
float4x4 WorldView;            // model -> world -> view
float4x4 WorldViewProjection;  // model -> world -> view -> projection

// Shadow map values
float4x4 LightViewProjectionShadowProjection;  // world -> light view -> light projection -> shadow map projection
texture  ShadowMapTexture;

// Z-bias and lighting coeffecients
float3 ZBias_Lighting;  // x = z-bias, y = diffuse, z = specular

// Fog values
float4 Fog;  // rgb = color of fog; a = distance from camera, everything is
             // normal color; FogDepth = FogStart, i.e. FogEnd = 2 * FogStart.

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
	float4 Color     : COLOR0;
	float3 Normal    : NORMAL;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position     : POSITION;
	float3 RelPosition  : TEXCOORD0;
	float2 TexCoords    : TEXCOORD1;
	float4 Color        : COLOR0;
	float4 Normal_Light : TEXCOORD2;
	float4 LightDir_Fog : TEXCOORD3;
	float4 Shadow       : TEXCOORD4;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

void _VSNormalProjection(in VERTEX_INPUT In, inout VERTEX_OUTPUT Out)
{
	// Project position, normal and copy texture coords
	Out.Position = mul(In.Position, WorldViewProjection);
	Out.RelPosition = mul(In.Position, WorldView);
	Out.TexCoords = In.TexCoords;
	Out.Color = In.Color;
	Out.Normal_Light.xyz = normalize(mul(In.Normal, World).xyz);
}

void _VSLightsAndShadows(in VERTEX_INPUT In, inout VERTEX_OUTPUT Out)
{
	// Normal lighting (range 0.0 - 1.0)
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
	Out.Position.z -= ZBias_Lighting.x * saturate(In.TexCoords.x * (1 - dot(In.Position.xyz, In.Normal.xyz))) / 1000;

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

VERTEX_OUTPUT VSSignalLight(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSNormalProjection(In, Out);

	// Apply a small z-bias so that lights are always on top of the shape.
	Out.Position.z *= 0.9999;

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

// Gets the ambient light effect.
float _PSGetAmbientEffect(in VERTEX_OUTPUT In)
{
	return In.Normal_Light.w * ZBias_Lighting.y;
}

// Gets the specular light effect.
float _PSGetSpecularEffect(in VERTEX_OUTPUT In)
{
	float3 halfVector = normalize(normalize(-In.RelPosition) + normalize(LightVector));
	return step(0.5, In.Normal_Light.w) * step(1, ZBias_Lighting.z) * pow(saturate(dot(normalize(In.Normal_Light.xyz), halfVector)), ZBias_Lighting.z);
}

// Gets the shadow effect.
float _PSGetShadowEffect(in VERTEX_OUTPUT In)
{
	float2 moments = tex2D(ShadowMap, In.Shadow.xy);
	bool outside_shadowmap = any(floor(In.Shadow.xy));
	bool not_shadowed = (In.Shadow.z <= moments.x);
	float E_x2 = moments.y;
	float Ex_2 = moments.x * moments.x;
	float variance = clamp(E_x2 - Ex_2, 0.00001, 1.0);
	float m_d = moments.x - In.Shadow.z;
	float p = variance / (variance + m_d * m_d);
	return saturate(outside_shadowmap + not_shadowed + p) * saturate(In.Normal_Light.w * 5 - 2);
}

// Gets the overcast effect.
float3 _PSGetOvercastEffect()
{
	return overcast;
}

// Gets the overcast color.
float3 _PSGetOvercastColor(in float4 Color, in VERTEX_OUTPUT In)
{
	// Value used to determine equivalent grayscale color.
	const float3 LumCoeff = float3(0.2125, 0.7154, 0.0721);

	float intensity = dot(Color, LumCoeff);
	return lerp(intensity, Color.rgb, 0.8) * 0.5;
}

// Gets the night-time effect.
float _PSGetNightEffect()
{
	// The following constants define the beginning and the end conditions of
	// the day-night transition. Values refer to the Y postion of LightVector.
	const float startNightTrans = 0.1;
	const float finishNightTrans = -0.1;
	return saturate((LightVector.y - finishNightTrans) / (startNightTrans - finishNightTrans));
}

// Applies the lighting effect of the train's headlights, including
// fade-in/fade-out animations.
void _PSApplyHeadlights(inout float3 Color, in float3 OriginalColor, in VERTEX_OUTPUT In)
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
	Color += OriginalColor * shading;
}

// Applies distance fog to the pixel.
void _PSApplyFog(inout float3 Color, in VERTEX_OUTPUT In)
{
	Color = lerp(Color, Fog.rgb, In.LightDir_Fog.w);
}

float4 PSImage(in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float ShadowBrightness = 0.5;
	const float NightBrightness = 0.2;

	float4 Color = tex2D(imageMap, In.TexCoords);
	// Ambient and shadow effects apply first; night-time textures cancel out all normal lighting.
	float3 litColor = Color.rgb * lerp(ShadowBrightness, FullBrightness, saturate(_PSGetAmbientEffect(In) * _PSGetShadowEffect(In) + isNight_Tex));
	// Specular effect next.
	litColor += _PSGetSpecularEffect(In);
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), _PSGetOvercastEffect());
	// Night-time darkens everything, except night-time textures.
	litColor *= lerp(NightBrightness, FullBrightness, saturate(_PSGetNightEffect() + isNight_Tex));
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSVegetation(in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float ShadowBrightness = 0.5;
	const float NightBrightness = 0.2;

	float4 Color = tex2D(imageMap, In.TexCoords);
	// Ambient effect applies first; no shadow effect for vegetation; night-time textures cancel out all normal lighting.
	float3 litColor = Color.rgb * lerp(ShadowBrightness, FullBrightness, saturate(_PSGetAmbientEffect(In) + isNight_Tex));
	// No specular effect for vegetation.
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), _PSGetOvercastEffect());
	// Night-time darkens everything, except night-time textures.
	litColor *= lerp(NightBrightness, FullBrightness, saturate(_PSGetNightEffect() + isNight_Tex));
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSTerrain(in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float ShadowBrightness = 0.5;
	const float NightBrightness = 0.2;

	float4 Color = tex2D(imageMap, In.TexCoords);
	// Ambient and shadow effects apply first; night-time textures cancel out all normal lighting.
	float3 litColor = Color.rgb * lerp(ShadowBrightness, FullBrightness, saturate(_PSGetAmbientEffect(In) * _PSGetShadowEffect(In) + isNight_Tex));
	// No specular effect for terrain.
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), _PSGetOvercastEffect());
	// Night-time darkens everything, except night-time textures.
	litColor *= lerp(NightBrightness, FullBrightness, saturate(_PSGetNightEffect() + isNight_Tex));

	// TODO: What are these values for?
	//float3 bump = tex2D(normalMap, In.TexCoords * 50);
	//bump -= 0.5;
	//Color.rgb += 0.5 * bump;

	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSDarkShade(in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float ShadowBrightness = 0.5;
	const float NightBrightness = 0.2;

	float4 Color = tex2D(imageMap, In.TexCoords);
	// Fixed ambient and shadow effects at darkest level.
	float3 litColor = Color.rgb * ShadowBrightness;
	// No specular effect for dark shade.
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), _PSGetOvercastEffect());
	// Night-time darkens everything, except night-time textures.
	litColor *= lerp(NightBrightness, FullBrightness, saturate(_PSGetNightEffect() + isNight_Tex));
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSHalfBright(in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float HalfShadowBrightness = 0.75;
	const float NightBrightness = 0.2;

	float4 Color = tex2D(imageMap, In.TexCoords);
	// Fixed ambient and shadow effects at mid-dark level.
	float3 litColor = Color.rgb * HalfShadowBrightness;
	// No specular effect for half-bright.
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), _PSGetOvercastEffect());
	// Night-time darkens everything, except night-time textures.
	litColor *= lerp(NightBrightness, FullBrightness, saturate(_PSGetNightEffect() + isNight_Tex));
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSFullBright(in VERTEX_OUTPUT In) : COLOR0
{
	const float FullBrightness = 1.0;
	const float NightBrightness = 0.2;

	float4 Color = tex2D(imageMap, In.TexCoords);
	// Fixed ambient and shadow effects at brightest level.
	float3 litColor = Color.rgb;
	// No specular effect for full-bright.
	// Overcast blanks out ambient, shadow and specular effects (so use original Color).
	litColor = lerp(litColor, _PSGetOvercastColor(Color, In), _PSGetOvercastEffect());
	// Night-time darkens everything, except night-time textures.
	litColor *= lerp(NightBrightness, FullBrightness, saturate(_PSGetNightEffect() + isNight_Tex));
	// Headlights effect use original Color.
	_PSApplyHeadlights(litColor, Color, In);
	// And fogging is last.
	_PSApplyFog(litColor, In);
	return float4(litColor, Color.a);
}

float4 PSSignalLight(in VERTEX_OUTPUT In) : COLOR0
{
	float4 Color = tex2D(imageMap, In.TexCoords);
	// No ambient and shadow effects for signal lights.
	// Apply signal coloring effect.
	float3 litColor = lerp(Color.rgb, In.Color.rgb, Color.r);
	// No specular effect, overcast effect, night-time darkening, headlights or fogging effect for signal lights.
	return float4(litColor, Color.a);
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique Image {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSGeneral();
		PixelShader = compile ps_2_0 PSImage();
	}
}

technique Forest {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSForest();
		PixelShader = compile ps_2_0 PSVegetation();
	}
}

technique Vegetation {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSGeneral();
		PixelShader = compile ps_2_0 PSVegetation();
	}
}

technique Terrain {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSTerrain();
		PixelShader = compile ps_2_0 PSTerrain();
	}
}

technique DarkShade {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSGeneral();
		PixelShader = compile ps_2_0 PSDarkShade();
	}
}

technique HalfBright {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSGeneral();
		PixelShader = compile ps_2_0 PSHalfBright();
	}
}

technique FullBright {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSGeneral();
		PixelShader = compile ps_2_0 PSFullBright();
	}
}

technique SignalLight {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSSignalLight();
		PixelShader = compile ps_2_0 PSSignalLight();
	}
}
