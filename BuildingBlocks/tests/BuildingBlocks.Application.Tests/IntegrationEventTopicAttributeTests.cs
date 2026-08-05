using BuildingBlocks.Application.IntegrationEvents;

namespace BuildingBlocks.Application.Tests;

public class IntegrationEventTopicAttributeTests
{
    [Theory]
    [InlineData("nutrition.recipe-created")]
    [InlineData("sample.widget-created")]
    [InlineData("fitness.workout-session-completed-v2")]
    [InlineData("a.b")]
    public void Constructor_WithContextDotEventInKebabCase_ExposesTheTopic(string topic)
    {
        var attribute = new IntegrationEventTopicAttribute(topic);

        Assert.Equal(topic, attribute.Topic);
    }

    [Theory]
    [InlineData("recipe-created")]
    [InlineData("nutrition.recipe.created")]
    [InlineData("Nutrition.recipe-created")]
    [InlineData("nutrition.Recipe-Created")]
    [InlineData("nutrition.recipe--created")]
    [InlineData("nutrition.-recipe-created")]
    [InlineData("nutrition.recipe-created-")]
    [InlineData("nutrition.")]
    [InlineData(".recipe-created")]
    [InlineData("nutrition.recipe_created")]
    [InlineData("nutrition.recipe created")]
    public void Constructor_WithInvalidTopic_Throws(string topic)
    {
        var exception = Assert.Throws<ArgumentException>(() => new IntegrationEventTopicAttribute(topic));

        Assert.Contains(topic, exception.Message, StringComparison.Ordinal);
        Assert.Contains("<context>.<event>", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyTopic_Throws(string topic)
    {
        Assert.Throws<ArgumentException>(() => new IntegrationEventTopicAttribute(topic));
    }

    [Fact]
    public void Constructor_WithNullTopic_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new IntegrationEventTopicAttribute(null!));
    }
}
