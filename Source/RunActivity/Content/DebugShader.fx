// COPYRIGHT 2013 by the Open Rails project.
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
//                  D E B U G   O V E R L A Y   S H A D E R                   //
////////////////////////////////////////////////////////////////////////////////

////////////////////    G L O B A L   V A L U E S    ///////////////////////////

float2 ScreenSize;
float4 GraphPos; // xy = xy position, zw = width/height
float2 GraphSample; // x = index, y = count

const float2 GraphOffset = float2(10, 10);

////////////////////    V E R T E X   I N P U T S    ///////////////////////////

struct VERTEX_INPUT
{
	float4 Position : POSITION;
	float4 Color    : COLOR0;
};

////////////////////    V E R T E X   O U T P U T S    /////////////////////////

struct VERTEX_OUTPUT
{
	float4 Position : POSITION;  // position x, y, z, w
	float4 Color    : COLOR0;    // color r, g, b, a
};

////////////////////    V E R T E X   S H A D E R S    /////////////////////////

VERTEX_OUTPUT VSGraph(in VERTEX_INPUT In)
{
	VERTEX_OUTPUT Out = (VERTEX_OUTPUT)0;
	
	float x = frac(In.Position.x - GraphSample.x / GraphSample.y + 1);
	Out.Position.x = x * GraphPos.z + GraphPos.x;
	Out.Position.y = In.Position.y * GraphPos.w + GraphPos.y;
	Out.Position.xy /= ScreenSize / 2;
	Out.Position.xy -= 1;
	Out.Position.w = 1;
	Out.Color = In.Color;

	return Out;
}

////////////////////    P I X E L   S H A D E R S    ///////////////////////////

float4 PSGraph(in VERTEX_OUTPUT In) : COLOR0
{
	return In.Color;
}

////////////////////    T E C H N I Q U E S    /////////////////////////////////

////////////////////////////////////////////////////////////////////////////////
// IMPORTANT: ATI graphics cards/drivers do NOT like mixing shader model      //
//            versions within a technique/pass. Always use the same vertex    //
//            and pixel shader versions within each technique/pass.           //
////////////////////////////////////////////////////////////////////////////////

technique Graph {
	pass Pass_0 {
		VertexShader = compile vs_2_0 VSGraph();
		PixelShader = compile ps_2_0 PSGraph();
	}
}
