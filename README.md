# Shader Plugin for Photoshop
Shader Plugin allows to process images in Photoshop with GLSL shaders. It's will be usefull for technical artists for creation shader effects, masks, noise textures in Photoshop. Also you can process images with multipass filters like FXAA (by NVIDIA) inside Photoshop!!!

<p align="center">
<img src="https://github.com/user-attachments/assets/5aae2abe-fc59-49a0-8b9c-c894928396ab" alt="Screenshot" width="60%">
</p>

**☕Support us on:** https://hellmapper.itch.io/shader-plugin-for-photoshop

## Features:
* Realtime rendering
* Support 8/16/32 bit images (color and grayscale).
* Uniforms with usefull image information from Photoshop.
* 4 additional buffers for making complex effects.
* Syntax Highlighting
* 8K Image Support
* Errors highlighting. Full errors list can be shown in separent window (just click on statusbar).

## System Requiremets:
Windows 7+ (x64 only)\
Photoshop CS5+ (tested on CS6 .. 2022)\
.Net Framework 4.6.2+

## License
MIT

ResourcesPatchTool also destributed under MIT license except DiffMatchPatch.cs\
Google [Diff Match and Patch](https://github.com/google/diff-match-patch) licensed under the Apache License, Version 2.0.

Photoshop Plug-In and Connection SDK - ADOBE® PHOTOSHOP® SDK License (not included, see `pluginsdk\About_SDK.txt`)

## Authors:
Alex Zelensky\
Vladimir Rymkevich

## How to Compile
ADOBE® PHOTOSHOP® SDK License Agreement restrict to distribute SDK parts (include modified) as code.\
This makes unavailable to share c++ Resources. They use custom build tool and process for generate them.\
Any way we can share patch with ResourcesPatchTool to solve this problem:

1. Download "Photoshop Plug-In and Connection SDK", see `pluginsdk\About_SDK.txt`
2. Open solution `ResourcesPatchTool\ResourcesPatchTool.sln` and Build it.
3. Run `ResourcesPatchTool` and select item: "1 - Apply patch and generate Resource file."
4. Now you have created `ShaderPlugin\Common\ShaderPlugin.r` from SDK and `ShaderPlugin.r_patch`.
5. Open solution `ShaderPlugin\ShaderPlugin.sln` and Build it.
6. Copy GLSL shaders from `Shaders` folder to User Documents `Documents\ShaderPlugin for Photoshop\Shaders`.
