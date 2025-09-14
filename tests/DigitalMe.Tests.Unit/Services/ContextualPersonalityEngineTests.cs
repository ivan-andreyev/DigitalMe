using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using DigitalMe.Services;
using DigitalMe.Services.PersonalityEngine;
using DigitalMe.Data.Entities;
using DigitalMe.Tests.Unit.Builders;
using Xunit;

namespace DigitalMe.Tests.Unit.Services;

public class ContextualPersonalityEngineTests
{
    private readonly Mock<ILogger<ContextualPersonalityEngine>> _mockLogger;
    private readonly Mock<IPersonalityContextAdapter> _mockContextAdapter;
    private readonly Mock<IStressBehaviorAnalyzer> _mockStressBehaviorAnalyzer;
    private readonly Mock<IExpertiseConfidenceAnalyzer> _mockExpertiseConfidenceAnalyzer;
    private readonly Mock<ICommunicationStyleAnalyzer> _mockCommunicationStyleAnalyzer;
    private readonly Mock<IContextAnalyzer> _mockContextAnalyzer;
    private readonly ContextualPersonalityEngine _engine;

    public ContextualPersonalityEngineTests()
    {
        _mockLogger = new Mock<ILogger<ContextualPersonalityEngine>>();
        _mockContextAdapter = new Mock<IPersonalityContextAdapter>();
        _mockStressBehaviorAnalyzer = new Mock<IStressBehaviorAnalyzer>();
        _mockExpertiseConfidenceAnalyzer = new Mock<IExpertiseConfidenceAnalyzer>();
        _mockCommunicationStyleAnalyzer = new Mock<ICommunicationStyleAnalyzer>();
        _mockContextAnalyzer = new Mock<IContextAnalyzer>();

        _engine = new ContextualPersonalityEngine(
            _mockLogger.Object,
            _mockContextAdapter.Object,
            _mockStressBehaviorAnalyzer.Object,
            _mockExpertiseConfidenceAnalyzer.Object,
            _mockCommunicationStyleAnalyzer.Object,
            _mockContextAnalyzer.Object);
    }

    private PersonalityProfile CreateIvanPersonality()
    {
        return PersonalityProfileBuilder.ForIvan()
            .WithTraits(new List<PersonalityTrait>
            {
                new() { Name = "Technical", Category = "Technical", Weight = 0.9 },
                new() { Name = "Decision Making", Category = "Personality", Weight = 1.0 },
                new() { Name = "Current Challenges", Category = "Personal", Weight = 0.8 }
            })
            .Build();
    }

    private PersonalityProfile CreateGenericPersonality()
    {
        return PersonalityProfileBuilder.Create()
            .WithName("GenericPersonality")
            .WithDescription("A generic personality for testing")
            .Build();
    }

    #region Context Adaptation Tests

    [Fact]
    public async Task AdaptPersonalityToContextAsync_WithTechnicalContext_ShouldBoostTechnicalTraits()
    {
        // Arrange
        var personality = CreateIvanPersonality();
        var context = new SituationalContext
        {
            ContextType = ContextType.Technical,
            Environment = EnvironmentType.Technical,
            Topic = "C# programming best practices",
            UrgencyLevel = 0.5,
            TimeOfDay = TimeOfDay.Morning
        };

        // Act
        var result = await _engine.AdaptPersonalityToContextAsync(personality, context);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(personality.Name);

        var technicalTrait = result.Traits?.FirstOrDefault(t => t.Name == "Technical");
        technicalTrait.Should().NotBeNull();
        technicalTrait!.Weight.Should().BeGreaterThan(0.9); // Should be boosted from original 0.9
    }

    [Fact]
    public async Task AdaptPersonalityToContextAsync_WithPersonalContext_ShouldBoostPersonalTraits()
    {
        // Arrange
        var personality = CreateIvanPersonality();
        var context = new SituationalContext
        {
            ContextType = ContextType.Personal,
            Environment = EnvironmentType.Personal,
            Topic = "family time",
            UrgencyLevel = 0.3,
            TimeOfDay = TimeOfDay.Evening
        };

        // Act
        var result = await _engine.AdaptPersonalityToContextAsync(personality, context);

        // Assert
        result.Should().NotBeNull();

        var challengesTrait = result.Traits?.FirstOrDefault(t => t.Name == "Current Challenges");
        challengesTrait.Should().NotBeNull();
        challengesTrait!.Weight.Should().BeGreaterThan(0.8); // Should be boosted from original 0.8
    }

