// COPYRIGHT 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

////////////////////////////////////////////////////////////////////////////////
//         P A R T I C L E   E M I T T E R   O B J E C T   S H A D E R        //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

float4x4 worldViewProjection;  // model -> world -> view -> projection
float4x4 invView;				// inverse view

float3 emitDirection;
float emitSize;

float2 cameraTileXY;
float currentTime;

static float2 texCoords[4] = { float2(0, 0), float2(0.25f, 0), float2(0.25f, 0.25f), float2(0, 0.25f) };
static float3 offsets[4] = { float3(-0.5f, 0.5f, 0), float3(0.5f, 0.5f, 0), float3(0.5f, -0.5f, 0), float3(-0.5f, -0.5f, 0) };

// Textures
texture particle_Tex;

// Texture settings
sampler ParticleSamp = sampler_state
{
	texture = (particle_Tex);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
	AddressU = Clamp;
	AddressV = Clamp;
};

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float4 Position : POSITION0;
	float4 TileXY_Idx_AtlasPosition : POSITION1;
	float4 Color_Random : POSITION2;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position	: POSITION;
	float2 TexCoord : TEXCOORD0;
	float4 Color_Age : TEXCOORD1;
};

struct PIXEL_INPUT
{
	float2 TexCoord : TEXCOORD0;
	float4 Color_Age : TEXCOORD1;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

float2x2 GetRotationMatrix(float age, float random)
{
	random = (random * 2) - 1;
	age *= random * 0.25f;
	float c, s;
	sincos(age, c, s);
	return float2x2(c, -s, s, c);
}

// Particle vertex shader.
VERTEX_OUTPUT VSParticles(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	float particleSpawnTime = In.Position.w;	
	Out.Color_Age.a = (currentTime - particleSpawnTime);
	
	float2 tileXY = In.TileXY_Idx_AtlasPosition.xy;
	float2 diff = cameraTileXY - tileXY;
	float2 offset = diff * float2(-2048, 2048);
	In.Position.xz += offset;
	
	In.Position.xyz += (emitDirection * Out.Color_Age.a * 3);	// Constant velocity for now.
	
	Out.Color_Age.a *= 4;
	float particleSize = (emitSize * 2) * (1 + Out.Color_Age.a);	// Start off at emitSize and increases in size.
	
	int vertIdx = (int)In.TileXY_Idx_AtlasPosition.z;
	
	float3 right = invView[0].xyz;
	float3 up = invView[1].xyz;
	
	float2x2 rotMatrix = GetRotationMatrix(Out.Color_Age.a, In.Color_Random.a);	
	float3 vertOffset = offsets[vertIdx] * particleSize;
	vertOffset.xy = mul(vertOffset.xy, rotMatrix);
	In.Position.xyz += right * vertOffset.x;
	In.Position.xyz += up * vertOffset.y;
	
	Out.Position = mul(float4(In.Position.xyz, 1), worldViewProjection);
	
	Out.TexCoord = texCoords[vertIdx];
	int texAtlasPosition = In.TileXY_Idx_AtlasPosition.w;
	int atlasX = texAtlasPosition % 4;
	int atlasY = texAtlasPosition / 4;
	Out.TexCoord += float2(0.25f * atlasX, 0.25f * atlasY);

	Out.Color_Age.rgb = In.Color_Random.rgb;

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

float4 PSParticles(in PIXEL_INPUT In) : COLOR0
{
	float normalizedAge = saturate(In.Color_Age.a / 12);
	float alpha = (1 - normalizedAge);
	
	float4 tex = tex2D(ParticleSamp, In.TexCoord);
	tex.rgb += In.Color_Age.rgb;
	tex.rgb /= 2;
	tex.rgb = min(tex.rgb, In.Color_Age.rgb);
	tex.a -= 0.033f;	// Get rid of the non zero edge on the texture. No idea why it's there.
	tex.a *= alpha;
	return tex;
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique ParticleEmitterTechnique
{
	pass Pass_0
	{
		VertexShader = compile vs_2_0 VSParticles();
		PixelShader = compile ps_2_0 PSParticles();
	}
}
