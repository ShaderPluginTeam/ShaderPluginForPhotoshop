<Shader>
	<AutoCompileAfterLoading>true</AutoCompileAfterLoading>
	<AutoPlayAfterLoading>true</AutoPlayAfterLoading>
	<UseMipMaps>false</UseMipMaps>
	<MultiPassBuffers>NoBuffers</MultiPassBuffers>
	<CommonCode>uint ihash1D(uint q)
{
    // hash by Hugo Elias, Integer Hash - I, 2017
    q = q * 747796405u + 2891336453u;
    q = (q &lt;&lt; 13u) ^ q;
    return q * (q * q * 15731u + 789221u) + 1376312589u;
}

uvec2 ihash1D(uvec2 q)
{
    // hash by Hugo Elias, Integer Hash - I, 2017
    q = q * 747796405u + 2891336453u;
    q = (q &lt;&lt; 13u) ^ q;
    return q * (q * q * 15731u + 789221u) + 1376312589u;
}

uvec4 ihash1D(uvec4 q)
{
    // hash by Hugo Elias, Integer Hash - I, 2017
    q = q * 747796405u + 2891336453u;
    q = (q &lt;&lt; 13u) ^ q;
    return q * (q * q * 15731u + 789221u) + 1376312589u;
}

// @return Value of the noise, range: [0, 1]
float hash1D(vec2 x)
{
    // hash by Inigo Quilez, Integer Hash - III, 2017
    uvec2 q = uvec2(x * 8192.0);
    q = 1103515245u * ((q &gt;&gt; 1u) ^ q.yx);
    uint n = 1103515245u * (q.x ^ (q.y &gt;&gt; 3u));
    return float(n) * (1.0 / float(0xffffffffu));
}

// generates 2 random numbers for the coordinate
vec2 Hash2D(vec2 x)
{
    uvec2 q = uvec2(x);
    uint h0 = ihash1D(ihash1D(q.x) + q.y);
    uint h1 = h0 * 1933247u + ~h0 ^ 230123u;
    return vec2(h0, h1)  * (1.0 / float(0xffffffffu));
}

// generates 2 random numbers for each of the 2D coordinates
vec4 Hash2D(vec2 coords0, vec2 coords1)
{
    uvec4 i = uvec4(coords0, coords1);
    uvec4 hash = ihash1D(ihash1D(i.xz) + i.yw).xxyy;
    hash.yw = hash.yw * 1933247u + ~hash.yw ^ 230123u;
    return vec4(hash) * (1.0 / float(0xffffffffu));;
}

// generates a random number for each of the 4 cell corners
vec4 Hash2D(vec4 cell)    
{
    uvec4 i = uvec4(cell);
    uvec4 hash = ihash1D(ihash1D(i.xzxz) + i.yyww);
    return vec4(hash) * (1.0 / float(0xffffffffu));
}

// generates 2 random numbers for each of the four 2D coordinates
void Hash2D(vec4 coords0, vec4 coords1, out vec4 hashX, out vec4 hashY)
{
    uvec4 hash0 = ihash1D(ihash1D(uvec4(coords0.xz, coords1.xz)) + uvec4(coords0.yw, coords1.yw));
    uvec4 hash1 = hash0 * 1933247u + ~hash0 ^ 230123u;
    hashX = vec4(hash0) * (1.0 / float(0xffffffffu));
    hashY = vec4(hash1) * (1.0 / float(0xffffffffu));
}

// @return Value of the noise, range: [0, 1]
vec3 hash3D(vec2 x) 
{
    // based on: pcg3 by Mark Jarzynski: http://www.jcgt.org/published/0009/03/02/
    uvec3 v = uvec3(x.xyx * 8192.0) * 1664525u + 1013904223u;
    v += v.yzx * v.zxy;
    v ^= v &gt;&gt; 16u;

    v.x += v.y * v.z;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    return vec3(v) * (1.0 / float(0xffffffffu));
}</CommonCode>
	<Image>
		<Shader_VS>#version 330

// Region Common code include
#extension GL_ARB_shading_language_include : enable

#ifdef GL_ARB_shading_language_include
#include &lt;/common.glsl&gt;
#endif
// EndRegion Common code include