    [Fact]
    public async Task AdaptPersonalityToContextAsync_WithHighUrgency_ShouldReduceCommunicationWeight()
    {
        // Arrange
        var personality = CreateIvanPersonality();
        personality.Traits?.Add(new PersonalityTrait
        {
            Name = "Communication",
            Category = "Communication",
            Weight = 0.8
        });

        var context = new SituationalContext
        {
            ContextType = ContextType.Professional,
            UrgencyLevel = 0.9, // High urgency
            TimeOfDay = TimeOfDay.Afternoon
        };

        // Act
        var result = await _engine.AdaptPersonalityToContextAsync(personality, context);

        // Assert
        result.Should().NotBeNull();

        var communicationTrait = result.Traits?.FirstOrDefault(t => t.Name == "Communication");
        communicationTrait.Should().NotBeNull();
        communicationTrait!.Weight.Should().BeLessThan(0.8); // Should be reduced due to high urgency
    }

    #endregion

    #region Stress and Time Pressure Tests

    [Fact]
    public void ModifyBehaviorForStressAndTime_WithIvanPersonality_ShouldApplyIvanSpecificPatterns()
    {
        // Arrange
        var personality = CreateIvanPersonality();
        var stressLevel = 0.7;
        var timePressure = 0.8;

        // Act
        var result = _engine.ModifyBehaviorForStressAndTime(personality, stressLevel, timePressure);

        // Assert
        result.Should().NotBeNull();
        result.StressLevel.Should().Be(stressLevel);
        result.TimePressure.Should().Be(timePressure);

        // Ivan specific: becomes more direct under stress
        result.DirectnessIncrease.Should().BeGreaterThan(0);
        result.DirectnessIncrease.Should().Be(stressLevel * 0.3);

        // Ivan specific: more structured when stressed
        result.StructuredThinkingBoost.Should().BeGreaterThan(0);

        // Ivan specific: solution-focused under time pressure
        result.SolutionFocusBoost.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ModifyBehaviorForStressAndTime_WithGenericPersonality_ShouldApplyGenericPatterns()
    {
        // Arrange
        var personality = CreateGenericPersonality();
        var stressLevel = 0.6;
        var timePressure = 0.7;

        // Act
        var result = _engine.ModifyBehaviorForStressAndTime(personality, stressLevel, timePressure);

        // Assert
        result.Should().NotBeNull();
        result.DirectnessIncrease.Should().Be(stressLevel * 0.2); // Generic multiplier
        result.TechnicalDetailReduction.Should().Be(timePressure * 0.3); // Generic multiplier
        result.SolutionFocusBoost.Should().Be(0); // Should not have Ivan-specific boosts
    }

    [Fact]
    public void ModifyBehaviorForStressAndTime_WithExtremeValues_ShouldClampToValidRange()
    {
        // Arrange
        var personality = CreateIvanPersonality();
        var stressLevel = 1.5; // Above max
        var timePressure = -0.2; // Below min

        // Act
        var result = _engine.ModifyBehaviorForStressAndTime(personality, stressLevel, timePressure);

        // Assert
        result.StressLevel.Should().Be(1.0); // Clamped to max
        result.TimePressure.Should().Be(0.0); // Clamped to min
    }

    #endregion

    #region Expertise Confidence Tests

    [Fact]
    public void AdjustConfidenceByExpertise_WithCSharpDomain_ShouldShowHighConfidence()
    {
        // Arrange
        var personality = CreateIvanPersonality();
        var domainType = DomainType.CSharpDotNet;
        var taskComplexity = 3; // Lower complexity to test high confidence scenario

        // Act
        var result = _engine.AdjustConfidenceByExpertise(personality, domainType, taskComplexity);

        // Assert
        result.Should().NotBeNull();
        result.Domain.Should().Be(domainType);
        result.TaskComplexity.Should().Be(taskComplexity);
        result.BaseConfidence.Should().Be(0.95); // Ivan's high C# expertise
        result.AdjustedConfidence.Should().BeGreaterThan(0.8); // Should remain high with moderate complexity
        result.DomainExpertiseBonus.Should().BeGreaterThan(0); // Should get Ivan's core competency bonus
    }

    [Fact]
    public void AdjustConfidenceByExpertise_WithWorkLifeBalanceDomain_ShouldShowLowerConfidence()
    {
        // Arrange
        var personality = CreateIvanPersonality();
        var domainType = DomainType.WorkLifeBalance;
        var taskComplexity = 3;

        // Act
        var result = _engine.AdjustConfidenceByExpertise(personality, domainType, taskComplexity);

        // Assert
        result.Should().NotBeNull();
        result.BaseConfidence.Should().Be(0.30); // Ivan's known weakness
        result.AdjustedConfidence.Should().BeLessThan(0.5); // Should be quite low
        result.KnownWeaknessReduction.Should().BeGreaterThan(0); // Should get weakness reduction
        result.ConfidenceExplanation.Should().Contain("weakness");
    }

    [Fact]
    public void AdjustConfidenceByExpertise_WithHighComplexity_ShouldReduceConfidence()
    {
        // Arrange
        var personality = CreateIvanPersonality();
        var domainType = DomainType.SoftwareArchitecture;
        var taskComplexity = 10; // Maximum complexity

        // Act
        var result = _engine.AdjustConfidenceByExpertise(personality, domainType, taskComplexity);

        // Assert
        result.Should().NotBeNull();
        result.TaskComplexity.Should().Be(10);
        result.AdjustedConfidence.Should().BeLessThan(result.BaseConfidence); // Should be reduced due to complexity
        result.ConfidenceExplanation.Should().Contain("Complex task reduces confidence");
    }

    [Fact]
    public void AdjustConfidenceByExpertise_WithGenericPersonality_ShouldUseGenericCalculation()
    {
        // Arrange
        var personality = CreateGenericPersonality();
        var domainType = DomainType.CSharpDotNet;
        var taskComplexity = 5;

        // Act
        var result = _engine.AdjustConfidenceByExpertise(personality, domainType, taskComplexity);

        // Assert
        result.Should().NotBeNull();
        result.BaseConfidence.Should().Be(0.6); // Generic base confidence
        result.DomainExpertiseBonus.Should().Be(0); // No Ivan-specific bonuses
        result.KnownWeaknessReduction.Should().Be(0); // No Ivan-specific reductions
    }

    #endregion

    #region Communication Style Tests

    [Fact]
    public void DetermineOptimalCommunicationStyle_WithTechnicalContext_ShouldReturnTechnicalStyle()
    {
        // Arrange
        var personality = CreateIvanPersonality();
        var context = new SituationalContext
        {
            ContextType = ContextType.Technical,
            Environment = EnvironmentType.Technical,
            UrgencyLevel = 0.5
        };

        // Act
        var result = _engine.DetermineOptimalCommunicationStyle(personality, context);

        // Assert
        result.Should().NotBeNull();
        result.BasePersonalityName.Should().Be("Ivan");
        result.FormalityLevel.Should().Be(0.4); // Low formality for technical contexts
        result.TechnicalDepth.Should().Be(0.9); // High technical depth
        result.DirectnessLevel.Should().Be(0.8); // High directness
        result.StyleSummary.Should().Contain("Technical expert mode");
    }

    [Fact]
    public void DetermineOptimalCommunicationStyle_WithFamilyContext_ShouldReturnWarmStyle()
    {
        // Arrange
        var personality = CreateIvanPersonality();
        var context = new SituationalContext
        {
            ContextType = ContextType.Family,
            Environment = EnvironmentType.Family,
            UrgencyLevel = 0.2
        };

        // Act
        var result = _engine.DetermineOptimalCommunicationStyle(personality, context);

        // Assert
        result.Should().NotBeNull();
        result.FormalityLevel.Should().Be(0.1); // Very informal for family
        result.WarmthLevel.Should().Be(0.9); // High warmth
        result.ProtectiveInstinct.Should().Be(0.85); // High protective instinct
        result.StyleSummary.Should().Contain("Family mode");
    }

    [Fact]
    public void DetermineOptimalCommunicationStyle_WithHighUrgency_ShouldAdjustForUrgency()
    {
        // Arrange
        var personality = CreateIvanPersonality();
        var context = new SituationalContext
        {
            ContextType = ContextType.Professional,
            UrgencyLevel = 0.9 // Very high urgency
        };

        // Act
        var result = _engine.DetermineOptimalCommunicationStyle(personality, context);

        // Assert
        result.Should().NotBeNull();
        result.DirectnessLevel.Should().BeGreaterThan(0.85); // Should be boosted for urgency
        result.StyleSummary.Should().Contain("urgent");
    }

    #endregion

    #region Context Analysis Tests

    [Fact]
    public void AnalyzeContextRequirements_WithHighUrgencyTechnicalContext_ShouldProvideCorrectAnalysis()
    {
        // Arrange
        var context = new SituationalContext
        {
            ContextType = ContextType.Technical,
            Environment = EnvironmentType.Professional,
            UrgencyLevel = 0.8,
            Topic = "Production bug fix"
        };

        // Act
        var result = _engine.AnalyzeContextRequirements(context);

        // Assert
        result.Should().NotBeNull();
        result.Context.Should().Be(context);
        result.AnalysisTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        result.RequiredResponseSpeed.Should().Be(ResponseSpeed.Immediate); // High urgency
        result.RecommendedDetailLevel.Should().Be(DetailLevel.High); // Technical context
        result.RecommendedFormalityLevel.Should().Be(0.7); // Professional environment
        result.RecommendedEmotionalTone.Should().Be(EmotionalTone.Focused); // High urgency

        result.AdaptationRecommendations.Should().NotBeEmpty();
        result.AdaptationRecommendations.Should().Contain(r => r.Contains("directness"));
        result.AdaptationRecommendations.Should().Contain(r => r.Contains("technical"));
    }

    [Fact]
    public void AnalyzeContextRequirements_WithPersonalContext_ShouldProvidePersonalAnalysis()
    {
        // Arrange
        var context = new SituationalContext
        {
            ContextType = ContextType.Personal,
            Environment = EnvironmentType.Personal,
            UrgencyLevel = 0.3,
            Topic = "Work-life balance discussion"
        };

        // Act
        var result = _engine.AnalyzeContextRequirements(context);

        // Assert
        result.Should().NotBeNull();
        result.RequiredResponseSpeed.Should().Be(ResponseSpeed.Thoughtful); // Low urgency
        result.RecommendedDetailLevel.Should().Be(DetailLevel.Medium); // Personal context
        result.RecommendedFormalityLevel.Should().Be(0.2); // Personal environment
        result.RecommendedEmotionalTone.Should().Be(EmotionalTone.Reflective); // Personal context

        result.AdaptationRecommendations.Should().Contain(r => r.Contains("emotional awareness"));
        result.AdaptationRecommendations.Should().Contain(r => r.Contains("work-life balance"));
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task AdaptPersonalityToContextAsync_WithNullTraits_ShouldHandleGracefully()
    {
        // Arrange
        var personality = CreateIvanPersonality();
        personality.Traits = null; // Null traits

        var context = new SituationalContext
        {
            ContextType = ContextType.Technical,
            UrgencyLevel = 0.5
        };

        // Act
        var result = await _engine.AdaptPersonalityToContextAsync(personality, context);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(personality.Name);
        // Should not throw exception even with null traits
    }

    [Fact]
    public void ModifyBehaviorForStressAndTime_WithZeroValues_ShouldReturnBaseModifications()
    {
        // Arrange
        var personality = CreateIvanPersonality();
        var stressLevel = 0.0;
        var timePressure = 0.0;

        // Act
        var result = _engine.ModifyBehaviorForStressAndTime(personality, stressLevel, timePressure);

        // Assert
        result.Should().NotBeNull();
        result.StressLevel.Should().Be(0.0);
        result.TimePressure.Should().Be(0.0);
        result.DirectnessIncrease.Should().Be(0.0);
        result.StructuredThinkingBoost.Should().Be(0.0);
    }

    #endregion
}