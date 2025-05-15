using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using System.Text.RegularExpressions;

namespace ShaderPluginGUI
{
    public delegate void ShaderCompileErrorHandler(ShaderError[] Errors);

    public class ShaderProgram
    {
        public const string CommonIncludeName = "/common.glsl"; // Virtual filename for Common include code string

        public string ShaderName { get; private set; }
        public string CommonCode { get; private set; }
        public string CodeVS { get; private set; }
        public string CodeFS { get; private set; }

        public int ProgramID { get; private set; } // Shader program ID
        public int VS_ID { get; private set; } // Vertex Shader ID
        public int FS_ID { get; private set; } // Fragment Shader ID
        
        int LinkStatus = 0; // Last Link status

        public bool WasSuccessfullyCompiled => LinkStatus == 1;

        public event ShaderCompileErrorHandler CompileError;
        List<ShaderError> CompileErrors = new List<ShaderError>();

        public Dictionary<string, AttributeInfo> Attributes = new Dictionary<string, AttributeInfo>();
        public Dictionary<string, UniformInfo> Uniforms = new Dictionary<string, UniformInfo>();

        public ShaderProgram(string ShaderName, string CodeVS, string CodeFS, string CommonCode = "")
        {
            this.ShaderName = ShaderName;
            this.CodeVS = CodeVS;
            this.CodeFS = CodeFS;
            this.CommonCode = CommonCode;
        }

        public bool CompileShader()
        {
            if (GL.IsProgram(ProgramID))
            {
                return true;
            }

            ProgramID = GL.CreateProgram();

            string CommonCodeStr = CommonCode != Properties.Resources.Shader_CommonCode ? CommonCode : String.Empty;
            if (GLExtensions.IsSupported("GL_ARB_shading_language_include"))
            {
                if (GL.Arb.IsNamedString(CommonIncludeName.Length, CommonIncludeName))
                {
                    GL.Arb.DeleteNamedString(CommonIncludeName.Length, CommonIncludeName);
                }

                string NamedStr = String.IsNullOrEmpty(CommonCodeStr) ? " " : CommonCodeStr; // Send at least one space to shader to prevent missing include.
                GL.Arb.NamedString(ArbShadingLanguageInclude.ShaderIncludeArb, CommonIncludeName.Length, CommonIncludeName, NamedStr.Length, NamedStr);
            }
            else if (!String.IsNullOrEmpty(CommonCodeStr)) // Not supported
            {
                CompileErrors.Add(new ShaderError(ShaderName, ShaderErrorType.CommonInclude, "Extension \"GL_ARB_shading_language_include\" not supported!"));
                CommonCodeStr = String.Empty;
            }

            Compile(ShaderType.VertexShader);
            Compile(ShaderType.FragmentShader);

            Link();
            TextureUnits();

            if (GLExtensions.IsSupported("GL_ARB_shading_language_include") && GL.Arb.IsNamedString(CommonIncludeName.Length, CommonIncludeName))
            {
                GL.Arb.DeleteNamedString(CommonIncludeName.Length, CommonIncludeName);
            }

            // Check for Errors
            GL.GetProgram(ProgramID, GetProgramParameterName.LinkStatus, out LinkStatus);
            if (LinkStatus == 1)
            {
                return true;
            }

            string InfoLog = GL.GetShaderInfoLog(VS_ID);
            if (InfoLog != String.Empty)
            {
                CompileErrors.Add(new ShaderError(ShaderName, ShaderErrorType.Vertex, InfoLog));
            }

            InfoLog = GL.GetShaderInfoLog(FS_ID);
            if (InfoLog != String.Empty)
            {
                CompileErrors.Add(new ShaderError(ShaderName, ShaderErrorType.Fragment, InfoLog));
            }

            // Check shader for errors
            InfoLog = GL.GetProgramInfoLog(ProgramID);
            if (InfoLog.Trim() != String.Empty)
            {
                CompileErrors.Add(new ShaderError(ShaderName, ShaderErrorType.ShaderProgram, InfoLog));
            }

            if (!String.IsNullOrEmpty(CommonCodeStr)) // Need to duplicate Common code include errors
            {
                List<ShaderError> CommonCodeCompileErrors = new List<ShaderError>();
                foreach (ShaderError Error in CompileErrors)
                {
                    if (Error.InfoLog.Contains(CommonIncludeName))
                    {
                        ShaderError CommonCodeError = Error;
                        CommonCodeError.ShaderName = "CommonCode";
                        CommonCodeError.ErrorType = ShaderErrorType.CommonInclude;
                        CommonCodeCompileErrors.Add(CommonCodeError);
                    }
                }
                CompileErrors.AddRange(CommonCodeCompileErrors);
            }

            if (CompileErrors.Count > 0)
            {
                OnShaderCompileError(CompileErrors.ToArray());
            }

            return CompileErrors.Count == 0;
        }

