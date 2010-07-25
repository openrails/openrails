////////////////////////////////////////////////////////////////////////////////
//                 S C E N E R Y   O B J E C T   S H A D E R                  //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

// General values
float4x4 World;               // model -> world
float4x4 View;                // world -> view
//float4x4 Projection;          // view -> projection (currently unused)
float4x4 WorldViewProjection; // model -> world -> view -> projection

// Shadow map values
float4x4 LightView;
float4x4 LightProj;
float4x4 ShadowMapProj;

// Fog values (unchanging)
uniform const float FogEnabled; // 0 for off, 1 for on
uniform const float FogStart;   // distance from camera, everything is normal color
uniform const float FogEnd;     // distance from camera, everything is FogColor
uniform const float3 FogColor;  // color of fog

// Z-bias setting.
uniform const float ZBias;

float3 LightVector;								// Direction vector to sun
float3 BumpScale = float3( 1.0, -1.0, 1.0 );	// multiply bump map by this  -1 seems to work with Ultimapper sometimes

float Saturation = 0.9;
float Ambient = 0.5;
float Brightness = 0.7;
float overcast;									// Lower saturation & brightness when overcast
float3 viewerPos;								// Viewer's world coordinates.
float specularPower;							// Exponent -- lower number yields greater specularity
bool isNight_Tex;								// Using night texture

// Headlight illumination params
float3 headlightPosition;
float3 headlightDirection;
float lightStrength = 2.0;
float coneAngle = 0.5;
float coneDecay = 8.0;
float fadeinTime;								// Constant, from ENG file
float fadeoutTime;								// Constant, from ENG file
float fadeTime;									// Varies, reset on H key press
int stateChange;								// 1=Off->On; 2=On->Off

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
   MagFilter =  Linear;
   MinFilter =  Linear;
   MipFilter =  Linear;
   MipMapLodBias = 0;
   AddressU = Wrap;
   AddressV = Wrap;
};

texture shadowMap_Tex;
sampler shadowMap = sampler_state
{
	Texture = (shadowMap_Tex);
	MagFilter = Point;
	MinFilter = Point;
	MipFilter = Point;
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
	Out.LightDir_Fog.xyz = mul(In.Position, World) - headlightPosition;

	// Fog fading
	Out.LightDir_Fog.w = saturate((length(Out.Position.xyz) - FogStart) / (FogEnd - FogStart)) * FogEnabled;

	// Shadow map
	Out.Shadow = mul(mul(mul(mul(In.Position, World), LightView), LightProj), ShadowMapProj);
}

VERTEX_OUTPUT VSGeneral(VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSNormalProjection(In, Out);
	_VSLightsAndShadows(In, Out);

	// Z-bias to reduce and eliminate z-fighting on track ballast. ZBias is 0 or 1.
	Out.Position.z -= ZBias * saturate(In.TexCoords.x * (1 - dot(In.Position.xyz, In.Normal.xyz))) / 1000;

	return Out;
}

VERTEX_OUTPUT VSTerrain(VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	_VSNormalProjection(In, Out);
	_VSLightsAndShadows(In, Out);
	return Out;
}

VERTEX_OUTPUT VSForest(VERTEX_INPUT In)
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

// Applies the shadow map to the pixel using single texture look-up.
void _PS2ApplyShadowMap(inout float4 Color, in VERTEX_OUTPUT In)
{
	Color.rgb *= (saturate(In.Shadow.x) != In.Shadow.x) || (saturate(In.Shadow.y) != In.Shadow.y) ? 1 : (tex2D(shadowMap, In.Shadow.xy).x < In.Shadow.z - 0.001f ? 0.5f : 1.0f);
}

// Applies the shadow map to the pixel using multiple texture look-ups.
void _PS3ApplyShadowMap(inout float4 Color, in VERTEX_OUTPUT In)
{
	int clip = ((saturate(In.Shadow.x) != In.Shadow.x) || (saturate(In.Shadow.y) != In.Shadow.y)) ? 1 : 0;
	const float o = 1.0 / 4096;
	float x = 0;
	x += step(In.Shadow.z - 0.001f, tex2D(shadowMap, In.Shadow.xy + float2(-o, -o)).x);
	x += step(In.Shadow.z - 0.001f, tex2D(shadowMap, In.Shadow.xy + float2(-o,  0)).x);
	x += step(In.Shadow.z - 0.001f, tex2D(shadowMap, In.Shadow.xy + float2(-o,  o)).x);
	x += step(In.Shadow.z - 0.001f, tex2D(shadowMap, In.Shadow.xy + float2( 0, -o)).x);
	x += step(In.Shadow.z - 0.001f, tex2D(shadowMap, In.Shadow.xy + float2( 0,  0)).x);
	x += step(In.Shadow.z - 0.001f, tex2D(shadowMap, In.Shadow.xy + float2( 0,  o)).x);
	x += step(In.Shadow.z - 0.001f, tex2D(shadowMap, In.Shadow.xy + float2( o, -o)).x);
	x += step(In.Shadow.z - 0.001f, tex2D(shadowMap, In.Shadow.xy + float2( o,  0)).x);
	x += step(In.Shadow.z - 0.001f, tex2D(shadowMap, In.Shadow.xy + float2( o,  o)).x);
	Color.rgb *= saturate(clip + 0.5f + x / 18);
}

// Apply lighting with brightness and ambient modifiers.
void _PSApplyBrightnessAndAmbient(inout float4 Color, in VERTEX_OUTPUT In)
{
	Color.rgb *= In.Normal_Light.w * 0.65 + 0.4;
}

