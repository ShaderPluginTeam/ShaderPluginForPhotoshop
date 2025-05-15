<Shader>
	<AutoCompileAfterLoading>true</AutoCompileAfterLoading>
	<AutoPlayAfterLoading>true</AutoPlayAfterLoading>
	<UseMipMaps>false</UseMipMaps>
	<MultiPassBuffers>NoBuffers</MultiPassBuffers>
	<CommonCode>// @return Value of the noise, range: [0, 1]
vec4 hash4D(vec2 x)
{
    // based on: pcg4 by Mark Jarzynski: http://www.jcgt.org/published/0009/03/02/
    uvec4 v = uvec4(x.xyyx * 8192.0) * 1664525u + 1013904223u;

    v += v.yzxy * v.wxyz;
    v.x += v.y * v.w;
    v.y += v.z * v.x;
    v.z += v.x * v.y;
    v.w += v.y * v.z;
    
    v.x += v.y * v.w;
    v.w += v.y * v.z;
    
    v ^= v &gt;&gt; 16u;

    return vec4(v ^ (v &gt;&gt; 16u)) * (1.0 / float(0xffffffffu));
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

// 2D Variant of Value noise that produces dots with random size or luminance.
// @param scale Number of tiles, must be integer for tileable results, range: [2, inf]
// @param density The density of the dots distribution, range: [0, 1], default: 1.0
// @param size The radius of the dots, range: [0, 1], default: 0.5
// @param sizeVariation The variation for the size of the dots, range: [0, 1], default: 0.75
// @param roundness The roundness of the dots, if zero will result in square, range: [0, 1], default: 1.0
// @param seed Seed to randomize result, range: [0, inf]
// @return x = value of the noise, y = random luminance value, z = size of the dot, range: [0, 1]
vec3 dotsNoise(vec2 pos, vec2 scale, float density, float size, float sizeVariation, float roundness, float seed)
{
    pos *= scale;
    vec4 i = floor(pos).xyxy + vec2(0.0, 1.0).xxyy;
    vec2 f = pos - i.xy;
    i = mod(i, scale.xyxy);
    
    vec4 hash = hash4D(i.xy + seed);
    if (hash.w &gt; density)
        return vec3(0.0);

    float radius = clamp(size + (hash.z * 2.0 - 1.0) * sizeVariation * 0.5, 0.0, 1.0);
    float value = radius / size;  
    radius = 2.0 / radius;
    f = f * radius - (radius - 1.0);
    f += hash.xy * (radius - 2.0);
    f = pow(abs(f), vec2((mix(20.0, 1.0, sqrt(roundness)))));

    float u = 1.0 - min(dot(f, f), 1.0);
    return vec3(clamp(u * u * u * value, 0.0, 1.0), hash.w, hash.z);
}

void main()
{
	vec2 Scale = vec2(10.0);
	float Density = 0.5; // [0..1]
	float Size = 0.5; // Dots Radius
	float SizeVariation = 0.75; // [0..1]
	float Roundness = 1.0; // [0..1] Rectangles..Circles
	float Seed = floor(iTime);
	vec3 DotsNoise = dotsNoise(f_UV, Scale, Density, Size, SizeVariation, Roundness, Seed);
	
	vec3 Result = DotsNoise.xxx; // Noise Value
	
	// Random Colors
	Result *= hash4D(DotsNoise.yz).xyz;
	
	FragColor.rgb = Result;
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