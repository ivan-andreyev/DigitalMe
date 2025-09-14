namespace DigitalMe.Services;

/// <summary>
/// Константы для настройки поведенческих модификаторов личности.
/// Все magic numbers вынесены в именованные константы для лучшей читаемости и сопровождения.
/// </summary>
public static class PersonalityConstants
{
    #region Stress Behavior Modifiers

    /// <summary>
    /// Коэффициент увеличения прямолинейности Ивана под стрессом.
    /// Ivan becomes more direct when stressed.
    /// </summary>
    public const double IvanStressDirectnessFactor = 0.3;

    /// <summary>
    /// Коэффициент усиления структурированного мышления Ивана под стрессом.
    /// Ivan becomes more structured when stressed.
    /// </summary>
    public const double IvanStressStructuredThinkingBoost = 0.2;

    /// <summary>
    /// Коэффициент снижения детализации Ивана при временном давлении.
    /// Ivan provides less technical detail when time-pressed.
    /// </summary>
    public const double IvanTimePressureDetailReduction = 0.4;

    /// <summary>
    /// Коэффициент снижения теплоты общения при стрессе/спешке.
    /// Ivan becomes less warm when stressed or hurried.
    /// </summary>
    public const double IvanStressWarmthReduction = 0.2;

    /// <summary>
    /// Коэффициент усиления фокуса на решениях при временном давлении.
    /// Ivan becomes more solution-focused under time pressure.
    /// </summary>
    public const double IvanTimePressureSolutionFocusBoost = 0.3;

    /// <summary>
    /// Коэффициент снижения самоанализа Ивана под стрессом.
    /// Ivan engages in less self-reflection when stressed.
    /// </summary>
    public const double IvanStressSelfReflectionReduction = 0.25;

    /// <summary>
    /// Базовые коэффициенты стрессовых модификаций для общего случая.
    /// Generic stress behavior modification factors.
    /// </summary>
    public const double GenericStressDirectnessFactor = 0.2;
    public const double GenericTimePressureDetailReduction = 0.3;
    public const double GenericStressWarmthReduction = 0.15;

    #endregion

    #region Expertise and Confidence

    /// <summary>
    /// Пороговый уровень экспертизы для считания домена экспертным.
    /// Domain expertise threshold for expert-level confidence boost.
    /// </summary>
    public const double ExpertLevelThreshold = 0.85;

    /// <summary>
    /// Пороговый уровень для считания домена слабой стороной.
    /// Domain expertise threshold for weakness adjustment.
    /// </summary>
    public const double WeaknessLevelThreshold = 0.40;

    /// <summary>
    /// Скорость снижения уверенности с ростом сложности задачи.
    /// Rate at which confidence decreases as task complexity increases.
    /// </summary>
    public const double ComplexityConfidenceReductionRate = 0.08;

    /// <summary>
    /// Базовый уровень уверенности для общего случая.
    /// Base confidence level for generic personality.
    /// </summary>
    public const double GenericBaseConfidence = 0.6;

    /// <summary>
    /// Снижение скорости падения уверенности для общего случая.
    /// Generic complexity adjustment rate.
    /// </summary>
    public const double GenericComplexityReductionRate = 0.05;

    /// <summary>
    /// Бонус уверенности для ключевых доменов экспертизы Ивана.
    /// Confidence bonus for Ivan's core expertise domains.
    /// </summary>
    public const double IvanCoreDomainConfidenceBonus = 0.1;

    /// <summary>
    /// Снижение уверенности для признанных слабых сторон Ивана.
    /// Confidence reduction for Ivan's acknowledged weaknesses.
    /// </summary>
    public const double IvanKnownWeaknessReduction = 0.2;

    /// <summary>
    /// Максимальный уровень уверенности для слабых доменов.
    /// Maximum confidence cap for weakness domains.
    /// </summary>
    public const double WeaknessDomainConfidenceCap = 0.6;

    /// <summary>
    /// Минимальный уровень уверенности (нижняя граница).
    /// Minimum confidence floor to prevent zero confidence.
    /// </summary>
    public const double MinimumConfidenceLevel = 0.1;

    /// <summary>
    /// Максимальный уровень уверенности (верхняя граница).
    /// Maximum confidence ceiling.
    /// </summary>
    public const double MaximumConfidenceLevel = 1.0;

    #endregion

    #region Contextual Adaptation

    /// <summary>
    /// Коэффициент усиления технических черт в техническом контексте.
    /// Technical traits boost factor in technical contexts.
    /// </summary>
    public const double TechnicalContextTraitBoost = 1.4;

