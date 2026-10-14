using JumpRunShoot.Engine.Physics;
using Xunit;

namespace JumpRunShoot.Tests;

public class AabbTests
{
    [Fact]
    public void Overlaps_WhenBoxesIntersect_ReturnsTrue()
    {
        var boxA = new AABB(10, 10, 20, 20);
        var boxB = new AABB(15, 15, 20, 20);

        Assert.True(boxA.Overlaps(boxB));
        Assert.True(boxB.Overlaps(boxA));
    }

    [Fact]
    public void Overlaps_WhenBoxesSeparatedHorizontally_ReturnsFalse()
    {
        var boxA = new AABB(10, 10, 20, 20);
        var boxB = new AABB(35, 10, 20, 20);

        Assert.False(boxA.Overlaps(boxB));
        Assert.False(boxB.Overlaps(boxA));
    }

    [Fact]
    public void Overlaps_WhenBoxesSeparatedVertically_ReturnsFalse()
    {
        var boxA = new AABB(10, 10, 20, 20);
        var boxB = new AABB(10, 35, 20, 20);

        Assert.False(boxA.Overlaps(boxB));
        Assert.False(boxB.Overlaps(boxA));
    }

    [Fact]
    public void Overlaps_WhenTouchingEdgesOnly_ReturnsFalse()
    {
        // Touching exactly at X = 30 without penetration
        var boxA = new AABB(10, 10, 20, 20); // Right = 30
        var boxB = new AABB(30, 10, 20, 20); // Left = 30

        Assert.False(boxA.Overlaps(boxB));
        Assert.False(boxB.Overlaps(boxA));
    }

    [Fact]
    public void Contains_PointInside_ReturnsTrue()
    {
        var box = new AABB(10, 10, 20, 20);
        Assert.True(box.Contains(new Vector2f(15, 15)));
        Assert.True(box.Contains(new Vector2f(10, 10)));
        Assert.True(box.Contains(new Vector2f(30, 30)));
    }

    [Fact]
    public void Contains_PointOutside_ReturnsFalse()
    {
        var box = new AABB(10, 10, 20, 20);
        Assert.False(box.Contains(new Vector2f(5, 15)));
        Assert.False(box.Contains(new Vector2f(35, 15)));
        Assert.False(box.Contains(new Vector2f(15, 5)));
        Assert.False(box.Contains(new Vector2f(15, 35)));
    }
}
