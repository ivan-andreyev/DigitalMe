using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DigitalMe.Services;

namespace DigitalMe.Tests.Unit.Services;

/// <summary>
/// Unit tests for ProfileDataParser.
/// Tests parsing of Ivan's profile data from markdown format.
/// </summary>
public class ProfileDataParserTests
{
    private readonly Mock<ILogger<ProfileDataParser>> _mockLogger;
    private readonly ProfileDataParser _parser;

    public ProfileDataParserTests()
    {
        _mockLogger = new Mock<ILogger<ProfileDataParser>>();
        _parser = new ProfileDataParser(_mockLogger.Object);
    }

    [Fact]
    public async Task ParseProfileDataAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = "non-existent-file.md";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _parser.ParseProfileDataAsync(nonExistentPath));
    }

    [Fact]
    public async Task ParseProfileDataAsync_WithValidMarkdownContent_ShouldParseBasicInfo()
    {
        // Arrange
        var tempFilePath = CreateTempProfileFile(GetSampleProfileContent());

        try
        {
            // Act
            var result = await _parser.ParseProfileDataAsync(tempFilePath);

            // Assert
            Assert.Equal("Иван", result.Name);
            Assert.Equal(34, result.Age);
            Assert.Equal("г. Орск, РФ", result.Origin);
            Assert.Equal("Батуми, Грузия", result.CurrentLocation);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ParseProfileDataAsync_WithValidMarkdownContent_ShouldParseFamilyInfo()
    {
        // Arrange
        var tempFilePath = CreateTempProfileFile(GetSampleProfileContent());

        try
        {
            // Act
            var result = await _parser.ParseProfileDataAsync(tempFilePath);

            // Assert
            Assert.Equal("Марина", result.Family.WifeName);
            Assert.Equal(33, result.Family.WifeAge);
            Assert.Equal("София", result.Family.DaughterName);
            Assert.Equal(3.5, result.Family.DaughterAge);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ParseProfileDataAsync_WithValidMarkdownContent_ShouldParseProfessionalInfo()
    {
        // Arrange
        var tempFilePath = CreateTempProfileFile(GetSampleProfileContent());

        try
        {
            // Act
            var result = await _parser.ParseProfileDataAsync(tempFilePath);

            // Assert
            Assert.Equal("Head of R&D в EllyAnalytics inc", result.Professional.Position);
            Assert.Equal("EllyAnalytics", result.Professional.Company);
            Assert.Equal("4 года 4 месяца программирования", result.Professional.Experience);
            Assert.Equal("Junior → Team Lead за 4 года 1 месяц", result.Professional.CareerPath);
            Assert.Contains("Unity indie game framework", result.Professional.PetProjects);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ParseProfileDataAsync_WithValidMarkdownContent_ShouldParsePersonalityTraits()
    {
        // Arrange
        var tempFilePath = CreateTempProfileFile(GetSampleProfileContent());

        try
        {
            // Act
            var result = await _parser.ParseProfileDataAsync(tempFilePath);

            // Assert
            Assert.NotEmpty(result.Personality.CoreValues);
            Assert.Contains("Financial independence and career confidence", result.Personality.CoreValues);
            Assert.Contains("Family safety and daughter's opportunities", result.Personality.CoreValues);

            Assert.NotEmpty(result.Personality.WorkStyle);
            Assert.Contains("Rational and structured decision making", result.Personality.WorkStyle);
            Assert.Contains("High-intensity work ethic", result.Personality.WorkStyle);

            Assert.NotEmpty(result.Personality.Challenges);
            Assert.Contains("Balancing work demands with family time", result.Personality.Challenges);

            Assert.NotEmpty(result.Personality.Motivations);
            Assert.Contains("Financial security", result.Personality.Motivations);
            Assert.Contains("Technical innovation and problem-solving", result.Personality.Motivations);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ParseProfileDataAsync_WithValidMarkdownContent_ShouldParseTechnicalPreferences()
    {
        // Arrange
        var tempFilePath = CreateTempProfileFile(GetSampleProfileContent());

        try
        {
            // Act
            var result = await _parser.ParseProfileDataAsync(tempFilePath);

            // Assert
            Assert.NotEmpty(result.TechnicalPreferences);
            Assert.Contains("C#/.NET development stack", result.TechnicalPreferences);
            Assert.Contains("Strong typing and type safety", result.TechnicalPreferences);
            Assert.Contains("Code generation approaches", result.TechnicalPreferences);
            Assert.Contains("Client-server architecture patterns", result.TechnicalPreferences);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ParseProfileDataAsync_WithValidMarkdownContent_ShouldParseGoals()
    {
        // Arrange
        var tempFilePath = CreateTempProfileFile(GetSampleProfileContent());

        try
        {
            // Act
            var result = await _parser.ParseProfileDataAsync(tempFilePath);

            // Assert
            Assert.NotEmpty(result.Goals);
            Assert.Contains("Achieve financial independence", result.Goals);
            Assert.Contains("Build successful Unity game framework", result.Goals);
            Assert.Contains("Eventually relocate family to USA", result.Goals);
            Assert.Contains("Advance in R&D leadership role", result.Goals);
            Assert.Contains("Balance professional success with family life", result.Goals);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ParseProfileDataAsync_WithInvalidMarkdown_ShouldReturnPartialData()
    {
        // Arrange
        var invalidContent = """
            # Some Random Header
            This is not properly formatted profile data.
            **Random:** value
            """;

        var tempFilePath = CreateTempProfileFile(invalidContent);

        try
        {
            // Act
            var result = await _parser.ParseProfileDataAsync(tempFilePath);

            // Assert
            Assert.NotNull(result);
            // Should still return object with default/empty values
            Assert.Equal(string.Empty, result.Name);
            Assert.Equal(0, result.Age);
            Assert.NotNull(result.Family);
            Assert.NotNull(result.Professional);
            Assert.NotNull(result.Personality);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ParseProfileDataAsync_WithEmptyFile_ShouldReturnEmptyData()
    {
        // Arrange
        var tempFilePath = CreateTempProfileFile(string.Empty);

        try
        {
            // Act
            var result = await _parser.ParseProfileDataAsync(tempFilePath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(string.Empty, result.Name);
            Assert.Equal(0, result.Age);
            Assert.NotNull(result.Family);
            Assert.Equal(string.Empty, result.Family.WifeName);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ParseProfileDataAsync_WithException_ShouldThrowAndLog()
    {
        // Arrange - Use a path that will cause an exception
        var invalidPath = "C:\\invalid\\path\\that\\does\\not\\exist.md";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _parser.ParseProfileDataAsync(invalidPath));

        // Verify exception message
        Assert.Contains("Profile data file not found", exception.Message);
    }

    [Fact]
    public async Task ParseProfileDataAsync_ShouldLogParsingStart()
    {
        // Arrange
        var tempFilePath = CreateTempProfileFile(GetSampleProfileContent());

        try
        {
            // Act
            await _parser.ParseProfileDataAsync(tempFilePath);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Parsing Ivan's profile data")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    [Fact]
    public async Task ParseProfileDataAsync_ShouldLogSuccessfulCompletion()
    {
        // Arrange
        var tempFilePath = CreateTempProfileFile(GetSampleProfileContent());

        try
        {
            // Act
            await _parser.ParseProfileDataAsync(tempFilePath);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully parsed profile data")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            File.Delete(tempFilePath);
        }
    }

    #region Helper Methods

    private string CreateTempProfileFile(string content)
    {
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, content);
        return tempPath;
    }

    private string GetSampleProfileContent()
    {
        return """
            # Профиль личности: Иван

            ## Базовые данные
            - **Имя:** Иван
            - **Возраст:** 34 года  
            - **Происхождение:** г. Орск, РФ
            - **Текущее местоположение:** Батуми, Грузия
            - **Семья:** Жена Марина (33), дочь София (3.5)

            ## Профессиональная сфера
            - **Должность:** Head of R&D в EllyAnalytics inc
            - **Опыт:** 4 года 4 месяца программирования
            - **Карьерный путь:** Junior → Team Lead за 4 года 1 месяц
            - **Текущий статус:** 3 месяца на новой позиции
            - **Самооценка:** "Стараюсь выкладываться по максимуму, работаю очень интенсивно"

            ## Побочные проекты
            - **Основной pet-project:** Фреймворк для инди-игр на Unity
            - **Цель:** Клиент-серверная расширяемая архитектура для многих жанров
            - **Подход:** Генерация контента вместо работы в Unity Editor
            - **Мотивация:** Финансовая независимость и карьерная уверенность

            ## Жизненные приоритеты и время
            - **Рабочее время:** Доминирует в расписании
            - **Семейное время:** 1-2 часа в день в среднем (включая выходные)
            - **Отношение к семье:** "Очень люблю, но провожу катастрофически мало времени"

            ## Стиль коммуникации
            - **Общение:** Открыто, дружелюбно, избегает провокаций

            ## Стиль принятия решений
            - **Процесс:** Определение значимых факторов → взвешивание → оценка результата → решение/итерация
            """;
    }

    #endregion
}