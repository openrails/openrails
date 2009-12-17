//--------------------------------------------------------------//
// SCENERY OBJECT SHADER 
//--------------------------------------------------------------//
//--------------------------------------------------------------//
// Pass 0
//--------------------------------------------------------------//


float4x4 mModelToProjection : ViewProjection;	// SetValueTranspose((world * view) * projection);  
float4x4 mWorldToView  : ViewInverse;			// SetValue(Matrix.Invert(view));
float4x4 mModelToWorld : WorldMatrix;			// SetValue(world);

float3 LightVector = float3( 0.5 ,1,0.5 );  // direction vector to light
float3 BumpScale = float3( 1.0, -1.0, 1.0 );  // multiply bump map by this  -1 seems to work with Ultimapper sometimes

float Saturation = 0.9;
float Ambient = 0.5;
float Brightness = 0.7;

texture imageMap_Tex;
sampler imageMap = sampler_state
{
   Texture = (imageMap_Tex);
   MAGFILTER = LINEAR;
   MINFILTER = LINEAR;
   MIPFILTER = LINEAR;
   MIPMAPLODBIAS = 0.000000;
   AddressU = Wrap;
   AddressV = Wrap;
};

texture normalMap_Tex;
sampler normalMap = sampler_state
{
   Texture = (normalMap_Tex);
   MAGFILTER =  LINEAR;
   MINFILTER =  LINEAR;
   MIPFILTER =  LINEAR;
   MIPMAPLODBIAS = 0.000000;
   AddressU = Wrap;
   AddressV = Wrap;
};


/////////////////////    V E R T E X     S H A D E R    /////////////////////////////////

struct VS_OUTPUT
{
   float  light      : TEXCOORD1;
   float  distance   : TEXCOORD2;
   float4 pPositionP : POSITION;    // in projection space
   float2 uvImageT	 : TEXCOORD0;   // in texture space
   float3 vNormalW   : TEXCOORD3;	// in world space
};

VS_OUTPUT VS(   float4 pPositionM : POSITION,	// in model space
				float3 vNormalM   : NORMAL,		// in model space
				float2 uvImageT   : TEXCOORD0	// in texture space
			)	
{
   VS_OUTPUT Out = (VS_OUTPUT) 0; 

   Out.pPositionP = mul( mModelToProjection, pPositionM );		// shift point position from model space to projection space
   Out.vNormalW   = normalize(mul(vNormalM,mModelToWorld).xyz);	   // from model space to world space
   Out.uvImageT = uvImageT;	
   
   Out.distance = length( Out.pPositionP );
   
   Out.light = dot( Out.vNormalW, LightVector ) *0.5 + 0.5;									

   return Out;
}


/////////////////////    P I X E L     S H A D E R    /////////////////////////////////

float4 AdjustSaturation( float4 color )
{
	float level = (color.x + color.y + color.z) / 3;
	color.r *= 0.95;
	color.g *= 0.95;
	color.b *= 1.1;
	return lerp( float4( level,level,level, color.w ), color, level * 0.25 + 0.75 );   // 0 is no saturation, 1 is full saturation	
	//return color;
}

float4 PSImage( 
		   float light          : TEXCOORD1,
           float2 uvImageT		: TEXCOORD0,	// in texture space
           float3 vNormalW     : TEXCOORD3 )	// in world space
           : COLOR
{ 

    float4 surfColor = tex2D( imageMap, uvImageT );
    float alpha = surfColor.a;
    surfColor *= light * 0.85 + 0.4; //Brightness + Ambient;
    surfColor.a = alpha;
    return AdjustSaturation( surfColor );
}

float4 PSVegetation( 
		   float light          : TEXCOORD1,
           float2 uvImageT		: TEXCOORD0,	// in texture space
           float3 vNormalW     : TEXCOORD3 )	// in world space
           : COLOR
{ 
	float4 surfColor = tex2D( imageMap, uvImageT );
	surfColor *= 0.7 + 0.5;   //Brightness + Ambient;
	return AdjustSaturation(surfColor);
}


float4 PSTerrain( 
		   float light          : TEXCOORD1,
		   float distance		: TEXCOORD2,
           float2 uvImageT		: TEXCOORD0,	// in texture space
           float3 vNormalW     : TEXCOORD3 )	// in world space
           : COLOR
{ 

    float3 surfColor = tex2D( imageMap, uvImageT );
    
    //distance = clamp(distance,10,100);
    float effect = 10/distance;
    float3 bump = tex2D( normalMap, uvImageT * 50 ) - 0.5;
	surfColor *=  1.0 + effect * 2.0 * bump;
    surfColor *= light * 0.85 + 0.4; //Brightness + Ambient;
    return AdjustSaturation(float4( surfColor,1));
}

float4 PSSky( 
		   float light          : TEXCOORD1,
           float2 uvImageT		: TEXCOORD0,	// in texture space
           float3 vNormalW     : TEXCOORD3 )	// in world space
           : COLOR
{ 
	float4 surfColor = tex2D( imageMap, uvImageT );
	return surfColor;
}

technique Image   //0
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VS ( );
      PixelShader = compile ps_2_0 PSImage ( );
   }

}

technique Vegetation  // 1
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VS ( );
      PixelShader = compile ps_2_0 PSVegetation ( );
   }

}

technique Terrain   // 2
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VS ( );
      PixelShader = compile ps_2_0 PSTerrain ( );
   }

}

technique Sky   // 3
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VS ( );
      PixelShader = compile ps_2_0 PSSky ( );
   }
}
