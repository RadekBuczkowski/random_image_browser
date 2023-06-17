namespace Random_Image.Classes.Cache;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Random_Image.Classes.Utilities;

/// <summary>
/// Implements the base class for the <see cref="ImageTag"/> class.
/// Note: Only the base class is used when caching image files and for setting the order of images on canvas.
/// </summary>
[Serializable]
public class ImageCacheTag
{
    /// <summary>
    /// The index in the sequence of shuffled images (defining the order of images on canvas).
    /// </summary>
    [JsonInclude]
    public int Index { get; private set; }

    /// <summary>
    /// Corresponding group number to get to the image filename.
    /// </summary>
    [JsonInclude]
    public int GroupIndex { get; private set; }

    /// <summary>
    /// Corresponding index inside the group to get to the image filename.
    /// </summary>
    [JsonInclude]
    public int GroupItemIndex { get; private set; }

    /// <summary>
    /// Count of all items in the group specified by <see cref="GroupIndex"/>.
    /// </summary>
    [JsonInclude]
    public int ItemCountWithinGroup { get; private set; }

    /// <summary>
    /// Modified when the mouse wheel is used while hovering over an image and the neighbor of the image file
    /// gets loaded instead. Contains the distance between the original image and the neighbor.
    /// </summary>
    [JsonInclude]
    public int NeighborDelta { get; private set; }

    /// <summary>
    /// Last time when <see cref="NeighborDelta"/> was set to 0. We use it to prevent going over 0 too fast
    /// while scrolling the mouse wheel.
    /// </summary>
    private long _neighborResetTime = 0;

    public ImageCacheTag()
    {
        Index = 0;
        GroupIndex = 0;
        GroupItemIndex = 0;
        ItemCountWithinGroup = 0;
        NeighborDelta = 0;
    }

    public ImageCacheTag(int groupIndex, int groupItemIndex, int itemCountWithinGroup) : this()
    {
        GroupIndex = groupIndex;
        GroupItemIndex = groupItemIndex;
        ItemCountWithinGroup = itemCountWithinGroup;
    }

    protected void CopyFrom(ImageCacheTag tag)
    {
        if (tag == null)
            return;
        Index = tag.Index;
        GroupIndex = tag.GroupIndex;
        GroupItemIndex = tag.GroupItemIndex;
        ItemCountWithinGroup = tag.ItemCountWithinGroup;
        NeighborDelta = tag.NeighborDelta;
    }

    /// <summary>
    /// Compares two <see cref="ImageCacheTag"/> objects. Objects are identical when they have
    /// the same <see cref="Index"/>, also for classes inheriting from <see cref="ImageCacheTag"/>.
    /// </summary>
    public override bool Equals(object obj)
    {
        return Index == (obj as ImageCacheTag)?.Index;
    }

    /// <summary>
    /// Returns the hash code for this instance. Objects are identical if they have the same <see cref="Index"/>,
    /// also for classes inheriting from <see cref="ImageCacheTag"/>.
    /// </summary>
    public override int GetHashCode() => Index.GetHashCode();

    /// <summary>
    /// Sets the <see cref="Index"/> property.
    /// </summary>
    public void SetIndex(int index)
    {
        Index = index;
    }

    /// <summary>
    /// Marks the image as ready to be recycled or removed.
    /// </summary>
    public void Expire()
    {
        Index = int.MinValue;
    }

    /// <summary>
    /// Goes to the direct neighbor of the image file in the file folder distant by <paramref name="delta"/>.
    /// </summary>
    public void GoToNeighbor(int delta, List<List<string>> groups)
    {
        if (delta != 0 && SimpleTimer.IsTimerRunning(ref _neighborResetTime, SimpleTimer.BlockMouseWheelTime) == false)
        {
            if (NeighborDelta != 0 && Math.Sign(NeighborDelta) != Math.Sign(NeighborDelta + delta))
            {
                delta = -NeighborDelta;
                SimpleTimer.StartTimer(ref _neighborResetTime);
            }
            NeighborDelta += delta;
            GroupItemIndex += delta;
            int count = groups[GroupIndex].Count;
            while (GroupItemIndex >= count)
            {
                GroupItemIndex -= count;
                GroupIndex = (GroupIndex + 1) % groups.Count;
                count = groups[GroupIndex].Count;
            }
            while (GroupItemIndex < 0)
            {
                GroupIndex = (GroupIndex - 1 + groups.Count) % groups.Count;
                count = groups[GroupIndex].Count;
                GroupItemIndex += count;
            }
            ItemCountWithinGroup = groups[GroupIndex].Count;
        }
    }
}