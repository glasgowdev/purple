/* XMRig
 * Copyright 2010      Jeff Garzik <jgarzik@pobox.com>
 * Copyright 2012-2014 pooler      <pooler@litecoinpool.org>
 * Copyright 2014      Lucas Jones <https://github.com/lucasjones>
 * Copyright 2014-2016 Wolf9466    <https://github.com/OhGodAPet>
 * Copyright 2016      Jay D Dee   <jayddee246@gmail.com>
 * Copyright 2016-2017 XMRig       <support@xmrig.com>
 *
 *
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

#include <stdio.h>

#include "CryptoNight.h"

#if defined(XMRIG_ARM)
#   include "CryptoNight_arm.h"
#else
#   include "CryptoNight_x86.h"
#endif

void(*cryptonight_hash_ctx)(const void *input, size_t size, void *output, cryptonight_ctx *ctx) = nullptr;


static void cryptonight_av1_aesni(const void *input, size_t size, void *output, struct cryptonight_ctx *ctx) {
#   if !defined(XMRIG_ARMv7)
	cryptonight_hash<0x80000, MEMORY, 0x1FFFF0, false>(input, size, output, ctx);
#   endif
}


static void cryptonight_av2_aesni_double(const void *input, size_t size, void *output, cryptonight_ctx *ctx) {
#   if !defined(XMRIG_ARMv7)
	cryptonight_double_hash<0x80000, MEMORY, 0x1FFFF0, false>(input, size, output, ctx);
#   endif
}


static void cryptonight_av3_softaes(const void *input, size_t size, void *output, cryptonight_ctx *ctx) {
	cryptonight_hash<0x80000, MEMORY, 0x1FFFF0, true>(input, size, output, ctx);
}


static void cryptonight_av4_softaes_double(const void *input, size_t size, void *output, cryptonight_ctx *ctx) {
	cryptonight_double_hash<0x80000, MEMORY, 0x1FFFF0, true>(input, size, output, ctx);
}


#ifndef XMRIG_NO_AEON
static void cryptonight_lite_av1_aesni(const void *input, size_t size, void *output, cryptonight_ctx *ctx) {
#   if !defined(XMRIG_ARMv7)
	cryptonight_hash<0x40000, MEMORY_LITE, 0xFFFF0, false>(input, size, output, ctx);
#endif
}


static void cryptonight_lite_av2_aesni_double(const void *input, size_t size, void *output, cryptonight_ctx *ctx) {
#   if !defined(XMRIG_ARMv7)
	cryptonight_double_hash<0x40000, MEMORY_LITE, 0xFFFF0, false>(input, size, output, ctx);
#   endif
}


static void cryptonight_lite_av3_softaes(const void *input, size_t size, void *output, cryptonight_ctx *ctx) {
	cryptonight_hash<0x40000, MEMORY_LITE, 0xFFFF0, true>(input, size, output, ctx);
}


static void cryptonight_lite_av4_softaes_double(const void *input, size_t size, void *output, cryptonight_ctx *ctx) {
	cryptonight_double_hash<0x40000, MEMORY_LITE, 0xFFFF0, true>(input, size, output, ctx);
}

void(*cryptonight_variations[8])(const void *input, size_t size, void *output, cryptonight_ctx *ctx) = {
			cryptonight_av1_aesni,
			cryptonight_av2_aesni_double,
			cryptonight_av3_softaes,
			cryptonight_av4_softaes_double,
			cryptonight_lite_av1_aesni,
			cryptonight_lite_av2_aesni_double,
			cryptonight_lite_av3_softaes,
			cryptonight_lite_av4_softaes_double
};
#else
void(*cryptonight_variations[4])(const void *input, size_t size, void *output, cryptonight_ctx *ctx) = {
			cryptonight_av1_aesni,
			cryptonight_av2_aesni_double,
			cryptonight_av3_softaes,
			cryptonight_av4_softaes_double
};
#endif


extern "C"
{
	const static char input2[] = "This is a test";

	static void * create_ctx(int ratio) {
		struct cryptonight_ctx *ctx = (struct cryptonight_ctx*) _mm_malloc(sizeof(struct cryptonight_ctx), 16);
		ctx->memory = (uint8_t *)_mm_malloc(MEMORY * ratio, 16);
		return ctx;
	}


	static void free_ctx(struct cryptonight_ctx *ctx) {
		_mm_free(ctx->memory);
		_mm_free(ctx);
	}

	__declspec(dllexport) void hardware_hash(const uint8_t *input, const int size, uint8_t *output) {
		struct cryptonight_ctx *ctx = (struct cryptonight_ctx*) create_ctx(1);
		cryptonight_av1_aesni(input, size, output, ctx);
		free_ctx(ctx);
	}

	__declspec(dllexport) void software_hash(const uint8_t *input, const int size, uint8_t *output) {
		struct cryptonight_ctx *ctx = (struct cryptonight_ctx*) create_ctx(1);
		cryptonight_av3_softaes(input, size, output, ctx);
		free_ctx(ctx);
	}
}