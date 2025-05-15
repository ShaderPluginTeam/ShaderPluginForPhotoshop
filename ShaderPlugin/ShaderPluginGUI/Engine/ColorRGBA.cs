using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Text;

using OpenTK;
using OpenTK.Graphics;

namespace ShaderPluginGUI
{
    public class ColorRGBA
    {
        public float R = 0f, G = 0f, B = 0f, A = 1f;

        public byte RByte
        {
            get => (byte)MathHelper.Clamp(R * 255f, 0, 255);
            set => R = value / 255.0f;
        }

        public byte GByte
        {
            get => (byte)MathHelper.Clamp(G * 255f, 0, 255);
            set => G = value / 255.0f;
        }

        public byte BByte
        {
            get => (byte)MathHelper.Clamp(B * 255f, 0, 255);
            set => B = value / 255.0f;
        }

        public byte AByte
        {
            get => (byte)MathHelper.Clamp(A * 255f, 0, 255);
            set => A = value / 255.0f;
        }

        public ColorRGBA WithoutAlpha => new ColorRGBA(R, G, B);


        public ColorRGBA() // Don't delete it. Needed for serialization.
        {
        }

        public ColorRGBA(byte Value = 0)
        {
            R = Value / 255f;
            G = Value / 255f;
            B = Value / 255f;
        }

        public ColorRGBA(byte R, byte G, byte B, byte A = 255)
        {
            this.R = R / 255f;
            this.G = G / 255f;
            this.B = B / 255f;
            this.A = A / 255f;
        }

        public ColorRGBA(float Value = 0f)
        {
            R = Value;
            G = Value;
            B = Value;
        }

        public ColorRGBA(float R, float G, float B, float A = 1f)
        {
            this.R = R;
            this.G = G;
            this.B = B;
            this.A = A;
        }

        public ColorRGBA(Color color)
        {
            R = color.R / 255f;
            G = color.G / 255f;
            B = color.B / 255f;
            A = color.A / 255f;
        }

        public ColorRGBA(Color4 color)
        {
            R = color.R;
            G = color.G;
            B = color.B;
            A = color.A;
        }

        public ColorRGBA(Vector4 vector)
        {
            R = vector.X;
            G = vector.Y;
            B = vector.Z;
            A = vector.W;
        }

        public ColorRGBA(Vector3 vector)
        {
            R = vector.X;
            G = vector.Y;
            B = vector.Z;
        }

        public static implicit operator Color(ColorRGBA color) => Color.FromArgb(color.AByte, color.RByte, color.GByte, color.BByte);

        public static implicit operator Color4(ColorRGBA color) => new Color4(color.R, color.G, color.B, color.A);

        public static implicit operator Vector3(ColorRGBA color) => new Vector3(color.R, color.G, color.B);

        public static implicit operator Vector4(ColorRGBA color) => new Vector4(color.R, color.G, color.B, color.A);

        public static implicit operator ColorRGBA(Color color) => new ColorRGBA(color);

        public static implicit operator ColorRGBA(Color4 color) => new ColorRGBA(color);

        public static implicit operator ColorRGBA(Vector3 vector) => new ColorRGBA(vector);

        public static implicit operator ColorRGBA(Vector4 vector) => new ColorRGBA(vector);

        public static ColorRGBA Empty = new ColorRGBA(0.0f, 0.0f, 0.0f, 0.0f);
    }
}