        void Compile(ShaderType Type)
        {
            int CompileShader(String Code)
            {
                int ShaderID = GL.CreateShader(Type);
                GL.ShaderSource(ShaderID, Code);

                if (GLExtensions.IsSupported("GL_ARB_shading_language_include"))
                {
                    string[] ShaderIncludes = new string[] { CommonIncludeName };
                    int ShaderIncludeLength = 0; // must be null for null-terminated strings, see https://registry.khronos.org/OpenGL/extensions/ARB/ARB_shading_language_include.txt
                    GL.Arb.CompileShaderInclude(ShaderID, ShaderIncludes.Length, ShaderIncludes, ref ShaderIncludeLength);
                }
                else
                {
                    GL.CompileShader(ShaderID);
                }

                // Set labels for debug (OpenGL 4.3+)
                // string ShaderLabel = ShaderName + "_" + Type.ToString();
                // GL.ObjectLabel(ObjectLabelIdentifier.Shader, ShaderID, ShaderLabel.Length, ShaderLabel);

                GL.AttachShader(ProgramID, ShaderID);
                return ShaderID;
            }

            switch (Type)
            {
                case ShaderType.VertexShader:
                    VS_ID = CompileShader(CodeVS);
                    break;

                case ShaderType.FragmentShader:
                    FS_ID = CompileShader(CodeFS);
                    break;
            }
        }

        void Link()
        {
            GL.LinkProgram(ProgramID);

            GL.GetProgram(ProgramID, GetProgramParameterName.ActiveAttributes, out int AttributeCount);
            GL.GetProgram(ProgramID, GetProgramParameterName.ActiveUniforms, out int UniformCount);

            for (int i = 0; i < AttributeCount; i++)
            {
                AttributeInfo Attribute = new AttributeInfo();
                Attribute.Name = GL.GetActiveAttrib(ProgramID, i, out Attribute.Size, out Attribute.Type);
                Attribute.Address = GL.GetAttribLocation(ProgramID, Attribute.Name);
                Attributes.Add(Attribute.Name, Attribute);
            }

            for (int i = 0; i < UniformCount; i++)
            {
                UniformInfo Uniform = new UniformInfo();
                Uniform.Name = GL.GetActiveUniform(ProgramID, i, out Uniform.Size, out Uniform.Type);
                Uniform.Address = GL.GetUniformLocation(ProgramID, Uniform.Name);
                Uniforms.Add(Uniform.Name, Uniform);
            }
        }

        void TextureUnits()
        {
            GL.UseProgram(ProgramID);

            // Set default Texture uniforms
            for (int TextureUnit = TextureIDs.OriginalImage; TextureUnit < TextureIDs.MAX; TextureUnit++)
            {
                int TextureUnitLocation = GetUniform("TextureUnit" + TextureUnit.ToString());
                if (TextureUnitLocation != -1)
                {
                    GL.Uniform1(TextureUnitLocation, TextureUnit);
                }
            }

            // ShaderToy compability
            for (int iChannel = 0; iChannel <= TextureIDs.BufferD - TextureIDs.BufferA; iChannel++)
            {
                int TextureUnitLocation = GetUniform("iChannel" + iChannel.ToString());
                if (TextureUnitLocation != -1)
                {
                    GL.Uniform1(TextureUnitLocation, iChannel);
                }
            }

            GL.UseProgram(0);
        }

