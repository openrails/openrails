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
//                     L I G H T   C O N E   S H A D E R                      //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

// General values
float4x4 WorldViewProjection;  // model -> world -> view -> projection

float2 Fade; // overall fade (0 = off, 1 = on); transition fade (0 = original, 1 = transition)

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float3 PositionO : POSITION0; // original position x, y, z
	float3 PositionT : POSITION1; // transition position x, y, z
	float4 ColorO    : COLOR0;    // original color r, g, b, a
	float4 ColorT    : COLOR1;    // transition color r, g, b, a
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position  : POSITION0;
	float4 Color     : COLOR0;
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

VERTEX_OUTPUT VSLightCone(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
    Out.Position = mul(float4(lerp(In.PositionO, In.PositionT, Fade.y), 1), WorldViewProjection);
    Out.Color = lerp(In.ColorO, In.ColorT, Fade.y);
    Out.Color.a *= Fade.x * 0.1;
	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

float4 PSLightCone(in VERTEX_OUTPUT In) : COLOR0
{
	return In.Color;
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// IMPORTANT: ATI graphics cards/drivers do NOT like mixing shader model      //
//            versions within a technique/pass. Always use the same vertex    //
//            and pixel shader versions within each technique/pass.           //
////////////////////////////////////////////////////////////////////////////////

technique LightCone {
	pass Pass_0 {
		VertexShader = compile vs_4_0_level_9_1 VSLightCone();
		PixelShader = compile ps_4_0_level_9_1 PSLightCone();
	}
}
