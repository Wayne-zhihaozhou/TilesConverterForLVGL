namespace lvglConverter
{
    /// <summary>
    /// Target pixel format for the generated LVGL binary image.
    /// RGB332 / RGB888 are reserved placeholders (not yet implemented in the original).
    /// </summary>
    public enum LVGLImageType
    {
        RGB332,
        RGB565,
        RGB565SWAP,
        RGB888
    }
}
