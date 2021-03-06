/*
 * LZO DLL Wrapper
 *
 * Copyright (C) 2014-2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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

#define _WIN32_WINNT 0x0501
#include <windows.h>
#include "lzo1x.h"

BOOL WINAPI DllMain(HINSTANCE hin, DWORD reason, LPVOID lpvReserved) { return TRUE; }

#define LZO_EXPORT __declspec(dllexport)

LZO_EXPORT int LZODecompress(unsigned char *src, unsigned int src_len, unsigned char *dst, unsigned int *dst_len)
{
	lzo_uint len = *dst_len;

	int status = lzo_init();
	if (status != LZO_E_OK)
		return status;

	status = lzo1x_decompress_safe(src, src_len, dst, &len, NULL);
	*dst_len = (unsigned int)len;

	return status;
}

LZO_EXPORT int LZOCompress(unsigned char *src, unsigned int src_len, unsigned char *dst, unsigned int *dst_len)
{
	lzo_uint len = 0;

	int status = lzo_init();
	if (status != LZO_E_OK)
		return status;

	unsigned char *wrkmem = malloc(LZO1X_1_15_MEM_COMPRESS);
	if (wrkmem == NULL)
		return LZO_E_OUT_OF_MEMORY;
	memset(wrkmem, 0, LZO1X_1_15_MEM_COMPRESS);

	unsigned char *tmpBuffer = malloc(src_len + LZO1X_1_15_MEM_COMPRESS);
	if (tmpBuffer == NULL)
	{
		free(wrkmem);
		return LZO_E_OUT_OF_MEMORY;
	}
	memset(tmpBuffer, 0, src_len + LZO1X_1_15_MEM_COMPRESS);

	status = lzo1x_1_15_compress(src, src_len, tmpBuffer, &len, wrkmem);
	if (status == LZO_E_OK) {
		*dst_len = (unsigned int)len;
		memcpy(dst, tmpBuffer, len);
	}

	free(tmpBuffer);
	free(wrkmem);

	return status;
}
