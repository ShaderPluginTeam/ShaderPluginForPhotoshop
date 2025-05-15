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

uvec4 ihash1D(uvec4 q)
{
    // hash by Hugo Elias, Integer Hash - I, 2017
    q = q * 747796405u + 2891336453u;
    q = (q &lt;&lt; 13u) ^ q;
    return q * (q * q * 15731u + 789221u) + 1376312589u;
}

// generates 2 random numbers for the coordinate
vec2 Hash2D(vec2 x)
{
    uvec2 q = uvec2(x);
    uint h0 = ihash1D(ihash1D(q.x) + q.y);
    uint h1 = h0 * 1933247u + ~h0 ^ 230123u;
    return vec2(h0, h1)  * (1.0 / float(0xffffffffu));
}

// generates 2 random numbers for each of the four 2D coordinates
void Hash2D(vec4 coords0, vec4 coords1, out vec4 hashX, out vec4 hashY)
{
    uvec4 hash0 = ihash1D(ihash1D(uvec4(coords0.xz, coords1.xz)) + uvec4(coords0.yw, coords1.yw));
    uvec4 hash1 = hash0 * 1933247u + ~hash0 ^ 230123u;
    hashX = vec4(hash0) * (1.0 / float(0xffffffffu));
    hashY = vec4(hash1) * (1.0 / float(0xffffffffu));
}

float distanceMetric(vec2 pos, uint metric)
{
    switch (metric)
    {
        case 0u:
            // squared euclidean
            return dot(pos, pos);
        case 1u:
            // manhattam   
            return dot(abs(pos), vec2(1.0));
        case 2u:
            // chebyshev
            return max(abs(pos.x), abs(pos.y));
        default:
            // triangular
            return  max(abs(pos.x) * 0.866025 + pos.y * 0.5, -pos.y);
    }
}

vec4 distanceMetric(vec4 px, vec4 py, uint metric)
{
    switch (metric)
    {
        case 0u:
            // squared euclidean
            return px * px + py * py;
        case 1u:
            // manhattam   
            return abs(px) + abs(py);
        case 2u:
            // chebyshev
            return max(abs(px), abs(py));
        default:
            // triangular
            return max(abs(px) * 0.866025 + py * 0.5, -py);
    }
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

// A variation of 3x3 Cellular noise that multiplies the minimum distance between the cells.
// @param scale Number of tiles, must be  integer for tileable results, range: [2, inf]
// @param jitter Jitter factor for the cells, if zero then it will result in a square grid, range: [0, 1], default: 1.0
// @param phase The phase for rotating the cells, range: [0, inf], default: 0.0
// @param metric The distance metric used, can be euclidean, manhattan, chebyshev or triangular, range: [0, 3], default: 0
// @param seed Seed to randomize result, range: [0, inf], default: 0.0
// @return Returns the metaballs distance from the cell edges, range: [0, 1]
float metaballs(vec2 pos, vec2 scale, float jitter, float phase, uint metric, float seed) 
{       
    const float kPI2 = 6.2831853071;
    pos *= scale;
    vec2 i = floor(pos);
    vec2 f = pos - i;
    
    const vec3 offset = vec3(-1.0, 0.0, 1.0);
    vec4 cells = mod(i.xyxy + offset.xxzz, scale.xyxy) + seed;
    i = mod(i, scale) + seed;
    vec4 dx0, dy0, dx1, dy1;
    Hash2D(vec4(cells.xy, vec2(i.x, cells.y)), vec4(cells.zyx, i.y), dx0, dy0);
    Hash2D(vec4(cells.zwz, i.y), vec4(cells.xw, vec2(i.x, cells.w)), dx1, dy1);
    dx0 = 0.5 * sin(phase + kPI2 * dx0) + 0.5;
    dy0 = 0.5 * sin(phase + kPI2 * dy0) + 0.5;
    dx1 = 0.5 * sin(phase + kPI2 * dx1) + 0.5;
    dy1 = 0.5 * sin(phase + kPI2 * dy1) + 0.5;
    
    dx0 = offset.xyzx + dx0 * jitter - f.xxxx; // -1 0 1 -1
    dy0 = offset.xxxy + dy0 * jitter - f.yyyy; // -1 -1 -1 0
    dx1 = offset.zzxy + dx1 * jitter - f.xxxx; // 1 1 -1 0
    dy1 = offset.zyzz + dy1 * jitter - f.yyyy; // 1 0 1 1
    vec4 d0 = distanceMetric(dx0, dy0, metric);
    vec4 d1 = distanceMetric(dx1, dy1, metric);
    
    vec2 centerPos = (0.5 * sin(phase + kPI2 * Hash2D(i)) + 0.5) * jitter - f; // 0 0
    
    float d = min(1.0, distanceMetric(centerPos, metric));
    d = min(d, d * d0.x);
    d = min(d, d * d0.y);
    d = min(d, d * d0.z);
    d = min(d, d * d0.w);
    d = min(d, d * d1.x);
    d = min(d, d * d1.y);
    d = min(d, d * d1.z);
    d = min(d, d * d1.w);
    
    return metric == 0u ? sqrt(d) : d;
}

float metaballs(vec2 pos, vec2 scale, float jitter, float phase, float width, float smoothness, uint metric, float seed) 
{       
    float d = metaballs(pos, scale, jitter, phase, metric, seed);
    return smoothstep(width, width + smoothness, d);
}

void main()
{
	vec2 Scale = vec2(10.0);
	float Jitter = 1.0;
	float Phase = iTime;
	float Width = 0.1;
	float Smoothness = 0.01;
	uint Metric = 0u; // [0..3] Euclidean, Manhattan, Chebyshev, Triangular
	float Seed = iTime;
	
	// Metaballs
	float Metaballs = metaballs(f_UV, Scale, Jitter, Phase, Metric, Seed);
	
	// Metaballs with Smoothness
	// float Metaballs = metaballs(f_UV, Scale, Jitter, Phase, Width, Smoothness, Metric, Seed);
	
	FragColor = vec4(Metaballs, Metaballs, Metaballs, 1.0);
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