// Region Uniforms
uniform sampler2D TextureUnit0; // Original Image
uniform sampler2D TextureUnit1; // Buffer A (if available)
uniform sampler2D TextureUnit2; // Buffer B (if available)
uniform sampler2D TextureUnit3; // Buffer C (if available)
uniform sampler2D TextureUnit4; // Buffer D (if available)

uniform vec3	iColorBG;		// Photoshop Background Color [0..1]
uniform vec3	iColorFG;		// Photoshop Foreground Color [0..1]
uniform vec2	iImageSize;		// Image Size [vec2, ivec2]
uniform vec2	iViewSize;		// Viewport Size, also can use iResolution. [vec2, ivec2]
uniform vec4	iRandom;		// Random values [0 .. 1]
uniform vec4	iDate;			// Year, Month, Day, Time in seconds
uniform float	iTime;			// Running time (sec)
uniform float	iTimeDelta;		// Frame render time (sec)
uniform float	iFrameRate;		// Frame rate (FPS) [float, int]
uniform int		iFrame;			// Frame Number [float, int]
uniform vec4	iMouse;			// xy: Current position (if LMB Pressed), zw: Pressed position (signs: move, one frame). [vec4, ivec4]
uniform vec2	iMouseCoords;	// Current Mouse Position, update every frame [vec2, ivec2].
uniform uint	iKeyStates[24];	// Key codes in bits: Pressed [0 .. 7], One frame press [8 - 15], Toggle [16 - 23].
// EndRegion Uniforms

layout(location = 0) in vec2 v_UV;

out vec2 f_UV;

void main()
{
	f_UV = v_UV;
	vec2 Position = v_UV * 2.0 - 1.0;
	gl_Position = vec4(Position, 0.0, 1.0);
}</Shader_VS>
		<Shader_FS>#version 330

// Region Common code include
#extension GL_ARB_shading_language_include : enable

#ifdef GL_ARB_shading_language_include
#include &lt;/common.glsl&gt;
#endif
// EndRegion Common code include

// Region Uniforms
uniform sampler2D TextureUnit0; // Original Image
uniform sampler2D TextureUnit1; // Buffer A (if available)
uniform sampler2D TextureUnit2; // Buffer B (if available)
uniform sampler2D TextureUnit3; // Buffer C (if available)
uniform sampler2D TextureUnit4; // Buffer D (if available)

uniform vec3	iColorBG;		// Photoshop Background Color [0..1]
uniform vec3	iColorFG;		// Photoshop Foreground Color [0..1]
uniform vec2	iImageSize;		// Image Size [vec2, ivec2]
uniform vec2	iViewSize;		// Viewport Size, also can use iResolution. [vec2, ivec2]
uniform vec4	iRandom;		// Random values [0 .. 1]
uniform vec4	iDate;			// Year, Month, Day, Time in seconds
uniform float	iTime;			// Running time (sec)
uniform float	iTimeDelta;		// Frame render time (sec)
uniform float	iFrameRate;		// Frame rate (FPS) [float, int]
uniform int		iFrame;			// Frame Number [float, int]
uniform vec4	iMouse;			// xy: Current position (if LMB Pressed), zw: Pressed position (signs: move, one frame). [vec4, ivec4]
uniform vec2	iMouseCoords;	// Current Mouse Position, update every frame [vec2, ivec2].
uniform uint	iKeyStates[24];	// Key codes in bits: Pressed [0 .. 7], One frame press [8 - 15], Toggle [16 - 23].
// EndRegion Uniforms

in vec2 f_UV;

layout(location = 0) out vec4 FragColor;

// https://github.com/tuxalin/procedural-tileable-shaders
// License: MIT

