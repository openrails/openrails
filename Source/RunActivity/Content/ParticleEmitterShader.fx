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

static float2 texCoords[4] = { float2(0, 0), float2(1.0f, 0), float2(1.0f, 1.0f), float2(0, 1.0f) };
static float3 offsets[4] = { float3(-0.5f, 0.5f, 0), float3(0.5f, 0.5f, 0), float3(0.5f, -0.5f, 0), float3(-0.5f, -0.5f, 0) };

float4 Fog;

// Textures
texture particle_Tex;
float2 texAtlasSize;

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
    float4 Expansion_Rotation : POSITION4;
	float4 Color : POSITION5;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position	: POSITION;
	float2 TexCoord : TEXCOORD0;
	float4 Color : TEXCOORD1;
};

struct PIXEL_INPUT
{
	float2 TexCoord : TEXCOORD0;
	float4 Color : TEXCOORD1;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

float2x2 GetRotationMatrix(float age, float init, float rate)
{
    // "age" here represents the rotation angle in radians
	age = init + age * rate;
	float c, s;
	sincos(age, c, s);
	return float2x2(c, -s, s, c);
}

// Particle vertex shader.
VERTEX_OUTPUT VSParticles(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	float age = (currentTime - In.StartPosition_StartTime.w);

    // Reduce particle opacity over time
	Out.Color.a = (In.Color.a) * (1 - (age / (In.InitialVelocity_EndTime.w - In.StartPosition_StartTime.w)));
	
	float2 tileXY = In.TileXY_Vertex_ID.xy;
	float2 diff = cameraTileXY - tileXY;
	float2 offset = diff * float2(-2048, 2048);
	In.StartPosition_StartTime.xz += offset;
	
    // Calculate age of particle limited between 0 and the TargetTime
	float velocityAge = clamp(age, 0, In.TargetVelocity_TargetTime.w);

    // Modification of velocityAge such that it still goes from 0 to TargetTime, but follows a cubic polynomial curve rather than linear
    float velocityAgeReverse = velocityAge - In.TargetVelocity_TargetTime.w;
	float velocityAgeCubic = (velocityAgeReverse * velocityAgeReverse * velocityAgeReverse) / (In.TargetVelocity_TargetTime.w * In.TargetVelocity_TargetTime.w) + In.TargetVelocity_TargetTime.w;

    // Assuming acceleration decreases linearly over the duration of the TargetTime, then...
    // P(t) = dA/dt * t^3 / 6 + A(0) * t^2 / 2 + V(0) * t + P(0)    -when-  t > 0 and t < TargetTime
    // A(0) = 2 * (FinalVelocity - InitialVelocity) / TargetTime    -and-   dA/dt = - A(0) / TargetTime
    float3 accelFactor = (In.TargetVelocity_TargetTime.xyz - In.InitialVelocity_EndTime.xyz) / In.TargetVelocity_TargetTime.w * velocityAge; // This is A(0) * t / 2

	In.StartPosition_StartTime.xyz += velocityAge *
                                      (In.InitialVelocity_EndTime.xyz +                                                         // Initial velocity contribution
                                      accelFactor +                                                                             // Initial acceleration contribution
                                      (- accelFactor / In.TargetVelocity_TargetTime.w) * velocityAge / 3) +                     // Linear acceleration contribution
                                      In.TargetVelocity_TargetTime.xyz * clamp(age - In.TargetVelocity_TargetTime.w, 0, age);   // Final velocity contribution
	
    // Start off at emitSize and increases in size, with a rapid parabolic increase from 0 to target time, then slower increase with overall age
	float particleSize = max(0, (emitSize * 2) * (1 + velocityAgeCubic / In.TargetVelocity_TargetTime.w * In.Expansion_Rotation.x + age * In.Expansion_Rotation.y));

    // Increase height of particles as they expand to avoid clipping through ground
    In.StartPosition_StartTime.y += particleSize / 2 - emitSize;
	
	int vertIdx = (int)In.TileXY_Vertex_ID.z;
	
	float3 right = invView[0].xyz;
	float3 up = invView[1].xyz;
	
	float2x2 rotMatrix = GetRotationMatrix(age, In.Expansion_Rotation.z, In.Expansion_Rotation.w);	
	float3 vertOffset = offsets[vertIdx] * particleSize;
	vertOffset.xy = mul(vertOffset.xy, rotMatrix);
	In.StartPosition_StartTime.xyz += right * vertOffset.x;
	In.StartPosition_StartTime.xyz += up * vertOffset.y;
	
	Out.Position = mul(float4(In.StartPosition_StartTime.xyz, 1), worldViewProjection);
	
	Out.TexCoord = texCoords[vertIdx];
	float texAtlasPosition = In.TileXY_Vertex_ID.w;
	int atlasX = texAtlasPosition % texAtlasSize.x;
	int atlasY = texAtlasPosition / texAtlasSize.y;
    Out.TexCoord.x /= texAtlasSize.x;
    Out.TexCoord.y /= texAtlasSize.y;
	Out.TexCoord += float2(atlasX / texAtlasSize.x, atlasY / texAtlasSize.y);

	Out.Color.rgb = In.Color.rgb;

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
    // Don't render any pixel if calculated alpha is greater than 1 (indicates particle spawned too soon)
    clip(1 - In.Color.a);
	
	float4 tex = tex2D(ParticleSamp, In.TexCoord);
	tex.rgb *= In.Color.rgb;
	_PSApplyDay2Night(tex.rgb);
	tex.a *= In.Color.a;
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