        public void EnableVertexAttribArrays()
        {
            foreach (AttributeInfo Attribute in Attributes.Values)
            {
                GL.EnableVertexAttribArray(Attribute.Address);
            }
        }

        public void DisableVertexAttribArrays()
        {
            foreach (AttributeInfo Attribute in Attributes.Values)
            {
                GL.DisableVertexAttribArray(Attribute.Address);
            }
        }

        public int GetAttribute(string Name)
        {
            if (Attributes.ContainsKey(Name))
            {
                return Attributes[Name].Address;
            }

            return -1;
        }

        public int GetUniform(string Name)
        {
            if (Uniforms.ContainsKey(Name))
            {
                return Uniforms[Name].Address;
            }

            return -1;
        }

        public void Free()
        {
            GL.UseProgram(0);

            if (GL.IsShader(VS_ID))
            {
                GL.DetachShader(ProgramID, VS_ID);
                GL.DeleteShader(VS_ID);
                VS_ID = 0;
            }

            if (GL.IsShader(FS_ID))
            {
                GL.DetachShader(ProgramID, FS_ID);
                GL.DeleteShader(FS_ID);
                FS_ID = 0;
            }

            if (GL.IsProgram(ProgramID))
            {
                GL.DeleteProgram(ProgramID);
                ProgramID = 0;
            }

            CodeVS = CodeFS = CommonCode = String.Empty;

            Attributes = null;
            Uniforms = null;
        }

        private void OnShaderCompileError(ShaderError[] ShaderErrors)
        {
            CompileError?.Invoke(ShaderErrors);
        }

        public override string ToString()
        {
            return ShaderName.ToString();
        }
    }

    public class UniformInfo
    {
        public String Name = String.Empty;
        public int Address = -1;
        public int Size = 0;
        public ActiveUniformType Type;

        public override string ToString()
        {
            return Name.ToString();
        }
    }

    public class AttributeInfo
    {
        public String Name = String.Empty;
        public int Address = -1;
        public int Size = 0;
        public ActiveAttribType Type;

        public override string ToString()
        {
            return Name.ToString();
        }
    }

    public enum ShaderErrorType
    {
        Unknown = 0,
        LoadFromString,
        CommonInclude,
        ShaderProgram,
        Vertex,
        Fragment,
    }

    public struct ShaderError
    {
        public string ShaderName;
        public string InfoLog;
        public ShaderErrorType ErrorType;
        public Dictionary<int, string> ErrorLines;
        public Dictionary<int, string> ErrorLinesCommonInclude;

        public ShaderError(string ShaderName, ShaderErrorType ErrorType, string InfoLog)
        {
            this.ShaderName = ShaderName;
            this.ErrorType = ErrorType;
            this.InfoLog = InfoLog;

            ErrorLines = ErrorLinesCommonInclude = null;
            ErrorLines = GetErrorsFromLog(false);
            ErrorLinesCommonInclude = GetErrorsFromLog(true);
        }