// Voronoi with the distance from edges.
// @param scale Number of tiles, must be  integer for tileable results, range: [2, inf]
// @param jitter Jitter factor for the voronoi cells, if zero then it will result in a square grid, range: [0, 1], default: 1.0
// @param phase The phase for rotating the cells, range: [0, inf], default: 0.0
// @param seed Seed to randomize result, range: [0, inf], default: 0.0
// @return Returns the distance from the cell edges, yz = tile position of the cell, range: [0, 1]
vec3 voronoi(vec2 pos, vec2 scale, float jitter, float phase, float seed)
{
     // voronoi based on Inigo Quilez: https://archive.is/Ta7dm
    const float kPI2 = 6.2831853071;
    pos *= scale;
    vec2 i = floor(pos);
    vec2 f = pos - i;

    // first pass
    vec2 minPos, tilePos;
    float minDistance = 1e+5;
    for (int k=0; k&lt;8; k+=2)
    {
        ivec2 k1 = ivec2(k, k + 1);
        ivec2 ky = k1 / 3;
        vec4 n = vec4(k1 - ky * 3, ky).xzyw - 1.0;
        
        vec4 ni = mod(i.xyxy + n, scale.xyxy) + seed;
        vec4 cPos = Hash2D(ni.xy, ni.zw) * jitter;
        cPos = 0.5 * sin(phase + kPI2 * cPos) + 0.5;
        vec4 rPos = n + cPos - f.xyxy;

        vec4 temp = rPos * rPos;
        temp.xy = temp.xz + temp.yw;
        vec4 minResult = temp.x &lt; temp.y ? vec4(rPos.xy, cPos.xy) : vec4(rPos.zw, cPos.zw);
        float d = min(temp.x, temp.y);
        if(d &lt; minDistance)
        {
            minDistance = d;
            minPos = minResult.xy;
            tilePos = minResult.zw;
        }
    }
    // last cell
    {
        vec2 n = vec2(1.0);
        vec2 ni = mod(i.xy + n, scale) + seed;
        vec2 cPos = Hash2D(ni) * jitter;
        cPos = 0.5 * sin(phase + kPI2 * cPos) + 0.5;
        vec2 rPos = n + cPos - f;

        float d = dot(rPos, rPos);
        if(d &lt; minDistance)
        {
            minDistance = d;
            minPos = rPos;
            tilePos = cPos;
        }
    }

    // second pass, distance to edges
    minDistance = 1e+5;
    for (int y=-2; y&lt;=2; y++)
    {
        for (int x=-2; x&lt;=2; x+=2)
        { 
            vec4 n = vec4(x, y, x + 1, y);
            vec4 ni = mod(i.xyxy + n, scale.xyxy) + seed;
            vec4 cPos = Hash2D(ni.xy, ni.zw) * jitter;
            cPos = 0.5 * sin(phase + kPI2 * cPos) + 0.5;
            vec4 rPos = n + cPos - f.xyxy;
            
            // compute the perpendicular distance
            vec4 temp = minPos.xyxy - rPos;
            temp *= temp;
            vec2 l  = temp.xz + temp.yw;
            vec4 a = 0.5 * (minPos.xyxy + rPos);
            vec4 b = rPos - minPos.xyxy;
            temp = b * b;
            b /= sqrt(temp.xz + temp.yw).xxyy;
            
            temp = a * b;
            vec2 d = temp.xz + temp.yw; 
            if(l.x &gt; 1e-5) 
                minDistance = min(minDistance, d.x);
            if(l.y &gt; 1e-5) 
                minDistance = min(minDistance, d.y);
        }      
    }

    return vec3(minDistance, tilePos);
}

// Voronoi with the position and minimum distance.
// @param scale Number of tiles, must be  integer for tileable results, range: [2, inf]
// @param jitter Jitter factor for the voronoi cells, if zero then it will result in a square grid, range: [0, 1], default: 1.0
// @param phase The phase for rotating the cells, range: [0, inf], default: 0.0
// @param seed Seed to randomize result, range: [0, inf], default: 0.0
// @return Returns the distance from the cell edges, yz = tile position of the cell, range: [0, 1]
vec3 voronoiPosition(vec2 pos, vec2 scale, float jitter, float phase, float seed)
{
    const float kPI2 = 6.2831853071;
    pos *= scale;
    vec2 i = floor(pos);
    vec2 f = pos - i;

    // first pass
    vec2 tilePos;
    float minDistance = 1e+5;
    for (int k=0; k&lt;8; k+=2)
    {
        ivec2 k1 = ivec2(k, k + 1);
        ivec2 ky = k1 / 3;
        vec4 n = vec4(k1 - ky * 3, ky).xzyw - 1.0;
        
        vec4 ni = mod(i.xyxy + n, scale.xyxy) + seed;
        vec4 cPos = Hash2D(ni.xy, ni.zw) * jitter;
        cPos = 0.5 * sin(phase + kPI2 * cPos) + 0.5;
        vec4 rPos = n + cPos - f.xyxy;

        vec4 temp = rPos * rPos;
        temp.xy = temp.xz + temp.yw;

        vec3 minResult = temp.x &lt; temp.y ? vec3(cPos.xy, temp.x) : vec3(cPos.zw, temp.y);
        float d = minResult.z;
        if(d &lt; minDistance)
        {
            minDistance = d;
            tilePos = minResult.xy;
        }
    }
    // last cell
    {
        vec2 n = vec2(1.0);
        vec2 ni = mod(i.xy + n, scale) + seed;
        vec2 cPos = Hash2D(ni) * jitter;
        cPos = 0.5 * sin(phase + kPI2 * cPos) + 0.5;
        vec2 rPos = n + cPos - f;

        float d = dot(rPos, rPos);
        if(d &lt; minDistance)
        {
            minDistance = d;
            tilePos = cPos;
        }
    }
    return vec3(tilePos, minDistance);
}

