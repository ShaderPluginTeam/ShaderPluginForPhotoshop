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

#define M_PI 3.1415926535897932384626433832795

void main()
{
	vec2 Offset = vec2(0.0, 0.0);
	float Tiling = 2;
	float Angle = 0.0; // Degrees [0 .. 360]
	
	if (iMouse.z &gt; 0) // Rotate by mouse
	{
		vec2 MouseDir = (iViewSize * 0.5 - iMouse.xy);
		Angle = atan(MouseDir.y, MouseDir.x) * 180.0 / M_PI;
	}
	
	float AngleRad = Angle / 180 * M_PI;
	vec2 UV = f_UV - 0.5;
	float UV_RX = UV.x * cos(AngleRad) - UV.y * sin(AngleRad);
	float UV_RY = UV.y * cos(AngleRad) + UV.x * sin(AngleRad);
	UV = (vec2(UV_RX, UV_RY) + 0.5) * Tiling - Offset;
	FragColor = texture(TextureUnit0, UV);
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