# Data Access Contracts

**Родительский план**: [../02-05-interfaces.md](../02-05-interfaces.md)

## IPersonalityRepository Interface
**Файл**: `src/DigitalMe.Data/Repositories/IPersonalityRepository.cs:1-25`

```csharp
public interface IPersonalityRepository
{
    /// <summary>
    /// Получить профиль личности по имени
    /// </summary>
    /// <param name="name">Имя профиля</param>
    /// <returns>Профиль с загруженными traits или null</returns>
    Task<PersonalityProfile?> GetByNameAsync(string name);
    
    /// <summary>
    /// Создать новый профиль личности
    /// </summary>
    /// <param name="profile">Профиль для создания</param>
    /// <returns>Созданный профиль с ID</returns>
    Task<PersonalityProfile> CreateAsync(PersonalityProfile profile);
    
    /// <summary>
    /// Обновить черту личности
    /// </summary>
    /// <param name="profileId">ID профиля</param>
    /// <param name="traitName">Название черты</param>
    /// <param name="value">Новое значение</param>
    Task UpdateTraitAsync(Guid profileId, string traitName, object value);
    
    /// <summary>
    /// Получить все профили личности
    /// </summary>
    /// <returns>Список профилей</returns>
    Task<IEnumerable<PersonalityProfile>> GetAllAsync();
    
    /// <summary>
    /// Удалить профиль личности
    /// </summary>
    /// <param name="profileId">ID профиля</param>
    Task DeleteAsync(Guid profileId);
}
```

## IConversationRepository Interface
**Файл**: `src/DigitalMe.Data/Repositories/IConversationRepository.cs:1-20`

```csharp
public interface IConversationRepository
{
    /// <summary>
    /// Создать новый диалог
    /// </summary>
    /// <param name="conversation">Диалог для создания</param>
    /// <returns>Созданный диалог с ID</returns>
    Task<Conversation> CreateAsync(Conversation conversation);
    
    /// <summary>
    /// Получить диалог по ID
    /// </summary>
    /// <param name="conversationId">ID диалога</param>
    /// <returns>Диалог с сообщениями</returns>
    Task<Conversation?> GetByIdAsync(Guid conversationId);
    
    /// <summary>
    /// Добавить сообщение в диалог
    /// </summary>
    /// <param name="message">Сообщение для добавления</param>
    /// <returns>Добавленное сообщение с ID</returns>
    Task<Message> AddMessageAsync(Message message);
    
    /// <summary>
    /// Получить последние сообщения диалога
    /// </summary>
    /// <param name="conversationId">ID диалога</param>
    /// <param name="count">Количество сообщений</param>
    /// <returns>Последние сообщения</returns>
    Task<IEnumerable<Message>> GetRecentMessagesAsync(Guid conversationId, int count = 10);
}
```

## Generic Repository Pattern
**Файл**: `src/DigitalMe.Data/Repositories/IRepository.cs:1-15`

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
```

## Unit of Work Pattern
**Файл**: `src/DigitalMe.Data/UnitOfWork/IUnitOfWork.cs:1-15`

```csharp
public interface IUnitOfWork : IDisposable
{
    IPersonalityRepository Personalities { get; }
    IConversationRepository Conversations { get; }
    
    /// <summary>
    /// Сохранить все изменения в транзакции
    /// </summary>
    /// <returns>Количество измененных записей</returns>
    Task<int> SaveChangesAsync();
    
    /// <summary>
    /// Начать транзакцию
    /// </summary>
    Task BeginTransactionAsync();
    
    /// <summary>
    /// Подтвердить транзакцию
    /// </summary>
    Task CommitTransactionAsync();
    
    /// <summary>
    /// Отменить транзакцию
    /// </summary>
    Task RollbackTransactionAsync();
}
```