using System;
using System.IO;

namespace PackPanel.Look
{
    /// <summary>Validate the PNG header before allocating a preview texture. Unity validates the encoded image itself.</summary>
    public static class PreviewPng
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        public static void Validate(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 33)
                throw new InvalidDataException("PNG header is incomplete.");
            for (int i = 0; i < Signature.Length; i++)
                if (bytes[i] != Signature[i])
                    throw new InvalidDataException("Background must be a PNG file.");
            if (Number(bytes, 8) != 13 || bytes[12] != 'I' || bytes[13] != 'H' || bytes[14] != 'D' || bytes[15] != 'R')
                throw new InvalidDataException("PNG is missing its IHDR header.");
            uint width = Number(bytes, 16), height = Number(bytes, 20);
            if (width == 0 || height == 0 || width > 8192 || height > 8192)
                throw new InvalidDataException("Background dimensions must be between 1 and 8192 pixels.");
        }

        private static uint Number(byte[] bytes, int at) =>
            ((uint)bytes[at] << 24) | ((uint)bytes[at + 1] << 16) | ((uint)bytes[at + 2] << 8) | bytes[at + 3];
    }
}