        private Dictionary<int, string> GetErrorsFromLog(bool CommonIncludeOnly)
        {
            Dictionary<int, string> ErrorLines = new Dictionary<int, string>();

            if (String.IsNullOrEmpty(InfoLog))
            {
                return ErrorLines;
            }

            #region Nvidia
            /* Nvidia shader errors examples:
             * 0(39) : error C1503: undefined variable "gl_dPosition"
             * 0:78(17): error: sampler arrays indexed with non-constant expressions are forbidden in GLSL 1.30 and later
             * 0:86(49): warning: `tex_start' used uninitialized
             * 0:86(20): error: sampler arrays indexed with non - constant expressions are forbidden in GLSL 1.30 and later
             *
             * Group 1: Source file, by default - "0".
             * Group 2: Colunm (character position in string), usually not presented by driver now.
             * Group 3: Line Number.
             * Group 4: Error, like: "error C1115: unable to find compatible overloaded function".
             */
            MatchCollection MatchesNvidia = Regex.Matches(InfoLog, @"^(\d+|\/?common.glsl)(?::(\d*))?\((\d+)\) ?: ?((?:error|warning).*)", RegexOptions.Multiline);
            if (MatchesNvidia.Count > 0)
            {
                foreach (Match Match in MatchesNvidia)
                {
                    if (int.TryParse(Match.Groups[3].Value, out int ErrLine))
                    {
                        bool IsCommonInclude = Match.Groups[1].Value == ShaderProgram.CommonIncludeName;
                        if (CommonIncludeOnly != IsCommonInclude)
                        {
                            continue;
                        }

                        string ErrorLineStr = Match.Groups[4].Value;
                        if (ErrorLines.ContainsKey(ErrLine))
                        {
                            ErrorLines[ErrLine] += Environment.NewLine + ErrorLineStr;
                        }
                        else
                        {
                            ErrorLines.Add(ErrLine, ErrorLineStr);
                        }
                    }
                }
            }
            #endregion Nvidia

            #region AMD/Intel
            /* AMD shader errors examples:
             * ERROR: 0:5: error(#328) interface block should not apply in 'Vertex Shader in'.
             * ERROR: 0:14: error(#143) Undeclared identifier ds_VertexPosition
             * ERROR: 0:14: error(#202) No matching overloaded function found ProjectVertexPosition
             * ERROR: 0:14: error(#160) Cannot convert from 'const float' to 'Position 4-component vector of float'
             * 
             * Intel shader errors examples:
             * WARNING: 0:5: '#extension' :  'GL_ARB_shading_language_include' is not supported
             * ERROR: 0:40: '}' : syntax error syntax error
             * 
             * Group 1: Error type: "ERROR" or "WARNING".
             * Group 2: Source file, by default - "0".
             * Group 3: Line number.
             * Group 4: Error, like: "Implicit cast from vec3 to vec4".
             */
            MatchCollection MatchesOtherVendors = Regex.Matches(InfoLog, @"^(ERROR|WARNING): (\d+|\/?common.glsl):(\d+): ?(.*)", RegexOptions.Multiline);
            if (MatchesOtherVendors.Count > 0)
            {
                foreach (Match Match in MatchesOtherVendors)
                {
                    if (int.TryParse(Match.Groups[3].Value, out int ErrLine))
                    {
                        bool IsCommonInclude = Match.Groups[1].Value == ShaderProgram.CommonIncludeName;
                        if (CommonIncludeOnly != IsCommonInclude)
                        {
                            continue;
                        }

                        string ErrorLineStr = Match.Groups[4].Value;
                        if (ErrorLines.ContainsKey(ErrLine))
                        {
                            ErrorLines[ErrLine] += Environment.NewLine + ErrorLineStr;
                        }
                        else
                        {
                            ErrorLines.Add(ErrLine, ErrorLineStr);
                        }
                    }
                }
            }
            #endregion AMD/Intel

            // Remove Duplicates (per Line)
            Dictionary<int, string> FilteredErrorLines = new Dictionary<int, string>();
            foreach (var kvp in ErrorLines)
            {
                int ErrLine = kvp.Key;
                string ErrorsPerLineStr = kvp.Value;

                if (kvp.Value.Contains(Environment.NewLine))
                {
                    string[] ErrorsPerLine = ErrorsPerLineStr.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    ErrorsPerLineStr = String.Join(Environment.NewLine, ErrorsPerLine.Distinct());
                }

                FilteredErrorLines.Add(ErrLine, ErrorsPerLineStr);
            }

            return FilteredErrorLines;
        }

        public override string ToString()
        {
            return $"Name: \"{ShaderName}\", ErrorType: \"{ErrorType}\", InfoLog: \"{InfoLog}\"...";
        }
    }
}
