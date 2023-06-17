namespace Random_Image.Classes.Browser;

/// <summary>
/// Defines reasons for loading and refreshing images.
/// </summary>
public enum Reasons
{
    /// <summary>
    /// Repaints the images without any additional transformations, e.g. after the window size changes.
    /// </summary>
    None = 0,

    /// <summary>
    /// First load after a restart of the application. No navigation animation and no loading icons.
    /// </summary>
    Restart,

    /// <summary>
    /// One image only: align the image with the mouse when the mouse enters or moves through the image.
    /// Depending on the length of the alignment jump, the transition is either animated or not.
    /// Used when the image is larger than canvas or its corresponding slot on canvas.
    /// </summary>
    Align,

    /// <summary>
    /// Animates aligning images back to center (default alignment), either when the mouse leaves an image or during
    /// the animated previewing of all images. Used when images are larger than canvas or canvas slots.
    /// </summary>
    AlignCenter,

    /// <summary>
    /// Animates sliding images to the top left corner. Used during the animated previewing of all images when images
    /// are larger than canvas slots.
    /// </summary>
    AlignTopLeft,

    /// <summary>
    /// Animates sliding images to the bottom right corner. Used during the animated previewing of all images when images
    /// are larger than canvas slots.
    /// </summary>
    AlignBottomRight,

    /// <summary>
    /// Restores image shadows when changing background to white. Note: Shadows are not painted on the black background.
    /// </summary>
    ChangeBackground,

    /// <summary>
    /// Changes the image layout, i.e. the arrangement and the number of images visible on the screen.
    /// </summary>
    ChangeLayout,

    /// <summary>
    /// Applies a rotation, mirror or toggles images upside-down.
    /// </summary>
    ChangeOrientation,

    /// <summary>
    /// Changes auto-scaling (inside edges, outside edges, thumbnails/original side), or applies
    /// an extra zoom to the image when the mouse wheel + Ctrl are activated in the zoomed mode.
    /// </summary>
    ChangeScaling,

    /// <summary>
    /// An image was clicked and will be shown on the entire canvas (zoomed mode).
    /// </summary>
    GoIn,

    /// <summary>
    /// Goes back to the normal mode showing more than one image on canvas.
    /// </summary>
    GoBack,

    /// <summary>
    /// One image only: an image is to be replaced with its alphabetic neighbor (activated with the mouse wheel when
    /// hovering /// the mouse over an image). Note: <see cref="ImageNeighbor"/>, <see cref="Restart"/>,
    /// and <see cref="Align"/> (if there is no jump) are the only values in this enumeration that do not trigger
    /// any animations.
    /// </summary>
    ImageNeighbor,

    /// <summary>
    /// The entire canvas was scrolled up or down or left or right.
    /// </summary>
    Navigate,

    /// <summary>
    /// Animates shaking images on screen.
    /// </summary>
    ShakeImages
}