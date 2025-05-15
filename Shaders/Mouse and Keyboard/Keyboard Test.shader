<Shader>
	<AutoCompileAfterLoading>true</AutoCompileAfterLoading>
	<AutoPlayAfterLoading>true</AutoPlayAfterLoading>
	<UseMipMaps>false</UseMipMaps>
	<MultiPassBuffers>NoBuffers</MultiPassBuffers>
	<CommonCode>// Region Uniforms
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

// Region Keys events
const int KEY_EVENT_DOWN	= 0; // return true until Release
const int KEY_EVENT_PRESS	= 1; // return true until Release or Next frame
const int KEY_EVENT_TOGGLE	= 2; // toggle Key state
// EndRegion Keys events

// Region KeyCodes (not all are available due Hotkeys)
const int KEY_BACKSPACE		= 8;
const int KEY_TAB			= 9;
const int KEY_ENTER			= 13;
const int KEY_SHIFT			= 16;
const int KEY_CTRL			= 17;
const int KEY_ALT			= 18;
const int KEY_PAUSE			= 19;
const int KEY_CAPSLOCK		= 20;
const int KEY_ESCAPE		= 27;
const int KEY_SPACE			= 32;
const int KEY_PAGEUP		= 33;
const int KEY_PAGEDOWN		= 34;
const int KEY_END			= 35;
const int KEY_HOME			= 36;
const int KEY_LEFT			= 37;
const int KEY_UP			= 38;
const int KEY_RIGHT			= 39;
const int KEY_DOWN			= 40;
const int KEY_INSERT		= 45;
const int KEY_DELETE		= 46;
const int KEY_0				= 48;
const int KEY_1				= 49;
const int KEY_2				= 50;
const int KEY_3				= 51;
const int KEY_4				= 52;
const int KEY_5				= 53;
const int KEY_6				= 54;
const int KEY_7				= 55;
const int KEY_8				= 56;
const int KEY_9				= 57;
const int KEY_A				= 65;
const int KEY_B				= 66;
const int KEY_C				= 67;
const int KEY_D				= 68;
const int KEY_E				= 69;
const int KEY_F				= 70;
const int KEY_G				= 71;
const int KEY_H				= 72;
const int KEY_I				= 73;
const int KEY_J				= 74;
const int KEY_K				= 75;
const int KEY_L				= 76;
const int KEY_M				= 77;
const int KEY_N				= 78;
const int KEY_O				= 79;
const int KEY_P				= 80;
const int KEY_Q				= 81;
const int KEY_R				= 82;
const int KEY_S				= 83;
const int KEY_T				= 84;
const int KEY_U				= 85;
const int KEY_V				= 86;
const int KEY_W				= 87;
const int KEY_X				= 88;
const int KEY_Y				= 89;
const int KEY_Z				= 90;
const int KEY_NUMPAD0		= 96;
const int KEY_NUMPAD1		= 97;
const int KEY_NUMPAD2		= 98;
const int KEY_NUMPAD3		= 99;
const int KEY_NUMPAD4		= 100;
const int KEY_NUMPAD5		= 101;
const int KEY_NUMPAD6		= 102;
const int KEY_NUMPAD7		= 103;
const int KEY_NUMPAD8		= 104;
const int KEY_NUMPAD9		= 105;
const int KEY_MULTIPLY		= 106;
const int KEY_ADD			= 107;
const int KEY_SUBTRACT		= 109;
const int KEY_DECIMAL		= 110;
const int KEY_DIVIDE		= 111;
const int KEY_F1			= 112;
const int KEY_F2			= 113;
const int KEY_F3			= 114;
const int KEY_F4			= 115;
const int KEY_F5			= 116;
const int KEY_F6			= 117;
const int KEY_F7			= 118;
const int KEY_F8			= 119;
const int KEY_F9			= 120;
const int KEY_F10			= 121;
const int KEY_F11			= 122;
const int KEY_F12			= 123;
const int KEY_NUMLOCK		= 144;
const int KEY_SCROLLLOCK	= 145;
const int KEY_SEMICOLON		= 186;
const int KEY_EQUALS		= 187;
const int KEY_COMMA			= 188;
const int KEY_MINUS			= 189;
const int KEY_PERIOD		= 190;
const int KEY_SLASH			= 191;
const int KEY_BACKQUOTE		= 192;
const int KEY_LEFTBRACKET	= 219;
const int KEY_BACKSLASH		= 220;
const int KEY_RIGHTBRACKET	= 221;
const int KEY_QUOTE			= 222;
// EndRegion KeyCodes

