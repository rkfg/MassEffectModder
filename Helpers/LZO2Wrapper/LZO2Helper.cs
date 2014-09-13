/*
 * C# LZO2 Helper for wrapper
 *
 * Copyright (C) 2014 Pawel Kolodziejski <aquadran at users.sourceforge.net>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 *
 */

using System;
using System.Runtime.InteropServices;

namespace LZO2Helper
{
    public static class LZO2
    {
        [DllImport("lzo2wrapper.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 LZODecompress([In] byte[] srcBuf, uint srcLen, [Out] byte[] dstBuf, ref uint dstLen);

        [DllImport("lzo2wrapper.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 LZOCompress([In] byte[] srcBuf, uint srcLen, [Out] byte[] dstBuf, ref uint dstLen);


        public unsafe static uint Decompress(byte[] src, byte[] dst)
        {
            uint dstLen = 0;

            int status = LZODecompress(src, (uint)src.Length, dst, ref dstLen);

            return dstLen;
        }

        public unsafe static byte[] Compress(byte[] src)
        {
            uint dstLen = 0;
            byte[] tmpbuf = new byte[src.Length + (src.Length / 16) + 64 + 3];

            int status = LZOCompress(src, (uint)src.Length, tmpbuf, ref dstLen);
            if (status != 0)
                return new byte[0];

            byte[] dst = new byte[dstLen];
            Array.Copy(tmpbuf, dst, dstLen);

            return dst;
        }
    }
}