// This function dims the lighting at night, with a transition period as the sun rises/sets.
void _PSApplyDay2Night(inout float4 Color)
{
	// The following constants define the beginning and the end conditions of the day-night transition
	const float startNightTrans = 0.1; // The "NightTrans" values refer to the Y postion of LightVector
	const float finishNightTrans = -0.1;
	const float minDarknessCoeff = 0.15;

	// Internal variables
	// The following two are used to interpoate between day and night lighting (y = mx + b)
	// Can't use lerp() here, as overall dimming action is too complex
	float slope = (1.0 - minDarknessCoeff) / (startNightTrans - finishNightTrans); // "m"
	float incpt = 1.0 - slope * startNightTrans; // "b"
	// This is the return value used to darken scenery
	float adjustment;

    if (LightVector.y < finishNightTrans)
      adjustment = minDarknessCoeff;
    else if (LightVector.y > startNightTrans)
      adjustment = 0.9; // Scenery is fully lit during the day
    else
      adjustment = slope * LightVector.y + incpt;

	Color.rgb *= adjustment;
}

// This function reduces color saturation and brightness as overcast increases.
// Adapted from an algorithm by Romain Dura aka Romz.
void _PSApplyOvercast(inout float4 Color)
{
	// This value limits desaturation amount:
	const float satLower = 0.8; 
	// Values used to determine equivalent grayscale color:
	const float3 LumCoeff = float3(0.2125, 0.7154, 0.0721);
	
	float sat = 1 - overcast;
	float intensityf = dot(Color, LumCoeff);
	float3 intensity = float3(intensityf, intensityf, intensityf);
	Color.rgb = lerp(intensity, Color.rgb, clamp(sat, satLower, 1.0));
	
	// Reduce brightness slightly
	// Default overcast=0.2 and sat=1-0.2, so this equation yields a default brightness of 1.0 
	Color.rgb *= 0.6 * (0.867 + sat); 
}

void _PSApplyHeadlights(inout float4 Color, in float4 OriginalColor, in VERTEX_OUTPUT In)
{
	float3 normal = normalize(In.Normal_Light.xyz);
	float3 lightDir = normalize(In.LightDir_Fog.xyz);
	float coneDot = dot(lightDir, normalize(headlightDirection));
	float shading = 0;

	if (coneDot > coneAngle)
	{
		float coneAtten = pow(coneDot, coneDecay * 1.75);
		shading = dot(normal, -lightDir) * lightStrength * coneAtten;
	}

	if (stateChange == 0)
		shading = 0;
	if (stateChange == 1)
		shading *= clamp(fadeTime / fadeinTime, 0, 1);
	if (stateChange == 2)
		shading *= clamp(1 - (fadeTime / fadeoutTime), 0, 1);

	Color.rgb += OriginalColor.rgb * shading;
}

// Applies distance fog to the pixel.
void _PSApplyFog(inout float4 Color, in VERTEX_OUTPUT In)
{
	Color.rgb = lerp(Color.rgb, FogColor, In.LightDir_Fog.w);
}

float4 PS2Image(VERTEX_OUTPUT In) : COLOR
{
	float4 Color = tex2D(imageMap, In.TexCoords);
	// No shadows cast on dark side (side facing away from light) of objects.
	if (In.Normal_Light.w > 0.5)
		_PS2ApplyShadowMap(Color, In);
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

float4 PS3Image(VERTEX_OUTPUT In) : COLOR
{
	float4 Color = tex2D(imageMap, In.TexCoords);
	// No shadows cast on dark side (side facing away from light) of objects.
	if (In.Normal_Light.w > 0.5)
		_PS3ApplyShadowMap(Color, In);
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

float4 PSVegetation(VERTEX_OUTPUT In) : COLOR
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

float4 PS2Terrain(VERTEX_OUTPUT In) : COLOR
{ 
	float4 Color = tex2D(imageMap, In.TexCoords);
	_PS2ApplyShadowMap(Color, In);

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

float4 PS3Terrain(VERTEX_OUTPUT In) : COLOR
{ 
	float4 Color = tex2D(imageMap, In.TexCoords);
	_PS3ApplyShadowMap(Color, In);

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

float4 PSDarkShade(VERTEX_OUTPUT In) : COLOR
{ 
	float4 Color = tex2D(imageMap, In.TexCoords);
	// No shadows cast on dark shade material - it is already dark.

	// TODO: What is this value for?
	Color.rgb *= 0.2;

	_PSApplyDay2Night(Color);
	_PSApplyOvercast(Color);
	_PSApplyFog(Color, In);
	return Color;

}

float4 PSHalfBright(VERTEX_OUTPUT In) : COLOR
{ 
	float4 Color = tex2D(imageMap, In.TexCoords);
	// No shadows cast on light sources.

	Color.rgb *= 0.55;

	_PSApplyFog(Color, In);
	return Color;	
}

float4 PSFullBright(VERTEX_OUTPUT In) : COLOR
{ 
	float4 Color = tex2D(imageMap, In.TexCoords);
	// No shadows cast on light sources.
	_PSApplyFog(Color, In);
	return Color;	
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique Image
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSGeneral ( );
      PixelShader = compile ps_2_0 PS2Image ( );
   }
}

technique Image_PS3
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSGeneral ( );
      PixelShader = compile ps_3_0 PS3Image ( );
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
      PixelShader = compile ps_2_0 PS2Terrain ( );
   }
}

technique Terrain_PS3
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSTerrain ( );
      PixelShader = compile ps_3_0 PS3Terrain ( );
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
