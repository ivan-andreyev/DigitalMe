using DigitalMe.Tests.Unit.Controllers;

namespace DigitalMe.Tests.Unit.Controllers;

[CollectionDefinition("ChatController")]
public class ChatControllerCollection : ICollectionFixture<TestWebApplicationFactory<Program>>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
