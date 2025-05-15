#define PluginName "Shader Plugin (for Photoshop) x64"
#define PluginFolder "ShaderPlugin"
#define PluginVersion "2.0"
#define PluginPublisher "ShaderPlugin"
#define PluginURL "https://hellmapper.itch.io/shader-plugin-for-photoshop"
#define PhotoshopRegPath "'SOFTWARE\Adobe\Photoshop'"
#define PhotoshopRegKey "'PluginPath'"

[Setup]
AppId={{E136B53B-CE00-42CF-BCE8-049C79D1DAF3}
AppName={#PluginName}
AppVersion={#PluginVersion}
VersionInfoVersion={#PluginVersion}
VersionInfoProductTextVersion="{#PluginVersion}"
AppPublisher={#PluginPublisher}
AppPublisherURL={#PluginURL}
AppSupportURL={#PluginURL}
AppUpdatesURL={#PluginURL}
DefaultDirName={code:GetPhotoshopPath}\{#PluginFolder}
UsePreviousAppDir=yes
DefaultGroupName={#PluginName}
AllowNoIcons=yes
SetupIconFile="icon.ico"
LicenseFile=License.txt
OutputBaseFilename="ShaderPluginInstaller_v{#PluginVersion}_x64"
UninstallDisplayIcon={uninstallexe}
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\ShaderPlugin\bin\x64\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\Shaders\*"; DestDir: "{userdocs}\ShaderPlugin for Photoshop\Shaders"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "License.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{cm:ProgramOnTheWeb,{#PluginName}}"; Filename: "{#PluginURL}"
Name: "{group}\{cm:UninstallProgram,{#PluginName}}"; Filename: "{uninstallexe}"

[Messages]
SelectDirLabel3=Select Photoshop Plugins folder like:%n"C:\Program Files\Adobe\Adobe Photoshop CC 2018\Plug-ins"

[Code]
var I: Integer;
    PhotoshopVersions: TArrayOfString;
    PhotoshopVersionKey: String;

function GetPhotoshopPath(Param: String): String;
begin
  Result := ExpandConstant('{autopf}');
  if RegGetSubkeyNames(HKEY_LOCAL_MACHINE_64, {#PhotoshopRegPath}, PhotoshopVersions) then
  begin
    for I := 0 to GetArrayLength(PhotoshopVersions) - 1 do
    begin
      PhotoshopVersionKey := {#PhotoshopRegPath} + '\' + PhotoshopVersions[I];
      if RegValueExists(HKEY_LOCAL_MACHINE_64, PhotoshopVersionKey, {#PhotoshopRegKey}) then
        RegQueryStringValue(HKEY_LOCAL_MACHINE_64, PhotoshopVersionKey, {#PhotoshopRegKey}, Result);
    end;
  end;
end;

function InitializeSetup: Boolean;
begin
  Result := IsDotNetInstalled(net4Client, 0) or IsDotNetInstalled(net462, 0);
  if not Result then
    SuppressibleMsgBox(FmtMessage(SetupMessage(msgWinVersionTooLowError), ['.NET Framework', '4.6.2']), mbCriticalError, MB_OK, IDOK);
end;