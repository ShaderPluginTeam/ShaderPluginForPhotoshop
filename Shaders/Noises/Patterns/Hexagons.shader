<Shader>
	<AutoCompileAfterLoading>true</AutoCompileAfterLoading>
	<AutoPlayAfterLoading>true</AutoPlayAfterLoading>
	<UseMipMaps>false</UseMipMaps>
	<MultiPassBuffers>NoBuffers</MultiPassBuffers>
	<CommonCode />
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

// 2D Hexagonal grid tiling.
// @param scale Number of tiles, must be an integer for tileable results, range: [1, inf]
// @param isVertical Changes the orientation of the hexagons.
// @return xy = normalized UV, zw = center position in UV space, range: [0, 1], edgeDistance = distonce from the edge
vec4 tileHexagons(vec2 pos, vec2 scale, bool isVertical, out float edgeDistance)
{
    const float kSqrtThree = 1.73205080757;
    const float kHalfSqrtThree = 0.866025403785;
    const float kInvSqrtThree = 0.57735026919;
    vec4 hexScale = vec4(1.0, kSqrtThree, 1.0, kInvSqrtThree);
    hexScale = isVertical ? hexScale : hexScale.yxwz;
    vec4 r = vec4(1.0, kHalfSqrtThree, 0.5, kInvSqrtThree);
    r = isVertical ? r : r.yxwz;
    
    vec2 invScale = 1.0 / scale;
    vec2 p = pos * scale * hexScale.xy;
    vec4 center = floor(p.xyxy * hexScale.zwzw + vec4(0.0, 0.0, -r.zw)) + 0.5;
    vec4 uv = p.xyxy - center * hexScale.xyxy + vec4(0.0, 0.0, -0.5 * hexScale.xy);
    // dot product
    vec4 temp = uv * uv;
    temp.xy = temp.xz + temp.yw;
    vec4 uvCenter = temp.x &lt; temp.y ? vec4(uv.xy, center.xy) : vec4(uv.zw, center.zw + 0.5);
    // normalize UV range and transform center position to UV space
    uvCenter = uvCenter * vec4(r.xy, invScale) + vec4(0.5, 0.5, 0.0, 0.0);
    
    // 60 degrees, tan of 30 degrees and one over angle
    const vec3 kAngles = vec3(1.047198, 0.954929249292, 0.523599);
    
    float size = isVertical ? invScale.x : invScale.y;
    p = (pos - uvCenter.zw) * scale * hexScale.xy * size;
    // rotate 90 degrees
    p = isVertical ? p: -p.yx;
    float radius = size * 0.5;
    temp.xy = vec2(radius, atan(p.y, p.x)) * kAngles.zy + vec2(0.0, 0.5);
    float angle = kAngles.x * floor(temp.y);
    vec2 rotation = vec2(sin(angle), cos(angle));
    p = p.x * rotation.yx * vec2(1.0, -1.0) + p.y * rotation;

    vec2 offset = vec2(radius, clamp(p.y, -temp.x, temp.x));
    edgeDistance = length(p - offset) / radius;
    
    return uvCenter;
}

void main()
{
	vec2 Scale = vec2(2.5, 4.0);
	bool isVertical = false;
	float EdgeDistance;
	vec4 Hexagons = tileHexagons(f_UV, Scale, isVertical, EdgeDistance);
	
	// Hexagons UV
	FragColor.rgb = vec3(Hexagons.xy, 0.0);
	
	// Hexagons UV Centers
	// FragColor.rgb = vec3(Hexagons.zw, 0.0);
	
	// Hexagons Center Distances
	// FragColor.rgb = vec3(EdgeDistance);
	
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