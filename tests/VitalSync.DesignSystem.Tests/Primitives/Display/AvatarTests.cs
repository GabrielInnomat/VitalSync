using Bunit;
using VitalSync.DesignSystem.Primitives.Display;

namespace VitalSync.DesignSystem.Tests.Primitives.Display;

public sealed class AvatarTests : BunitContext
{
    [Theory]
    [InlineData("Anna Maria Muster", "AM")]
    [InlineData("Anna", "A")]
    [InlineData("  émile   zola ", "ÉZ")]
    [InlineData("A\u030Angström Weber", "A\u030AW")]
    public void Renders_the_initials_of_the_first_and_last_word(string name, string expected)
    {
        var cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Name, name));

        Assert.Equal(expected, cut.Find(".vs-avatar__initials").TextContent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Throws_when_no_Name_is_given(string name)
    {
        Assert.Throws<InvalidOperationException>(() =>
            Render<Avatar>(parameters => parameters
                .Add(p => p.Name, name)));
    }

    [Fact]
    public void Exposes_a_standalone_avatar_as_one_image_named_after_the_person()
    {
        var cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Name, " Anna Muster ")
            .Add(p => p.Src, "/anna.jpg"));

        var avatar = cut.Find(".vs-avatar");
        Assert.Equal("img", avatar.GetAttribute("role"));
        Assert.Equal("Anna Muster", avatar.GetAttribute("aria-label"));
        Assert.False(avatar.HasAttribute("aria-hidden"));
        Assert.Equal("true", cut.Find(".vs-avatar__photo").GetAttribute("aria-hidden"));
        Assert.Equal("true", cut.Find(".vs-avatar__initials").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Hides_a_decorative_avatar_from_assistive_technology()
    {
        var cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Name, "Anna Muster")
            .Add(p => p.Decorative, true));

        var avatar = cut.Find(".vs-avatar");
        Assert.Equal("true", avatar.GetAttribute("aria-hidden"));
        Assert.False(avatar.HasAttribute("role"));
        Assert.False(avatar.HasAttribute("aria-label"));
    }

    [Fact]
    public void Keeps_the_initials_beneath_the_photo_as_its_fallback()
    {
        var cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Name, "Anna Muster")
            .Add(p => p.Src, "/anna.jpg"));

        Assert.Equal("AM", cut.Find(".vs-avatar__initials").TextContent);
        Assert.Equal(
            "background-image: url(\"/anna.jpg\")",
            cut.Find(".vs-avatar__photo").GetAttribute("style"));
    }

    [Fact]
    public void Escapes_the_Src_so_it_cannot_leave_the_css_string()
    {
        var cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Name, "Anna Muster")
            .Add(p => p.Src, "/a\"); color: red; x(\\\nb"));

        Assert.Equal(
            "background-image: url(\"/a\\\"); color: red; x(\\\\\\A b\")",
            cut.Find(".vs-avatar__photo").GetAttribute("style"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Renders_no_photo_without_a_Src(string? src)
    {
        var cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Name, "Anna Muster")
            .Add(p => p.Src, src));

        Assert.Empty(cut.FindAll(".vs-avatar__photo"));
    }

    [Theory]
    [InlineData(null, "vs-avatar--medium")]
    [InlineData(AvatarSize.Small, "vs-avatar--small")]
    [InlineData(AvatarSize.Large, "vs-avatar--large")]
    public void Renders_the_size_class(AvatarSize? size, string expectedClass)
    {
        var cut = Render<Avatar>(parameters =>
        {
            parameters.Add(p => p.Name, "Anna Muster");
            if (size is { } value)
            {
                parameters.Add(p => p.Size, value);
            }
        });

        Assert.Contains(expectedClass, cut.Find(".vs-avatar").ClassList);
    }

    [Fact]
    public void Keeps_its_own_accessibility_attributes_over_passed_ones()
    {
        var cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Name, "Anna Muster")
            .AddUnmatched("role", "button")
            .AddUnmatched("aria-label", "Anderer Name")
            .AddUnmatched("class", "extra"));

        var avatar = cut.Find(".vs-avatar");
        Assert.Equal("img", avatar.GetAttribute("role"));
        Assert.Equal("Anna Muster", avatar.GetAttribute("aria-label"));
        Assert.Contains("vs-avatar--medium", avatar.ClassList);
        Assert.Contains("extra", avatar.ClassList);
    }
}