bool GetKeyState(int KeyCode, int KeyEventType)
{
	int Index = KeyCode / 32 + KeyEventType * 8;
	uint Mask = 1u &lt;&lt; (KeyCode % 32);
	return (iKeyStates[Index] &amp; Mask) != 0u;
}

bool IsKeyDown(int KeyCode)
{
	return GetKeyState(KeyCode, KEY_EVENT_DOWN);
}

bool IsKeyPressed(int KeyCode)
{
	return GetKeyState(KeyCode, KEY_EVENT_PRESS);
}

bool IsKeyToggled(int KeyCode)
{
	return GetKeyState(KeyCode, KEY_EVENT_TOGGLE);
}</CommonCode>
	<Image>
		<Shader_VS>#version 330

// Region Common code include
#extension GL_ARB_shading_language_include : enable

#ifdef GL_ARB_shading_language_include
#include &lt;/common.glsl&gt;
#endif
// EndRegion Common code include

// Uniforms moved to Common code

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

// Uniforms moved to Common code

in vec2 f_UV;

layout(location = 0) out vec4 FragColor;

float SDF(vec2 UV, float Radius, float OutlineRadius)
{
	float CircleSDF = length(UV) - Radius;
	
	float WithOutlineSDF = OutlineRadius &gt; 0 ? abs(CircleSDF) - OutlineRadius : CircleSDF;
	
	const float Smoothness = 0.002;
    return smoothstep(Smoothness, -Smoothness, WithOutlineSDF);
}

void main()
{
	float Aspect = iImageSize.x / iImageSize.y;
	vec2 UV = (f_UV - vec2(0.5)) * vec2(Aspect, 1.0) + vec2(0.5);
	
	FragColor.rgb = vec3(0.0);
	
	vec2 UV_W = UV - vec2(0.5, 0.7);
	vec2 UV_A = UV - vec2(0.2, 0.4);
	vec2 UV_S = UV - vec2(0.5, 0.4);
	vec2 UV_D = UV - vec2(0.8, 0.4);
	
	vec3 ColorW = vec3(1.0, 0.0, 0.0);
	vec3 ColorA = vec3(0.0, 1.0, 0.0);
	vec3 ColorS = vec3(0.0, 0.0, 1.0);
	vec3 ColorD = vec3(1.0, 1.0, 0.0);
	
	FragColor.rgb += ColorW * SDF(UV_W, 0.08, 0.003) * (IsKeyDown(   KEY_W) ? 1.0  : 0.0);
	FragColor.rgb += ColorW * SDF(UV_W, 0.10, 0.005) * (IsKeyPressed(KEY_W) ? 1.0  : 0.0);
	FragColor.rgb += ColorW * SDF(UV_W, 0.08, 0.0)   * (IsKeyToggled(KEY_W) ? 0.75 : 0.25);
	
	FragColor.rgb += ColorA * SDF(UV_A, 0.08, 0.003) * (IsKeyDown(   KEY_A) ? 1.0  : 0.0);
	FragColor.rgb += ColorA * SDF(UV_A, 0.10, 0.005) * (IsKeyPressed(KEY_A) ? 1.0  : 0.0);
	FragColor.rgb += ColorA * SDF(UV_A, 0.08, 0.0)   * (IsKeyToggled(KEY_A) ? 0.75 : 0.25);
	
	FragColor.rgb += ColorS * SDF(UV_S, 0.08, 0.003) * (IsKeyDown(   KEY_S) ? 1.0  : 0.0);
	FragColor.rgb += ColorS * SDF(UV_S, 0.10, 0.005) * (IsKeyPressed(KEY_S) ? 1.0  : 0.0);
	FragColor.rgb += ColorS * SDF(UV_S, 0.08, 0.0)   * (IsKeyToggled(KEY_S) ? 0.75 : 0.25);
	
	FragColor.rgb += ColorD * SDF(UV_D, 0.08, 0.003) * (IsKeyDown(   KEY_D) ? 1.0  : 0.0);
	FragColor.rgb += ColorD * SDF(UV_D, 0.10, 0.005) * (IsKeyPressed(KEY_D) ? 1.0  : 0.0);
	FragColor.rgb += ColorD * SDF(UV_D, 0.08, 0.0)   * (IsKeyToggled(KEY_D) ? 0.75 : 0.25);
	
	FragColor.a = texture(TextureUnit0, f_UV).a;
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