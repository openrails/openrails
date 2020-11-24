// COPYRIGHT 2010, 2011, 2013 by the Open Rails project.
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
//                   P O P U P   W I N D O W   S H A D E R                    //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

float4x4 World;         // model -> world
float4x4 WorldViewProjection;  // model -> world -> view -> projection
float3   GlassColor;
float2   ScreenSize;
texture  ScreenTexture;
texture  WindowTexture;

sampler ScreenSampler = sampler_state
{
	Texture = (ScreenTexture);
	MagFilter = Point;
	MinFilter = Point;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

sampler WindowSampler = sampler_state
{
	Texture = (WindowTexture);
	MagFilter = Point;
	MinFilter = Point;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float4 Position     : POSITION;
	float2 TexCoords    : TEXCOORD0;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position      : POSITION;
	float4 TexCoords_Pos : TEXCOORD0;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

VERTEX_OUTPUT VSPopupWindow(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;

	Out.Position = mul(In.Position, WorldViewProjection);
	Out.TexCoords_Pos.xy = In.TexCoords;

	return Out;
}

VERTEX_OUTPUT VSPopupWindowGlass(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = VSPopupWindow(In);

	Out.TexCoords_Pos.zw = mul(In.Position, World).xy / ScreenSize;

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

float4 PSPopupWindow(in VERTEX_OUTPUT In) : COLOR
{
	float4 Color = tex2D(WindowSampler, In.TexCoords_Pos.xy);
	float Mask = tex2D(WindowSampler, In.TexCoords_Pos.xy + float2(0.5, 0.0)).r;
	float4 ScreenColor = float4(GlassColor, Mask);
	return lerp(ScreenColor, Color, Color.a);
}

float4 PSPopupWindowGlass(in VERTEX_OUTPUT In) : COLOR
{
	float4 Color = tex2D(WindowSampler, In.TexCoords_Pos.xy);
	float Mask = tex2D(WindowSampler, In.TexCoords_Pos.xy + float2(0.5, 0.0)).r;
	float3 ScreenColor = (float3)tex2D(ScreenSampler, In.TexCoords_Pos.zw);
	float3 ScreenColor1 = (float3)tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2(+1 / ScreenSize.x, +1 / ScreenSize.y));
	float3 ScreenColor2 = (float3)tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2(+1 / ScreenSize.x,  0 / ScreenSize.y));
	float3 ScreenColor3 = (float3)tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2(+1 / ScreenSize.x, -1 / ScreenSize.y));
	float3 ScreenColor4 = (float3)tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2( 0 / ScreenSize.x, +1 / ScreenSize.y));
	float3 ScreenColor5 = (float3)tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2( 0 / ScreenSize.x,  0 / ScreenSize.y));
	float3 ScreenColor6 = (float3)tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2( 0 / ScreenSize.x, -1 / ScreenSize.y));
	float3 ScreenColor7 = (float3)tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2(-1 / ScreenSize.x, +1 / ScreenSize.y));
	float3 ScreenColor8 = (float3)tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2(-1 / ScreenSize.x,  0 / ScreenSize.y));
	float3 ScreenColor9 = (float3)tex2D(ScreenSampler, In.TexCoords_Pos.zw + float2(-1 / ScreenSize.x, -1 / ScreenSize.y));
	ScreenColor = lerp(ScreenColor, (22 * GlassColor + ScreenColor + ScreenColor1 + ScreenColor2 + ScreenColor3 + ScreenColor4 + ScreenColor5 + ScreenColor6 + ScreenColor7 + ScreenColor8 + ScreenColor9) / 32, Mask);
	return float4(lerp(ScreenColor, Color.rgb, Color.a), 1);
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

technique PopupWindow {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSPopupWindow();
		PixelShader = compile ps_4_0_level_9_1 PSPopupWindow();
	}
}

technique PopupWindowGlass {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSPopupWindowGlass();
		PixelShader = compile ps_4_0_level_9_1 PSPopupWindowGlass();
	}
}
