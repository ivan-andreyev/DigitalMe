using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services;

/// <summary>
/// Интерфейс для управления конфигурациями личностей.
/// Предоставляет экстернализованные настройки для различных персоналий.
/// </summary>
public interface IPersonalityConfigurationService
{
    /// <summary>
    /// Получает уровни экспертизы для указанной персоналии.
    /// </summary>
    /// <param name="personalityName">Имя персоналии (например, "Ivan")</param>
    /// <returns>Словарь доменов и их уровней экспертизы</returns>
    Dictionary<DomainType, double> GetExpertiseLevels(string personalityName);

    /// <summary>
    /// Получает правила поведения для указанной персоналии.
    /// </summary>
    /// <param name="personalityName">Имя персоналии</param>
    /// <returns>Правила поведенческих модификаций</returns>
    PersonalityBehaviorRules GetBehaviorRules(string personalityName);

    /// <summary>
    /// Получает стрессовые модификаторы для указанной персоналии.
    /// </summary>
    /// <param name="personalityName">Имя персоналии</param>
    /// <returns>Настройки стрессового поведения</returns>
    StressModificationRules GetStressModificationRules(string personalityName);

    /// <summary>
    /// Получает настройки коммуникационного стиля для персоналии.
    /// </summary>
    /// <param name="personalityName">Имя персоналии</param>
    /// <returns>Правила коммуникационного стиля</returns>
    CommunicationStyleRules GetCommunicationStyleRules(string personalityName);

    /// <summary>
    /// Проверяет, поддерживается ли указанная персоналия.
    /// </summary>
    /// <param name="personalityName">Имя персоналии для проверки</param>
    /// <returns>True, если персоналия поддерживается</returns>
    bool IsPersonalitySupported(string personalityName);

    /// <summary>
    /// Получает список всех поддерживаемых персоналий.
    /// </summary>
    /// <returns>Список имен поддерживаемых персоналий</returns>
    IEnumerable<string> GetSupportedPersonalities();
}

/// <summary>
/// Реализация сервиса конфигурации личностей.
/// Загружает настройки из конфигурации и предоставляет типизированный доступ.
/// </summary>
public class PersonalityConfigurationService : IPersonalityConfigurationService
{
    private readonly ILogger<PersonalityConfigurationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, PersonalityConfiguration> _configurations;

    public PersonalityConfigurationService(
        ILogger<PersonalityConfigurationService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _configurations = LoadConfigurations();
    }

    public Dictionary<DomainType, double> GetExpertiseLevels(string personalityName)
    {
        if (!_configurations.TryGetValue(personalityName.ToLowerInvariant(), out var config))
        {
            _logger.LogWarning("No expertise configuration found for personality {PersonalityName}, returning generic levels", personalityName);
            return GetGenericExpertiseLevels();
        }

        return config.ExpertiseLevels;
    }

    public PersonalityBehaviorRules GetBehaviorRules(string personalityName)
    {
        if (!_configurations.TryGetValue(personalityName.ToLowerInvariant(), out var config))
        {
            _logger.LogWarning("No behavior rules found for personality {PersonalityName}, returning generic rules", personalityName);
            return GetGenericBehaviorRules();
        }

        return config.BehaviorRules;
    }

    public StressModificationRules GetStressModificationRules(string personalityName)
    {
        if (!_configurations.TryGetValue(personalityName.ToLowerInvariant(), out var config))
        {
            _logger.LogWarning("No stress rules found for personality {PersonalityName}, returning generic rules", personalityName);
            return GetGenericStressRules();
        }

        return config.StressRules;
    }

    public CommunicationStyleRules GetCommunicationStyleRules(string personalityName)
    {
        if (!_configurations.TryGetValue(personalityName.ToLowerInvariant(), out var config))
        {
            _logger.LogWarning("No communication rules found for personality {PersonalityName}, returning generic rules", personalityName);
            return GetGenericCommunicationRules();
        }

        return config.CommunicationRules;
    }

    public bool IsPersonalitySupported(string personalityName)
    {
        return _configurations.ContainsKey(personalityName.ToLowerInvariant());
    }

