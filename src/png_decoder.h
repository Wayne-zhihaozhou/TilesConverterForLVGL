/* png_decoder.h - minimal PNG loader wrapper around stb_image (public domain).
 * Loads a PNG as 8-bit RGBA (4 channels) into a freshly malloc'd buffer.
 * Caller releases with png_free(). Returns NULL on failure. */
#ifndef PNG_DECODER_H
#define PNG_DECODER_H

#ifdef __cplusplus
extern "C" {
#endif

unsigned char *png_load_rgba(const char *path, int *out_w, int *out_h);
void           png_free(unsigned char *img);

#ifdef __cplusplus
}
#endif

#endif /* PNG_DECODER_H */