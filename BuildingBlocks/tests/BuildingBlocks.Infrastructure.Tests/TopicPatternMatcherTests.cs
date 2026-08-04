using BuildingBlocks.Infrastructure.Messaging.IntegrationEvents;
using BuildingBlocks.Infrastructure.Messaging;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class TopicPatternMatcherTests
{
    [Theory]
    [InlineData("nutrition.*", "nutrition.recipe-created")]
    [InlineData("*.recipe-created", "nutrition.recipe-created")]
    [InlineData("nutrition.recipe-created", "nutrition.recipe-created")]
    [InlineData("#", "nutrition.recipe-created")]
    [InlineData("nutrition.#", "nutrition.recipe-created")]
    [InlineData("nutrition.#", "nutrition.recipe.created.v2")]
    [InlineData("nutrition.#", "nutrition")]
    [InlineData("#.created", "nutrition.recipe.created")]
    public void MatchingPattern_IsRecognised(string pattern, string topic) =>
        Assert.True(TopicPatternMatcher.Matches(pattern, topic));

    [Theory]
    [InlineData("nutrition.*", "fitness.recipe-created")]
    [InlineData("nutrition.*", "nutrition.recipe.created")]
    [InlineData("nutrition.*", "nutrition")]
    [InlineData("nutrion.*", "nutrition.recipe-created")]
    [InlineData("nutrition.recipe-created", "nutrition.recipe-createdx")]
    [InlineData("nutrition.#", "fitness.recipe-created")]
    public void NonMatchingPattern_IsRejected(string pattern, string topic) =>
        Assert.False(TopicPatternMatcher.Matches(pattern, topic));

    [Fact]
    public void SingleWildcard_MatchesExactlyOneWord()
    {
        Assert.True(TopicPatternMatcher.Matches("*.*", "a.b"));
        Assert.False(TopicPatternMatcher.Matches("*.*", "a.b.c"));
        Assert.False(TopicPatternMatcher.Matches("*.*", "a"));
    }
}
