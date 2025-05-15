<Shader>
	<AutoCompileAfterLoading>true</AutoCompileAfterLoading>
	<AutoPlayAfterLoading>true</AutoPlayAfterLoading>
	<UseMipMaps>false</UseMipMaps>
	<MultiPassBuffers>NoBuffers</MultiPassBuffers>
	<CommonCode>vec3 hue2rgb(float hue)
{
	hue=fract(hue);
	return clamp(vec3(
		abs(hue*6.-3.)-1.,
		2.-abs(hue*6.-2.),
		2.-abs(hue*6.-4.)
	), 0, 1);
}

vec3 rgb2hsl(vec3 c)
{
	float cMin=min(min(c.r,c.g),c.b),
	      cMax=max(max(c.r,c.g),c.b),
	      delta = cMax-cMin;
    
	vec3 hsl=vec3(0.,0.,(cMax+cMin)/2.);
	if(delta!=0.0) //If it has chroma and isn't gray.
    {
		if(hsl.z&lt;.5)
        {
			hsl.y=delta/(cMax+cMin); //Saturation.
		}
        else
        {
			hsl.y=delta/(2.-cMax-cMin); //Saturation.
		}
        
		float deltaR=(((cMax-c.r)/6.)+(delta/2.))/delta,
		      deltaG=(((cMax-c.g)/6.)+(delta/2.))/delta,
		      deltaB=(((cMax-c.b)/6.)+(delta/2.))/delta;
        
		//Hue.
		if(c.r==cMax)
        {
			hsl.x=deltaB-deltaG;
		}
        else if(c.g==cMax)
        {
			hsl.x=(1./3.)+deltaR-deltaB;
		}
        else
        {
			hsl.x=(2./3.)+deltaG-deltaR;
		}
		hsl.x=fract(hsl.x);
	}
	return hsl;
}

vec3 hsl2rgb(vec3 hsl)
{
	if(hsl.y==0.)
    {
		return vec3(hsl.z); //Luminance.
	}
    else
    {
		float b;
		if(hsl.z&lt;.5)
        {
			b=hsl.z*(1.+hsl.y);
		}
        else
        {
			b=hsl.z+hsl.y-hsl.y*hsl.z;
		}
		float a=2.*hsl.z-b;
		return a + hue2rgb(hsl.x) * (b-a);
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

void main()
{
	vec3 C = texture(TextureUnit0, f_UV).rgb;
	C = rgb2hsl(C);
	C.y = 0;
	FragColor.rgb = hsl2rgb(C);
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