    /// <summary>
    /// Коэффициент усиления технических предпочтений в техническом окружении.
    /// Tech preferences boost factor in technical environment.
    /// </summary>
    public const double TechnicalEnvironmentPreferencesBoost = 1.3;

    /// <summary>
    /// Коэффициент усиления черт работы с людьми в профессиональном контексте.
    /// Professional interpersonal traits boost in professional contexts.
    /// </summary>
    public const double ProfessionalContextInterpersonalBoost = 1.2;

    /// <summary>
    /// Коэффициент усиления личных черт в личном контексте.
    /// Personal traits boost factor in personal contexts.
    /// </summary>
    public const double PersonalContextTraitBoost = 1.3;

    /// <summary>
    /// Коэффициент усиления семейных черт в семейном контексте.
    /// Family traits boost factor in family contexts.
    /// </summary>
    public const double FamilyContextTraitBoost = 1.5;

    #endregion

    #region Ivan-Specific Context Modifiers

    /// <summary>
    /// Коэффициент усиления проблем work-life balance при обсуждении семьи.
    /// Challenge traits boost when discussing family/time topics.
    /// </summary>
    public const double IvanFamilyChallengeBoost = 1.5;

    /// <summary>
    /// Коэффициент усиления приоритетов жизни при личных темах.
    /// Life priorities boost in personal contexts.
    /// </summary>
    public const double IvanLifePrioritiesBoost = 1.3;

    /// <summary>
    /// Коэффициент усиления самооценки в технических лидерских контекстах.
    /// Self-assessment boost in technical leadership contexts.
    /// </summary>
    public const double IvanTechnicalLeadershipSelfAssessmentBoost = 1.2;

    /// <summary>
    /// Коэффициент усиления позиционных черт в профессиональном техническом контексте.
    /// Position traits boost in professional technical contexts.
    /// </summary>
    public const double IvanTechnicalProfessionalPositionBoost = 1.3;

    /// <summary>
    /// Коэффициент усиления навыков принятия решений в стратегических контекстах.
    /// Decision making boost in strategic contexts.
    /// </summary>
    public const double IvanStrategicDecisionMakingBoost = 1.4;

    /// <summary>
    /// Коэффициент усиления целей в стратегических/R&D контекстах.
    /// Goals boost in strategic/R&D contexts.
    /// </summary>
    public const double IvanStrategicGoalsBoost = 1.2;

    #endregion

    #region Communication Style Thresholds

    /// <summary>
    /// Пороговый уровень срочности для переключения в более прямой стиль.
    /// Urgency threshold for switching to more direct communication style.
    /// </summary>
    public const double HighUrgencyThreshold = 0.7;

    /// <summary>
    /// Пороговый уровень стресса для модификации стиля общения.
    /// Stress threshold for communication style modifications.
    /// </summary>
    public const double HighStressCommunicationThreshold = 0.6;

    /// <summary>
    /// Пороговый уровень сложности для увеличения формальности.
    /// Complexity threshold for increasing formality in communication.
    /// </summary>
    public const int HighComplexityFormalityThreshold = 7;

    #endregion

    #region Time-Based Modifiers

    /// <summary>
    /// Коэффициент снижения энергии в поздние часы (для контекстной адаптации).
    /// Energy reduction factor for late hours contextual adaptation.
    /// </summary>
    public const double LateHoursEnergyReduction = 0.8;

    /// <summary>
    /// Коэффициент повышения энергии в утренние часы.
    /// Energy boost factor for morning hours.
    /// </summary>
    public const double MorningHoursEnergyBoost = 1.2;

    /// <summary>
    /// Граница для считания времени "поздним" (час в 24-часовом формате).
    /// Hour boundary for considering time as "late hours".
    /// </summary>
    public const int LateHoursThreshold = 22;

    /// <summary>
    /// Граница для считания времени "утренним" (час в 24-часовом формате).
    /// Hour boundary for considering time as "morning hours".
    /// </summary>
    public const int MorningHoursThreshold = 6;

    #endregion

    #region Validation Constants

    /// <summary>
    /// Минимальное значение для уровня стресса.
    /// Minimum stress level value.
    /// </summary>
    public const double MinimumStressLevel = 0.0;

    /// <summary>
    /// Максимальное значение для уровня стресса.
    /// Maximum stress level value.
    /// </summary>
    public const double MaximumStressLevel = 1.0;

    /// <summary>
    /// Минимальное значение сложности задачи.
    /// Minimum task complexity value.
    /// </summary>
    public const int MinimumTaskComplexity = 1;