    public IEnumerable<string> GetSupportedPersonalities()
    {
        return _configurations.Keys;
    }

    #region Private Helper Methods

    private Dictionary<string, PersonalityConfiguration> LoadConfigurations()
    {
        var configurations = new Dictionary<string, PersonalityConfiguration>();

        try
        {
            // Загружаем Ivan configuration из кода (fallback)
            configurations.Add("ivan", CreateIvanConfiguration());

            // Пытаемся загрузить из appsettings.json если секция есть
            var configSection = _configuration.GetSection("PersonalityConfigurations");
            if (configSection.Exists())
            {
                LoadConfigurationsFromSettings(configurations, configSection);
            }

            _logger.LogInformation("Loaded configurations for {PersonalityCount} personalities: {PersonalityNames}",
                configurations.Count, string.Join(", ", configurations.Keys));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load personality configurations, using hardcoded Ivan configuration only");

            // Ensure Ivan configuration is always available
            if (!configurations.ContainsKey("ivan"))
            {
                configurations.Add("ivan", CreateIvanConfiguration());
            }
        }

        return configurations;
    }

    private void LoadConfigurationsFromSettings(Dictionary<string, PersonalityConfiguration> configurations, IConfigurationSection configSection)
    {
        foreach (var personalitySection in configSection.GetChildren())
        {
            try
            {
                var personalityName = personalitySection.Key.ToLowerInvariant();
                var config = LoadPersonalityConfigurationFromSection(personalitySection);

                configurations[personalityName] = config;
                _logger.LogDebug("Loaded configuration for personality {PersonalityName} from settings", personalityName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration for personality {PersonalityName} from settings", personalitySection.Key);
            }
        }
    }

    private PersonalityConfiguration LoadPersonalityConfigurationFromSection(IConfigurationSection section)
    {
        return new PersonalityConfiguration
        {
            ExpertiseLevels = LoadExpertiseLevelsFromSection(section.GetSection("ExpertiseLevels")),
            BehaviorRules = LoadBehaviorRulesFromSection(section.GetSection("BehaviorRules")),
            StressRules = LoadStressRulesFromSection(section.GetSection("StressRules")),
            CommunicationRules = LoadCommunicationRulesFromSection(section.GetSection("CommunicationRules"))
        };
    }

    private Dictionary<DomainType, double> LoadExpertiseLevelsFromSection(IConfigurationSection section)
    {
        var expertiseLevels = new Dictionary<DomainType, double>();

        foreach (var item in section.GetChildren())
        {
            if (Enum.TryParse<DomainType>(item.Key, true, out var domainType) &&
                double.TryParse(item.Value, out var level))
            {
                expertiseLevels[domainType] = Math.Clamp(level, 0.0, 1.0);
            }
        }

        return expertiseLevels;
    }

    private PersonalityBehaviorRules LoadBehaviorRulesFromSection(IConfigurationSection section)
    {
        return new PersonalityBehaviorRules
        {
            TechnicalContextBoost = section.GetValue<double>("TechnicalContextBoost", PersonalityConstants.TechnicalContextTraitBoost),
            PersonalContextBoost = section.GetValue<double>("PersonalContextBoost", PersonalityConstants.PersonalContextTraitBoost),
            FamilyContextBoost = section.GetValue<double>("FamilyContextBoost", PersonalityConstants.FamilyContextTraitBoost),
            ProfessionalContextBoost = section.GetValue<double>("ProfessionalContextBoost", PersonalityConstants.ProfessionalContextInterpersonalBoost),
            CoreDomainConfidenceBonus = section.GetValue<double>("CoreDomainConfidenceBonus", PersonalityConstants.IvanCoreDomainConfidenceBonus),
            WeaknessReduction = section.GetValue<double>("WeaknessReduction", PersonalityConstants.IvanKnownWeaknessReduction)
        };
    }

    private StressModificationRules LoadStressRulesFromSection(IConfigurationSection section)
    {
        return new StressModificationRules
        {
            DirectnessFactor = section.GetValue<double>("DirectnessFactor", PersonalityConstants.IvanStressDirectnessFactor),
            StructuredThinkingBoost = section.GetValue<double>("StructuredThinkingBoost", PersonalityConstants.IvanStressStructuredThinkingBoost),
            TechnicalDetailReduction = section.GetValue<double>("TechnicalDetailReduction", PersonalityConstants.IvanTimePressureDetailReduction),
            WarmthReduction = section.GetValue<double>("WarmthReduction", PersonalityConstants.IvanStressWarmthReduction),
            SolutionFocusBoost = section.GetValue<double>("SolutionFocusBoost", PersonalityConstants.IvanTimePressureSolutionFocusBoost),
            SelfReflectionReduction = section.GetValue<double>("SelfReflectionReduction", PersonalityConstants.IvanStressSelfReflectionReduction)
        };
    }

    private CommunicationStyleRules LoadCommunicationRulesFromSection(IConfigurationSection section)
    {
        return new CommunicationStyleRules
        {
            HighUrgencyThreshold = section.GetValue<double>("HighUrgencyThreshold", PersonalityConstants.HighUrgencyThreshold),
            HighStressThreshold = section.GetValue<double>("HighStressThreshold", PersonalityConstants.HighStressCommunicationThreshold),
            ComplexityFormalityThreshold = section.GetValue<int>("ComplexityFormalityThreshold", PersonalityConstants.HighComplexityFormalityThreshold),
            EnergyReductionLateHours = section.GetValue<double>("EnergyReductionLateHours", PersonalityConstants.LateHoursEnergyReduction),
            EnergyBoostMorningHours = section.GetValue<double>("EnergyBoostMorningHours", PersonalityConstants.MorningHoursEnergyBoost)
        };
    }

    private PersonalityConfiguration CreateIvanConfiguration()
    {
        return new PersonalityConfiguration
        {
            ExpertiseLevels = CreateIvanExpertiseLevels(),
            BehaviorRules = CreateIvanBehaviorRules(),
            StressRules = CreateIvanStressRules(),
            CommunicationRules = CreateIvanCommunicationRules()
        };
    }

    private Dictionary<DomainType, double> CreateIvanExpertiseLevels()
    {
        return new Dictionary<DomainType, double>
        {
            { DomainType.CSharpDotNet, 0.95 },
            { DomainType.SoftwareArchitecture, 0.85 },
            { DomainType.DatabaseDesign, 0.80 },
            { DomainType.GameDevelopment, 0.90 }, // Unity expertise
            { DomainType.TeamLeadership, 0.75 },
            { DomainType.RnDManagement, 0.80 },
            { DomainType.BusinessStrategy, 0.60 },
            { DomainType.PersonalRelations, 0.45 }, // Self-acknowledged weakness
            { DomainType.WorkLifeBalance, 0.30 }, // Known challenge area
            { DomainType.Politics, 0.50 },
            { DomainType.Finance, 0.65 },
            { DomainType.Education, 0.40 }
        };
    }

    private PersonalityBehaviorRules CreateIvanBehaviorRules()
    {
        return new PersonalityBehaviorRules
        {
            TechnicalContextBoost = PersonalityConstants.TechnicalContextTraitBoost,
            PersonalContextBoost = PersonalityConstants.PersonalContextTraitBoost,
            FamilyContextBoost = PersonalityConstants.FamilyContextTraitBoost,
            ProfessionalContextBoost = PersonalityConstants.ProfessionalContextInterpersonalBoost,
            CoreDomainConfidenceBonus = PersonalityConstants.IvanCoreDomainConfidenceBonus,
            WeaknessReduction = PersonalityConstants.IvanKnownWeaknessReduction
        };
    }

    private StressModificationRules CreateIvanStressRules()
    {
        return new StressModificationRules
        {
            DirectnessFactor = PersonalityConstants.IvanStressDirectnessFactor,
            StructuredThinkingBoost = PersonalityConstants.IvanStressStructuredThinkingBoost,
            TechnicalDetailReduction = PersonalityConstants.IvanTimePressureDetailReduction,
            WarmthReduction = PersonalityConstants.IvanStressWarmthReduction,
            SolutionFocusBoost = PersonalityConstants.IvanTimePressureSolutionFocusBoost,
            SelfReflectionReduction = PersonalityConstants.IvanStressSelfReflectionReduction
        };
    }

    private CommunicationStyleRules CreateIvanCommunicationRules()
    {
        return new CommunicationStyleRules
        {
            HighUrgencyThreshold = PersonalityConstants.HighUrgencyThreshold,
            HighStressThreshold = PersonalityConstants.HighStressCommunicationThreshold,
            ComplexityFormalityThreshold = PersonalityConstants.HighComplexityFormalityThreshold,
            EnergyReductionLateHours = PersonalityConstants.LateHoursEnergyReduction,
            EnergyBoostMorningHours = PersonalityConstants.MorningHoursEnergyBoost
        };
    }

    private Dictionary<DomainType, double> GetGenericExpertiseLevels()
    {
        return Enum.GetValues<DomainType>()
            .ToDictionary(domain => domain, _ => PersonalityConstants.GenericBaseConfidence);
    }

    private PersonalityBehaviorRules GetGenericBehaviorRules()
    {
        return new PersonalityBehaviorRules
        {
            TechnicalContextBoost = 1.2,
            PersonalContextBoost = 1.1,
            FamilyContextBoost = 1.1,
            ProfessionalContextBoost = 1.1,
            CoreDomainConfidenceBonus = 0.05,
            WeaknessReduction = 0.1
        };
    }

    private StressModificationRules GetGenericStressRules()
    {
        return new StressModificationRules
        {
            DirectnessFactor = PersonalityConstants.GenericStressDirectnessFactor,
            StructuredThinkingBoost = 0.1,
            TechnicalDetailReduction = PersonalityConstants.GenericTimePressureDetailReduction,
            WarmthReduction = PersonalityConstants.GenericStressWarmthReduction,
            SolutionFocusBoost = 0.2,
            SelfReflectionReduction = 0.1
        };
    }

    private CommunicationStyleRules GetGenericCommunicationRules()
    {
        return new CommunicationStyleRules
        {
            HighUrgencyThreshold = 0.8,
            HighStressThreshold = 0.7,
            ComplexityFormalityThreshold = 8,
            EnergyReductionLateHours = 0.9,
            EnergyBoostMorningHours = 1.1
        };
    }

    #endregion
}

#region Configuration Data Classes

/// <summary>
/// Полная конфигурация персоналии.
/// </summary>
public class PersonalityConfiguration
{
    public Dictionary<DomainType, double> ExpertiseLevels { get; set; } = new();
    public PersonalityBehaviorRules BehaviorRules { get; set; } = new();
    public StressModificationRules StressRules { get; set; } = new();
    public CommunicationStyleRules CommunicationRules { get; set; } = new();
}

/// <summary>
/// Правила поведенческих модификаций.
/// </summary>
public class PersonalityBehaviorRules
{
    public double TechnicalContextBoost { get; set; }
    public double PersonalContextBoost { get; set; }
    public double FamilyContextBoost { get; set; }
    public double ProfessionalContextBoost { get; set; }
    public double CoreDomainConfidenceBonus { get; set; }
    public double WeaknessReduction { get; set; }
}

/// <summary>
/// Правила стрессовых модификаций поведения.
/// </summary>
public class StressModificationRules
{
    public double DirectnessFactor { get; set; }
    public double StructuredThinkingBoost { get; set; }
    public double TechnicalDetailReduction { get; set; }
    public double WarmthReduction { get; set; }
    public double SolutionFocusBoost { get; set; }
    public double SelfReflectionReduction { get; set; }
}

/// <summary>
/// Правила коммуникационного стиля.
/// </summary>
public class CommunicationStyleRules
{
    public double HighUrgencyThreshold { get; set; }
    public double HighStressThreshold { get; set; }
    public int ComplexityFormalityThreshold { get; set; }
    public double EnergyReductionLateHours { get; set; }
    public double EnergyBoostMorningHours { get; set; }
}

// DomainType используется из DigitalMe.Services (ContextualPersonalityEngine.cs)

#endregion