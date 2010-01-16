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
float ZBias = 0.0;  // TODO TESTING

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
   // Out.pPositionP.z and .w = 0 - far clip plane , ie 0 - 1000
   Out.pPositionP.z += ZBias; 
   Out.pPositionP.w += ZBias;
   
   Out.vNormalW   = normalize(mul(vNormalM,mModelToWorld).xyz);	   // from model space to world space
   Out.uvImageT = uvImageT;	

   Out.distance = length( Out.pPositionP );

   Out.light = dot( Out.vNormalW, LightVector ) *0.5 + 0.5;									

   return Out;
}


/////////////////////    P I X E L     S H A D E R    /////////////////////////////////


float4 PSImage( 
		   float light          : TEXCOORD1,
           float2 uvImageT		: TEXCOORD0,	// in texture space
           float3 vNormalW     : TEXCOORD3 )	// in world space
           : COLOR
{ 

    float4 surfColor = tex2D( imageMap, uvImageT );
    float alpha = surfColor.a;
    surfColor *= light * 0.65 + 0.4; //Brightness + Ambient;
    surfColor.a = alpha;
    return surfColor;
}

float4 PSVegetation( 
		   float light          : TEXCOORD1,
           float2 uvImageT		: TEXCOORD0,	// in texture space
           float3 vNormalW     : TEXCOORD3 )	// in world space
           : COLOR
{ 
	float4 surfColor = tex2D( imageMap, uvImageT );
	float alpha = surfColor.a;
	surfColor *= 0.8;  
	surfColor += 0.03;
	surfColor.a = alpha;
	return surfColor;
}

float4 PSDark( 
		   float light          : TEXCOORD1,
           float2 uvImageT		: TEXCOORD0,	// in texture space
           float3 vNormalW     : TEXCOORD3 )	// in world space
           : COLOR
{ 
	float4 surfColor = tex2D( imageMap, uvImageT );
	float alpha = surfColor.a;
	surfColor *= 0.4;  
	surfColor.a = alpha;
	return surfColor;
}

float4 PSTerrain( 
		   float light          : TEXCOORD1,
		   float distance		: TEXCOORD2,
           float2 uvImageT		: TEXCOORD0,	// in texture space
           float3 vNormalW     : TEXCOORD3 )	// in world space
           : COLOR
{ 

    float3 surfColor = tex2D( imageMap, uvImageT );
    
    distance = clamp(distance,100,500);
    float effect = 100/distance;
    float3 bump = tex2D( normalMap, uvImageT * 50 );
    bump -= 0.5;
	surfColor +=  0.5 * bump;
    surfColor *= light * 0.65 + 0.4; //Brightness + Ambient;
    return float4( surfColor,1);
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

technique Dark  // 4
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 VS ( );
      PixelShader = compile ps_2_0 PSDark ( );
   }

}
