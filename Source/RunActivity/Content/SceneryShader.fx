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

float3 LightVector;								// Direction vector to sun
float3 BumpScale = float3( 1.0, -1.0, 1.0 );	// multiply bump map by this  -1 seems to work with Ultimapper sometimes

float Saturation = 0.9;
float Ambient = 0.5;
float Brightness = 0.7;
float ZBias = 0.0;  // TODO TESTING
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
   MAGFILTER = LINEAR;
   MINFILTER = LINEAR;
   MIPFILTER = Linear;
   //AddressU = Wrap;  set in the Materials class
   //AddressV = Wrap;
};

texture normalMap_Tex;
sampler normalMap = sampler_state
{
   Texture = (normalMap_Tex);
   MAGFILTER =  Linear;
   MINFILTER =  Linear;
   MIPFILTER =  Linear;
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

VERTEX_OUTPUT VSCommon(VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0; 

	// Project vertex normally.
	Out.Position = mul(In.Position, WorldViewProjection);
	Out.TexCoords = In.TexCoords;
	Out.Normal_Light.xyz = normalize(mul(In.Normal, World).xyz);
	Out.Normal_Light.w = dot(Out.Normal_Light.xyz, LightVector) * 0.5 + 0.5;

	// Headlight
	Out.LightDir_Fog.xyz = mul(In.Position, World) - headlightPosition;

	// Fog
	Out.LightDir_Fog.w = saturate((length(Out.Position.xyz) - FogStart) / (FogEnd - FogStart)) * FogEnabled;

	// Shadow map
	Out.Shadow = mul(mul(mul(mul(In.Position, World), LightView), LightProj), ShadowMapProj);

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

	// Project vertex with fixed w=1 and normal=eye.
	Out.Position = mul(float4(newPosition, 1), WorldViewProjection);
	Out.TexCoords = In.TexCoords;
	Out.Normal_Light.xyz = eyeVector;
	Out.Normal_Light.w = dot(Out.Normal_Light.xyz, LightVector) * 0.5 + 0.5;

	// Headlight
	Out.LightDir_Fog.xyz = mul(newPosition, World) - headlightPosition;

	// Fog
	Out.LightDir_Fog.w = saturate((length(Out.Position.xyz) - FogStart) / (FogEnd - FogStart)) * FogEnabled;

	// Shadow map
	Out.Shadow = mul(newPosition, mul(mul(mul(World, LightView), LightProj), ShadowMapProj));

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

// This function dims the lighting at night, with a transition period as the sun rises/sets
float Day2Night( )
{
	// The following constants define the beginning and the end conditions of the day-night transition
	const float startNightTrans = 0.1; // The "NightTrans" values refer to the Y postion of LightVector
	const float finishNightTrans = -0.1;
	const float minDarknessCoeff = 0.15;
	
	// Internal variables
	// The following two are used to interpoate between day and night lighting (y = mx + b)
	// Can't use lerp() here, as overall dimming action is too complex
	float slope = (1.0-minDarknessCoeff)/(startNightTrans-finishNightTrans); // "m"
	float incpt = 1.0 - slope*startNightTrans; // "b"
	// This is the return value used to darken scenery
	float adjustment;
	
    if (LightVector.y < finishNightTrans)
      adjustment = minDarknessCoeff;
    else if (LightVector.y > startNightTrans)
      adjustment = 0.9; // Scenery is fully lit during the day
    else
      adjustment = slope*LightVector.y + incpt;

	return adjustment;
}

// This function reduces color saturation and brightness as overcast increases
// Adapted from an algorithm by Romain Dura aka Romz
float3 Overcast(float3 color, float sat)
{
	// This value limits desaturation amount:
	const float satLower = 0.8; 
	// Values used to determine equivalent grayscale color:
	const float3 LumCoeff = float3(0.2125, 0.7154, 0.0721);
	
	float intensityf = dot(color, LumCoeff);
	float3 intensity = float3(intensityf, intensityf, intensityf);
	float3 satColor = lerp(intensity, color, clamp(sat, satLower, 1.0));
	
	// Reduce brightness slightly
	// Default overcast=0.2 and sat=1-0.2, so this equation yields a default brightness of 1.0 
	satColor *= 0.6*(0.867+sat); 
	
	return satColor;
}

// Take the mapped shadow location (Shadow) from the vetex shader and return
// an rgb multiplier for the shadow effect.
float GetShadowMap(float4 Shadow)
{
	int clip = ((saturate(Shadow.x) != Shadow.x) || (saturate(Shadow.y) != Shadow.y)) ? 1 : 0;
	//return tex2Dproj(shadowMap, Shadow).x < Shadow.z - 0.0001f ? 0.5f : 1.0f;
	//return lerp(1.0f, 0.5f, saturate(abs(tex2Dproj(shadowMap, Shadow).x - Shadow.z) * 100));
	const float s = 4096;
	const float o = 1 / s;
	float x = 0;
	x += step(Shadow.z - 0.001f, tex2D(shadowMap, Shadow.xy + float2(-o, -o)).x);
	x += step(Shadow.z - 0.001f, tex2D(shadowMap, Shadow.xy + float2(-o,  0)).x);
	x += step(Shadow.z - 0.001f, tex2D(shadowMap, Shadow.xy + float2(-o,  o)).x);
	x += step(Shadow.z - 0.001f, tex2D(shadowMap, Shadow.xy + float2( 0, -o)).x);
	x += step(Shadow.z - 0.001f, tex2D(shadowMap, Shadow.xy + float2( 0,  0)).x);
	x += step(Shadow.z - 0.001f, tex2D(shadowMap, Shadow.xy + float2( 0,  o)).x);
	x += step(Shadow.z - 0.001f, tex2D(shadowMap, Shadow.xy + float2( o, -o)).x);
	x += step(Shadow.z - 0.001f, tex2D(shadowMap, Shadow.xy + float2( o,  0)).x);
	x += step(Shadow.z - 0.001f, tex2D(shadowMap, Shadow.xy + float2( o,  o)).x);
	return saturate(clip + 0.5f + x / 18);
	//float xx = step(Shadow.z - 0.001f, tex2D(shadowMap, Shadow.xy + float2( 0,  0)).x);
	//float xy = step(Shadow.z - 0.001f, tex2D(shadowMap, Shadow.xy + float2( 0,  o)).x);
	//float yx = step(Shadow.z - 0.001f, tex2D(shadowMap, Shadow.xy + float2( o,  0)).x);
	//float yy = step(Shadow.z - 0.001f, tex2D(shadowMap, Shadow.xy + float2( o,  o)).x);
	//return lerp(0.5f, 1.0f, lerp(lerp(xx, yx, frac(Shadow.x * s)), lerp(xy, yy, frac(Shadow.x * s)), frac(Shadow.y * s)));
}

float4 PSImage(VERTEX_OUTPUT In) : COLOR
{
	float4 surfColor = tex2D(imageMap, In.TexCoords);

	if( In.Normal_Light.w > 0.5 )   // prevent shadows from casting on dark side ( side facing away from light ) of objects
		surfColor.rgb *= GetShadowMap(In.Shadow);

	surfColor.rgb *= In.Normal_Light.w * 0.65 + 0.4; //Brightness + Ambient;
	float4 litColor = surfColor;

	// TODO: Specular lighting goes here.

	if (!isNight_Tex)
	{
		// Darken at night unless using a night texture
		surfColor.rgb *= Day2Night(); 
		// Reduce saturaton when overcast
		surfColor.rgb = Overcast(surfColor.rgb, 1 - overcast);
	}

	// Headlight effect
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
	surfColor.rgb += shading * litColor;
	surfColor.rgb = lerp(surfColor, float4(FogColor, 1), In.LightDir_Fog.w);
	return surfColor;
}

float4 PSVegetation(VERTEX_OUTPUT In) : COLOR
{ 
	float4 surfColor = tex2D(imageMap, In.TexCoords);

	// shadows don't cast on cruciform material ( to prevent visibility of billboard panels )

	float alpha = surfColor.a;
	surfColor.rgb *= 0.8;  
	surfColor.rgb += 0.03;
	float4 litColor = surfColor;

	// Darken at night
	surfColor.rgb *= Day2Night();
	// Reduce saturaton when overcast
	surfColor.rgb = Overcast(surfColor.rgb, 1 - overcast);

	// Headlight effect
	float3 normal = normalize(In.Normal_Light.xyz);
	float3 lightDir = normalize(In.LightDir_Fog.xyz);
	float coneDot = dot(lightDir, normalize(headlightDirection));
	float shading = 0;
	if (coneDot > coneAngle)
	{
		float coneAtten = pow(coneDot, coneDecay * 2.25);
		shading = dot(normal, -lightDir) * lightStrength * coneAtten;
	}
	if (stateChange == 0)
		shading = 0;
	if (stateChange == 1)
		shading *= clamp(fadeTime/fadeinTime, 0, 1);
	if (stateChange == 2)
		shading *= clamp(1-(fadeTime/fadeoutTime), 0, 1);
	surfColor.rgb += shading * litColor;
	surfColor.rgb = lerp(surfColor, float4(FogColor, 1), In.LightDir_Fog.w);
	return surfColor;
}

float4 PSTerrain(VERTEX_OUTPUT In) : COLOR
{ 
	float3 surfColor = tex2D(imageMap, In.TexCoords);

	surfColor.rgb *= GetShadowMap(In.Shadow);

	float3 bump = tex2D(normalMap, In.TexCoords * 50);
	bump -= 0.5;
	surfColor +=  0.5 * bump;
	surfColor *= In.Normal_Light.w * 0.65 + 0.4; //Brightness + Ambient;
	float3 litColor = surfColor;

	// Darken at night
	surfColor *= Day2Night();
	// Reduce saturaton when overcast
	surfColor = Overcast(surfColor, 1 - overcast);

	// Headlight effect
	float3 normal = normalize(In.Normal_Light.xyz);
	float3 lightDir = normalize(In.LightDir_Fog.xyz);
	float coneDot = dot(lightDir, normalize(headlightDirection));
	float shading = 0;
	if (coneDot > coneAngle)
	{
		float coneAtten = pow(coneDot, coneDecay * 3.0);
		shading = dot(normal, -lightDir) * lightStrength * coneAtten;
	}
	if (stateChange == 0)
		shading = 0;
	if (stateChange == 1)
		shading *= clamp(fadeTime / fadeinTime, 0, 1);
	if (stateChange == 2)
		shading *= clamp(1 - (fadeTime / fadeoutTime), 0, 1);
	surfColor += shading * litColor;
	
	return float4(lerp(surfColor, FogColor, In.LightDir_Fog.w), 1);
}

float4 PSDarkShade(VERTEX_OUTPUT In) : COLOR
{ 
	float4 surfColor = tex2D(imageMap, In.TexCoords);
	
	// shadows don't cast on dark shade material - it is already dark

	surfColor.rgb *= 0.2;

	// Darken at night
	surfColor.rgb *= Day2Night();

	// Reduce saturaton when overcast
	surfColor.rgb = Overcast(surfColor.rgb, 1 - overcast);
	surfColor.rgb = lerp(surfColor, float4(FogColor, 1), In.LightDir_Fog.w);
	return surfColor;

}

float4 PSHalfBright(VERTEX_OUTPUT In) : COLOR
{ 
	float shadowMult = GetShadowMap(In.Shadow);
	float4 surfColor = tex2D(imageMap, In.TexCoords);

	surfColor.rgb *= shadowMult;
	surfColor.rgb *= 0.55;
	surfColor.rgb = lerp(surfColor, float4(FogColor, 1), In.LightDir_Fog.w);
	return surfColor;	
}

float4 PSFullBright(VERTEX_OUTPUT In) : COLOR
{ 
	float shadowMult = GetShadowMap(In.Shadow);
	float4 surfColor = tex2D(imageMap, In.TexCoords);

	surfColor.rgb *= shadowMult;
	surfColor.rgb =  lerp(surfColor, float4(FogColor, 1), In.LightDir_Fog.w);
	return surfColor;	
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique Image
{
   pass Pass_0
   {
      VertexShader = compile vs_3_0 VSCommon ( );
      PixelShader = compile ps_3_0 PSImage ( );
   }
}

technique Forest
{
   pass Pass_0
   {
      VertexShader = compile vs_3_0 VSForest ( );
      PixelShader = compile ps_3_0 PSImage ( );
   }
}

technique Vegetation
{
   pass Pass_0
   {
      VertexShader = compile vs_3_0 VSCommon ( );
      PixelShader = compile ps_3_0 PSVegetation ( );
   }
}

technique Terrain
{
   pass Pass_0
   {
      VertexShader = compile vs_3_0 VSCommon ( );
      PixelShader = compile ps_3_0 PSTerrain ( );
   }
}

technique DarkShade
{
   pass Pass_0
   {
      VertexShader = compile vs_3_0 VSCommon ( );
      PixelShader = compile ps_3_0 PSDarkShade ( );
   }
}

technique HalfBright
{
   pass Pass_0
   {
      VertexShader = compile vs_3_0 VSCommon ( );
      PixelShader = compile ps_3_0 PSHalfBright ( );
   }
}

technique FullBright
{
   pass Pass_0
   {
      VertexShader = compile vs_3_0 VSCommon ( );
      PixelShader = compile ps_3_0 PSFullBright ( );
   }
}
