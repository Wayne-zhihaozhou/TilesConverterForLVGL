# Third-Party Attribution

This project bundles two header-only libraries from the "STB" collection by
Sean Barrett ("nothings.org"). They are released into the **public domain** and
are included verbatim under their own terms (see below). No additional
restriction is imposed by this project on them.

## stb_image  (src/stb_image.h)
- Version: v2.34
- Author : Sean Barrett
- Source : http://nothings.org/stb
- License: Public Domain / Unlicense

## stb_image_write  (src/stb_image_write.h)
- Author : Sean Barrett
- Source : http://nothings.org/stb
- License: Public Domain / Unlicense

---

The standard public-domain/unlicense notice that ships inside these headers reads:

    This is free and unencumbered software released into the public domain.
    Anyone is free to copy, modify, publish, use, compile, sell, or distribute
    this software, either in source code form or as a compiled binary, for any
    purpose, commercial or non-commercial, and by any means.

    In jurisdictions that recognize copyright laws, the author or authors of
    this software dedicate any and all copyright interest in the software to
    the public domain. We make this dedication for the benefit of the public
    at large and to the detriment of our heirs and successors. We intend this
    dedication to be an overt act of relinquishment in perpetuity of all
    present and future rights to this software under copyright law.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
    ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Note: stb_image decodes PNGs into an RGBA buffer. For security-sensitive inputs
consider the Wuffs decoder (https://github.com/google/wuffs-mirror-release-c),
which is not public domain but is available under MIT / Apache licenses.
