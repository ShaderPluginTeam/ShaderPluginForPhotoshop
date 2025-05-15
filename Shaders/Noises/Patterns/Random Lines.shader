<Shader>
	<AutoCompileAfterLoading>true</AutoCompileAfterLoading>
	<AutoPlayAfterLoading>true</AutoPlayAfterLoading>
	<UseMipMaps>false</UseMipMaps>
	<MultiPassBuffers>NoBuffers</MultiPassBuffers>
	<CommonCode>uvec4 ihash1D(uvec4 q)
{
    // hash by Hugo Elias, Integer Hash - I, 2017
    q = q * 747796405u + 2891336453u;
    q = (q &lt;&lt; 13u) ^ q;
    return q * (q * q * 15731u + 789221u) + 1376312589u;
}

// generates a random number for each of the 4 cell corners
vec4 betterHash2D(vec4 cell)    
{
    uvec4 i = uvec4(cell);
    uvec4 hash = ihash1D(ihash1D(i.xzxz) + i.yyww);
    return vec4(hash) * (1.0 / float(0xffffffffu));
}

// the main noise interpolation function using a hermite polynomial
vec2 noiseInterpolate(const in vec2 x) 
{ 
    vec2 x2 = x * x;
    return x2 * x * (x * (x * 6.0 - 15.0) + 10.0); 
}

vec4 noiseInterpolate(const in vec4 x) 
{ 
    vec4 x2 = x * x;
    return x2 * x * (x * (x * 6.0 - 15.0) + 10.0); 
}

// 2D Value noise that returns two values.
// @param scale Number of tiles, must be an integer for tileable results, range: [2, inf]
// @param phase The phase for rotating the hash, range: [0, inf], default: 0.0
// @param seed Seed to randomize result, range: [0, inf]
// @return Value of the noise, range: [-1, 1]
vec2 multiNoise(vec4 pos, vec4 scale, float phase, vec2 seed) 
{
    const float kPI2 = 6.2831853071;
    pos *= scale;
    vec4 i = floor(pos);
    vec4 f = pos - i;
    vec4 i0 = mod(i.xyxy + vec2(0.0, 1.0).xxyy, scale.xyxy) + seed.x;
    vec4 i1 = mod(i.zwzw + vec2(0.0, 1.0).xxyy, scale.xyxy) + seed.y;

    vec4 hash0 = betterHash2D(i0);
    hash0 = 0.5 * sin(phase + kPI2 * hash0) + 0.5;
    vec4 hash1 = betterHash2D(i1);
    hash1 = 0.5 * sin(phase + kPI2 * hash1) + 0.5;
    vec2 a = vec2(hash0.x, hash1.x);
    vec2 b = vec2(hash0.y, hash1.y);
    vec2 c = vec2(hash0.z, hash1.z);
    vec2 d = vec2(hash0.w, hash1.w);

    vec4 u = noiseInterpolate(f);
    vec2 value = mix(a, b, u.xz) + (c - a) * u.yw * (1.0 - u.xz) + (d - b) * u.xz * u.yw;
    return value * 2.0 - 1.0;
}

// 2D Value noise.
// @param scale Number of tiles, must be an integer for tileable results, range: [2, inf]
// @param phase The phase for rotating the hash, range: [0, inf], default: 0.0
// @param seed Seed to randomize result, range: [0, inf]
// @return Value of the noise, range: [-1, 1]
float noise(vec2 pos, vec2 scale, float phase, float seed) 
{
    const float kPI2 = 6.2831853071;
    pos *= scale;
    vec4 i = floor(pos).xyxy + vec2(0.0, 1.0).xxyy;
    vec2 f = pos - i.xy;
    i = mod(i, scale.xyxy) + seed;

    vec4 hash = betterHash2D(i);
    hash = 0.5 * sin(phase + kPI2 * hash) + 0.5;
    float a = hash.x;
    float b = hash.y;
    float c = hash.z;
    float d = hash.w;

    vec2 u = noiseInterpolate(f);
    float value = mix(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
    return value * 2.0 - 1.0;
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

// 2D Variant of Value noise that produces lines of random color and configurable width.
// @param scale Number of tiles, must be integer for tileable results, range: [2, inf]
// @param count The density of the lines, range: [1, inf], default: 4.0
// @param jitter Jitter factor for the lines, if zero then it will result straight lines, range: [0, 1], default: 1.0
// @param smoothness The radius of the dots, range: [0, 1], default: 0.5
// @param seed Seed to randomize result, range: [0, inf]
// @return x = value of the noise, range: [0, 1], y = id of lines, range: [0, count] 
vec2 randomLines(vec2 pos, vec2 scale, float count, float width, float jitter, vec2 smoothness, float phase, float seed)
{
    float strength = jitter * 1.25;

    // compute gradient
    // TODO: compute the gradient analytically
    vec2 grad;
    vec3 offsets = vec3(1.0, 0.0, -1.0) / 1024.0;
    vec4 p = pos.xyxy + offsets.xyzy;
    vec2 nv = count * (strength * multiNoise(p, scale.xyxy, phase, vec2(seed)) + p.yw);
    grad.x = nv.x - nv.y;
    p = pos.xyxy + offsets.yxyz;
    nv = count * (strength * multiNoise(p, scale.xyxy, phase, vec2(seed)) + p.yw);
    grad.y = nv.x - nv.y;
    
    float v =  count * (strength * noise(pos, scale, phase, seed) + pos.y);
    float w = fract(v) / length(grad / (2.0 * offsets.x));
    width *= 0.1;
    smoothness *= width;
    smoothness += max(abs(grad.x), abs(grad.y)) * 0.02;
    
    float d = smoothstep(0.0, smoothness.x, w) - smoothstep(max(width - smoothness.y, 0.0), width, w);
    return vec2(d, mod(floor(v), count));
}

// 2D Variant of Value noise that produces lines of random color and configurable width.
// @param scale Number of tiles, must be integer for tileable results, range: [2, inf]
// @param count The density of the lines, range: [1, inf], default: 4.0
// @param jitter Jitter factor for the lines, if zero then it will result straight lines, range: [0, 1], default: 1.0
// @param smoothness The radius of the dots, range: [0, 1], default: 0.5
// @param colorVariation The variation for the color of the lines, range: [0, 1], default: 1.0
// @param seed Seed to randomize result, range: [0, inf]
// @return Color of the lines, black if background, range: [0, 1]
vec4 randomLines(vec2 pos, vec2 scale, float count, float width, float jitter, vec2 smoothness, float phase, float colorVariation, float seed)
{
    vec2 l = randomLines(pos, scale, count, width, jitter, smoothness, phase, seed);
    vec3 r = hash3D(l.yy + seed);
    return vec4(l.x * (r.x &lt;= colorVariation ? r : r.xxx), l.x);
}

void main()
{
	vec2 Scale = vec2(10.0);
	float Count = 10;
	float Width = 0.05;
	float Jitter = 0.1;
	vec2 Smoothness = vec2(0.35);
	float Phase = iTime;
	float ColorVariation = 1.0;
	float Seed = iTime;
	
	vec4 RandomLines = randomLines(f_UV, Scale, Count, Width, Jitter, Smoothness, Phase, ColorVariation, Seed);
	
	// Random Lines Mask
	// FragColor.rgb = RandomLines.www;
	
	// Random Lines Color
	FragColor.rgb = RandomLines.rgb;
	
	FragColor.a = 1.0;
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