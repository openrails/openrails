////////////////////////////////////////////////////////////////////////////////
//           P R E C I P I T A T I O N   O B J E C T   S H A D E R            //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

float4x4 mProjection;
float4x4 mWorld;
float4x4 mView;
float3 LightVector; // Direction vector to sun, used for day-night darkening
int viewportHeight;
double currentTime;
int weatherType;

// Textures
texture precip_Tex;

// Texture settings
sampler PrecipSamp = sampler_state
{
	texture = (precip_Tex);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
	MipMapLodBias = 0;
	AddressU = Wrap;
	AddressV = Wrap;
};

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float4 Position : POSITION;
	float  Size     : PSIZE;
	float  Time     : TEXCOORD0;
	float2 Wind     : TEXCOORD1;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position	: POSITION;
	float  Size	    : PSIZE;
	float2 TexCoord : TEXCOORD0;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

// Precipitation vertex shader - calculates raindrop/snowflake positions.
VERTEX_OUTPUT VSPrecipitation(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	Out.Position = mul(In.Position, mWorld);

	// Particle age determines Y position
	float age = currentTime - In.Time;
	// Wind effect
	Out.Position.xy += age * In.Wind;
	// Vertical velocity
	float velocity;
	if (weatherType == 1) // snow
		velocity = 8.0f;
	if (weatherType == 2) // rain
	{
		velocity = 20.0f;
		In.Size *= 1.4;
	}
	Out.Position.y -= age * velocity;

	Out.Position = mul(mul(Out.Position, mView), mProjection);

	// Calculate size based on distance from viewer
	Out.Size = In.Size * mProjection._m11 / Out.Position.w * viewportHeight / 4;

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

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

float4 PSPrecipitation(in VERTEX_OUTPUT In) : COLOR0
{
	float4 color = tex2D(PrecipSamp, In.TexCoord);
	_PSApplyDay2Night(color);
	return color;
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique RainTechnique
{
	pass Pass_0
	{
		VertexShader = compile vs_2_0 VSPrecipitation();
		PixelShader = compile ps_2_0 PSPrecipitation();
	}
}
