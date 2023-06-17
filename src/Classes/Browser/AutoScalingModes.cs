namespace Random_Image.Classes.Browser;

/// <summary>
/// Defines modes of automatic scaling of images. The modes determine how images scale to fit into designated
/// canvas slots or into the entire canvas, while maintaining the original aspect ratio of the image.
/// </summary>
public enum AutoScalingModes
{
    /// <summary>
    /// Auto-scales images to the outside edges, i.e. the two more distant image edges touch the canvas edges.
    /// It makes images appear smaller and the entire image content is always visible.
    /// </summary>
    OutsideEdges = 1,

    /// <summary>
    /// Auto-scales images to the inside edges, i.e. the two closer image edges touch the canvas edges. It makes
    /// images appear larger. Images are cut, have identical visual sizes, and the visible part of the image content
    /// can be aligned with the mouse.
    /// </summary>
    InsideEdges = 2,

    /// <summary>
    /// Auto-scales images to the inside edges, i.e. the two closer image edges touch the canvas edges. It makes
    /// images appear larger. Images are cut, have identical visual sizes and rounded corners, and are centered.
    /// Used only when <see cref="ImageBrowserState.IsZoomed"/> == <see langword="true"/>.
    /// </summary>
    Thumbnails = 3,

    /// <summary>
    /// Disables scaling and uses the original image resolution. When image resolution is larger than canvas,
    /// the visible part of the image content can be aligned with the mouse. Used only when
    /// <see cref="ImageBrowserState.IsZoomed"/> == <see langword="true"/>.
    /// </summary>
    OriginalSize = Thumbnails,
}