    /// <summary>
    /// Максимальное значение сложности задачи.
    /// Maximum task complexity value.
    /// </summary>
    public const int MaximumTaskComplexity = 10;

    #endregion

    #region Ivan-Specific Behavior Constants

    /// <summary>
    /// Коэффициент увеличения прагматизма Ивана при временном давлении.
    /// Ivan's pragmatism increase factor under time pressure.
    /// </summary>
    public const double IvanPragmatismIncreaseFactor = 0.25;

    /// <summary>
    /// Коэффициент увеличения уверенности Ивана под стрессом (лидерский режим).
    /// Ivan's confidence boost factor under stress (leadership mode).
    /// </summary>
    public const double IvanStressConfidenceBoostFactor = 0.1;

    /// <summary>
    /// Коэффициент увеличения результат-ориентированности Ивана.
    /// Ivan's results orientation increase factor.
    /// </summary>
    public const double IvanResultsOrientationFactor = 0.2;

    /// <summary>
    /// Коэффициенты усиления трейтов для различных контекстов.
    /// Trait boost factors for different contexts.
    /// </summary>
    public const double IvanTechnicalTraitBoost = 0.9;
    public const double IvanPersonalTraitBoost = 0.8;
    public const double IvanProfessionalTraitBoost = 0.9;
    public const double IvanFamilyTraitBoost = 0.8;

    /// <summary>
    /// Границы для модификации весов трейтов.
    /// Bounds for trait weight modifications.
    /// </summary>
    public const double MaxTraitWeightBoost = 2.0;
    public const double MinTraitWeight = 0.1;
    public const double MaxTraitWeight = 1.0;

    /// <summary>
    /// Базовые уровни формальности для разных типов контекста.
    /// Base formality levels for different context types.
    /// </summary>
    public const double TechnicalContextFormality = 0.4;
    public const double PersonalContextFormality = 0.2;
    public const double ProfessionalContextFormality = 0.65;
    public const double FamilyContextFormality = 0.1;
    public const double DefaultContextFormality = 0.5;

    /// <summary>
    /// Коэффициент увеличения формальности при высокой срочности.
    /// Formality increase factor for high urgency situations.
    /// </summary>
    public const double UrgencyFormalityBoost = 0.2;

    #endregion

    #region Generic Stress Behavior Constants

    /// <summary>
    /// Коэффициент усиления структурированного мышления для общего случая стресса.
    /// Generic structured thinking boost under stress.
    /// </summary>
    public const double GenericStructuredThinkingBoostFactor = 0.1;

    /// <summary>
    /// Коэффициент усиления фокуса на решениях для общего случая временного давления.
    /// Generic solution focus boost under time pressure.
    /// </summary>
    public const double GenericSolutionFocusBoostFactor = 0.15;

    /// <summary>
    /// Коэффициент снижения самоанализа для общего случая стресса.
    /// Generic self-reflection reduction under stress.
    /// </summary>
    public const double GenericSelfReflectionReductionFactor = 0.15;

    /// <summary>
    /// Коэффициент снижения уверенности для общего случая стресса.
    /// Generic confidence reduction under stress.
    /// </summary>
    public const double GenericConfidenceReductionFactor = 0.05;

    /// <summary>
    /// Коэффициент увеличения прагматизма для общего случая временного давления.
    /// Generic pragmatism increase under time pressure.
    /// </summary>
    public const double GenericPragmatismIncreaseFactor = 0.2;

    /// <summary>
    /// Коэффициент увеличения результат-ориентированности для общего случая временного давления.
    /// Generic results orientation increase under time pressure.
    /// </summary>
    public const double GenericResultsOrientationIncreaseFactor = 0.15;

    /// <summary>
    /// Максимальные границы для нормализации поведенческих модификаций.
    /// Maximum bounds for behavioral modification normalization.
    /// </summary>
    public const double MaxDirectnessIncrease = 0.5;
    public const double MaxStructuredThinkingBoost = 0.5;
    public const double MaxTechnicalDetailReduction = 0.8;
    public const double MaxWarmthReduction = 0.6;
    public const double MaxSolutionFocusBoost = 0.5;
    public const double MaxSelfReflectionReduction = 0.5;
    public const double MaxConfidenceBoostPositive = 0.3;
    public const double MaxConfidenceBoostNegative = -0.3;
    public const double MaxPragmatismIncrease = 0.5;
    public const double MaxResultsOrientationIncrease = 0.5;

    #endregion
}