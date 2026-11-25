/* png_decoder.c - single translation unit that compiles stb_image inline. */
#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"
#include "png_decoder.h"

/* req_comp = 4 forces an RGBA (8-bit per channel) output buffer, matching the
 * straight R/G/B values the original System.Drawing.GetPixel path used for packing. */
unsigned char *png_load_rgba(const char *path, int *out_w, int *out_h)
{
    return stbi_load(path, out_w, out_h, NULL, 4);
}

void png_free(unsigned char *img)
{
    stbi_image_free(img);
}