//--------------------------------------------------------------//
// LIGHT GLOW SHADER
// Displays train lights
//
// Principal Author: Rick Grout
//--------------------------------------------------------------//

// Values transferred from the game
float4x4 mWorldViewProj;

// Textures
texture lightGlow_Tex;

// Texture settings
sampler lightGlowMap = sampler_state
{
   Texture = <lightGlow_Tex>;
   MAGFILTER = LINEAR;
   MINFILTER = LINEAR;
   MIPFILTER = LINEAR;
   MIPMAPLODBIAS = 0.000000;
   AddressU = Clamp;
   AddressV = Clamp;
};

// Shader Input and Output Structures
//
// Vertex Shader Input
struct VS_IN
{
    float3 Position : POSITION0;
    float3 Normal : NORMAL;
    float3 Color : TEXCOORD0;
    float4 AlphScaleTex : POSITION1;
};

// Vertex Shader Output
struct VS_OUT
{
    float4 Position : POSITION0;
    float2 TexCoords : TEXCOORD0;
    float4 Color : COLOR0;
};


/////////////////////    V E R T E X     S H A D E R S    ////////////////////////////

VS_OUT VSlightGlow( VS_IN In )
{
	VS_OUT Out = ( VS_OUT ) 0;

    float4 color = float4(In.Color, In.AlphScaleTex.x);
    Out.Color = color;
    
    float2 texCoords = (In.AlphScaleTex.zw);
    Out.TexCoords = texCoords;
    
    float scale = In.AlphScaleTex.y;
 
    float3 position = In.Position;
    float3 normal = In.Normal;

 	float3 upVector = float3(0, -1, 0);
    float3 sideVector = normalize(cross(normal, upVector));    
    
    position += (texCoords.x-0.5f) * sideVector * scale;
    position += (0.5f+texCoords.y) * upVector * scale;
   
    Out.Position = mul( mWorldViewProj, float4(position, 1));

    return Out;
}

//////////////////////    P I X E L     S H A D E R S    /////////////////////////////

float4 PSlightGlow( VS_OUT In ) : COLOR
{
	float4 lightColor = tex2D(lightGlowMap, In.TexCoords.xy);
	lightColor *= In.Color;

    return lightColor;
}

////////////////////////////    T E C H N I Q U E S    ///////////////////////////////

technique LightGlow
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VSlightGlow ( );
      PixelShader = compile ps_2_0 PSlightGlow ( );
   }
}
