// COPYRIGHT 2011 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

////////////////////////////////////////////////////////////////////////////////
//         P A R T I C L E   E M I T T E R   O B J E C T   S H A D E R        //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

float4x4 worldViewProjection;  // model -> world -> view -> projection
float4x4 invView;				// inverse view

float3 LightVector; // Direction vector to sun, used for day-night darkening

float emitSize;

float2 cameraTileXY;
float currentTime;

static float2 texCoords[4] = { float2(0, 0), float2(0.25f, 0), float2(0.25f, 0.25f), float2(0, 0.25f) };
static float3 offsets[4] = { float3(-0.5f, 0.5f, 0), float3(0.5f, 0.5f, 0), float3(0.5f, -0.5f, 0), float3(-0.5f, -0.5f, 0) };

float4 Fog;

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
	float4 StartPosition_StartTime : POSITION0;
	float4 InitialVelocity_EndTime : POSITION1;
	float4 TargetVelocity_TargetTime : POSITION2;
	float4 TileXY_Vertex_ID : POSITION3;
	float4 Color_Random : POSITION4;
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

	float age = (currentTime - In.StartPosition_StartTime.w);
	Out.Color_Age.a = age / (In.InitialVelocity_EndTime.w - In.StartPosition_StartTime.w);
	
	float2 tileXY = In.TileXY_Vertex_ID.xy;
	float2 diff = cameraTileXY - tileXY;
	float2 offset = diff * float2(-2048, 2048);
	In.StartPosition_StartTime.xz += offset;
	
	float velocityAge = clamp(age, 0, In.TargetVelocity_TargetTime.w);
	In.StartPosition_StartTime.xyz += In.InitialVelocity_EndTime.xyz * velocityAge;
	In.StartPosition_StartTime.xyz += (In.TargetVelocity_TargetTime.xyz - In.InitialVelocity_EndTime.xyz) / In.TargetVelocity_TargetTime.w * velocityAge * velocityAge / 2;
	In.StartPosition_StartTime.xyz += In.TargetVelocity_TargetTime.xyz * clamp(age - In.TargetVelocity_TargetTime.w, 0, age);
	
	float particleSize = (emitSize * 2) * (1 + age * 4);  // Start off at emitSize and increases in size.
	
	int vertIdx = (int)In.TileXY_Vertex_ID.z;
	
	float3 right = invView[0].xyz;
	float3 up = invView[1].xyz;
	
	float2x2 rotMatrix = GetRotationMatrix(age, In.Color_Random.a);	
	float3 vertOffset = offsets[vertIdx] * particleSize;
	vertOffset.xy = mul(vertOffset.xy, rotMatrix);
	In.StartPosition_StartTime.xyz += right * vertOffset.x;
	In.StartPosition_StartTime.xyz += up * vertOffset.y;
	
	Out.Position = mul(float4(In.StartPosition_StartTime.xyz, 1), worldViewProjection);
	
	Out.TexCoord = texCoords[vertIdx];
	float texAtlasPosition = In.TileXY_Vertex_ID.w;
	int atlasX = texAtlasPosition % 4;
	int atlasY = texAtlasPosition / 4;
	Out.TexCoord += float2(0.25f * atlasX, 0.25f * atlasY);

	Out.Color_Age.rgb = In.Color_Random.rgb;

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

// This function dims the lighting at night, with a transition period as the sun rises/sets.
void _PSApplyDay2Night(inout float3 Color)
{
	// The following constants define the beginning and the end conditions of the day-night transition
	const float startNightTrans = 0.1; // The "NightTrans" values refer to the Y postion of LightVector
	const float finishNightTrans = -0.1;
	const float minDarknessCoeff = 0.03;
	
	// Internal variables
	// The following two are used to interpoate between day and night lighting (y = mx + b)
	// Can't use lerp() here, as overall dimming action is too complex
	float slope = (1.0 - minDarknessCoeff) / (startNightTrans - finishNightTrans); // "m"
	float incpt = 1.0 - slope * startNightTrans; // "b"
	// This is the return value used to darken scenery
	float adjustment;
	
	Color.rgb = lerp(Color.rgb, Fog.rgb, Fog.a);
	
	if (LightVector.y < finishNightTrans)
		adjustment = minDarknessCoeff;
	else if (LightVector.y > startNightTrans)
		adjustment = 1.0; // Scenery is fully lit during the day
	else
		adjustment = slope * LightVector.y + incpt;
	
	Color.rgb *= adjustment;
}

float4 PSParticles(in VERTEX_OUTPUT In) : COLOR0
{
	clip(In.Color_Age.a);
	
	float alpha = (1 - In.Color_Age.a);
	float4 tex = tex2D(ParticleSamp, In.TexCoord);
	tex.rgb *= In.Color_Age.rgb;
	_PSApplyDay2Night(tex.rgb);
	tex.a *= alpha;
	return tex;
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique ParticleEmitterTechnique
{
	pass Pass_0
	{
		VertexShader = compile vs_4_0_level_9_1 VSParticles();
		PixelShader = compile ps_4_0_level_9_1 PSParticles();
	}
}