// @param scale Number of tiles, must be  integer for tileable results, range: [2, inf]
// @param jitter Jitter factor for the voronoi cells, if zero then it will result in a square grid, range: [0, 1], default: 1.0
// @param variance The color variance, if zero then it will result in grayscale pattern, range: [0, 1], default: 1.0
// @param seed Random seed for the color pattern, range: [0, inf], default: 0.0
// @return Returns the color of the pattern cells., range: [0, 1]
vec3 voronoiPattern(vec2 pos, vec2 scale, float jitter, float variance, float seed)
{
    vec2 tilePos = voronoiPosition(pos, scale, jitter, seed, seed).xy;
    float rand = abs(hash1D(tilePos + seed));
    return (rand &lt; variance ? hash3D(tilePos + seed) : vec3(rand));
}

void main()
{
	vec2 Scale = vec2(10.0);
	float Jitter = 1.0;
	float Phase = iTime;
	float Seed = iTime;
	
	vec3 Voronoi = voronoi(f_UV, Scale, Jitter, Phase, Seed);
	vec3 VoronoiPosition = voronoiPosition(f_UV, Scale, Jitter, Phase, Seed);
	vec3 VoronoiPattern = voronoiPattern(f_UV, Scale, Jitter, 1.0, Seed);
	
	// Voronoi Noise
	FragColor = vec4(Voronoi.xxx, 1.0);
	
	// Tile Position [0..1]
	// FragColor = vec4(Voronoi.yz, 0, 1.0);
	
	// Distance from the cell edges
	// FragColor = vec4(VoronoiPosition.zzz, 1.0);
	
	// Voronoi Pattern
	// FragColor = vec4(VoronoiPattern, 1.0);
}</Shader_FS>
		<TextureFilter>
			<TextureMagFilter>Linear</TextureMagFilter>
			<TextureMinFilter>Linear</TextureMinFilter>
			<TextureWrapModeS>Repeat</TextureWrapModeS>
			<TextureWrapModeT>Repeat</TextureWrapModeT>
		</TextureFilter>
		<TextureFilterBufferA>
			<TextureMagFilter>Linear</TextureMagFilter>
			<TextureMinFilter>Linear</TextureMinFilter>
			<TextureWrapModeS>Repeat</TextureWrapModeS>
			<TextureWrapModeT>Repeat</TextureWrapModeT>
		</TextureFilterBufferA>
		<TextureFilterBufferB>
			<TextureMagFilter>Linear</TextureMagFilter>
			<TextureMinFilter>Linear</TextureMinFilter>
			<TextureWrapModeS>Repeat</TextureWrapModeS>
			<TextureWrapModeT>Repeat</TextureWrapModeT>
		</TextureFilterBufferB>
		<TextureFilterBufferC>
			<TextureMagFilter>Linear</TextureMagFilter>
			<TextureMinFilter>Linear</TextureMinFilter>
			<TextureWrapModeS>Repeat</TextureWrapModeS>
			<TextureWrapModeT>Repeat</TextureWrapModeT>
		</TextureFilterBufferC>
		<TextureFilterBufferD>
			<TextureMagFilter>Linear</TextureMagFilter>
			<TextureMinFilter>Linear</TextureMinFilter>
			<TextureWrapModeS>Repeat</TextureWrapModeS>
			<TextureWrapModeT>Repeat</TextureWrapModeT>
		</TextureFilterBufferD>
	</Image>
</Shader>