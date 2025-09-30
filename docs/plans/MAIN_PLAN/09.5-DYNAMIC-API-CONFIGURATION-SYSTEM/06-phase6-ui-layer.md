# 📋 PHASE 6: UI LAYER (Blazor Components)

**Parent Plan**: [09.5-DYNAMIC-API-CONFIGURATION-SYSTEM.md](../09.5-DYNAMIC-API-CONFIGURATION-SYSTEM.md)

**Phase Status**: PENDING
**Priority**: HIGH
**Estimated Duration**: 3-4 days
**Dependencies**: Phase 5 Complete

---

## Phase Objectives

Create intuitive Blazor components for managing API configurations, viewing usage statistics, and monitoring quotas. Ensure secure UI with proper masking and validation.

---

## Task 6.1: Create Settings Page Component

**Status**: PENDING
**Priority**: HIGH
**Estimated**: 120 minutes
**Dependencies**: Phase 5 complete

### TDD Cycle

#### 1. RED: Create component tests
File: `tests/DigitalMe.Tests.Unit/Components/ApiConfigurationComponentTests.cs`

```csharp
public class ApiConfigurationComponentTests : TestContext
{
    [Fact]
    public void Component_Should_Display_Provider_List()
    {
        // Arrange
        var component = RenderComponent<ApiConfiguration>();

        // Assert
        component.Find(".provider-card").Should().NotBeNull();
        component.FindAll(".provider-card").Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Should_Mask_API_Keys_By_Default()
    {
        // Arrange
        var component = RenderComponent<ApiConfiguration>();
        var input = component.Find("input[type='password']");

        // Assert
        input.Should().NotBeNull();
        input.GetAttribute("type").Should().Be("password");
    }

    [Fact]
    public async Task Should_Test_Connection_On_Button_Click()
    {
        // Arrange
        var mockService = Services.GetRequiredService<Mock<IApiConfigurationService>>();
        mockService.Setup(s => s.TestConnectionAsync("Anthropic", It.IsAny<string>()))
            .ReturnsAsync(true);

        var component = RenderComponent<ApiConfiguration>();

        // Act
        var testButton = component.Find(".btn-test");
        await testButton.ClickAsync();

        // Assert
        component.Find(".alert-success").Should().NotBeNull();
    }
}
```

#### 2. GREEN: Create Blazor components

File: `src/DigitalMe.Web/Pages/Settings/ApiConfiguration.razor`

```razor
@page "/settings/api-configuration"
@inject IApiConfigurationService ConfigService
@inject IApiKeyValidator Validator
@inject IApiUsageTracker UsageTracker
@inject IJSRuntime JS
@inject IToastService Toast

<PageTitle>API Configuration</PageTitle>

<div class="container-fluid">
    <div class="row mb-4">
        <div class="col">
            <h3>
                <i class="bi bi-key-fill me-2"></i>API Configuration
            </h3>
            <p class="text-muted">Manage your API keys and monitor usage</p>
        </div>
    </div>

    <div class="row">
        @foreach (var provider in Providers)
        {
            <div class="col-md-6 col-lg-4 mb-3">
                <ProviderCard Provider="@provider"
                            Configuration="@GetConfiguration(provider)"
                            OnSave="@(key => SaveApiKey(provider, key))"
                            OnTest="@(key => TestConnection(provider, key))"
                            OnRemove="@(() => RemoveApiKey(provider))" />
            </div>
        }
    </div>

    <div class="row mt-4">
        <div class="col">
            <UsageDisplay UserId="@CurrentUserId" />
        </div>
    </div>
</div>

@code {
    private List<string> Providers = new() { "Anthropic", "OpenAI", "Slack", "GitHub" };
    private Dictionary<string, ApiConfiguration> _configurations = new();
    private string CurrentUserId => AuthState?.User?.Identity?.Name ?? "anonymous";

    [CascadingParameter]
    private Task<AuthenticationState>? AuthState { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadConfigurations();
    }

    private async Task LoadConfigurations()
    {
        foreach (var provider in Providers)
        {
            var config = await ConfigService.GetUserConfigurationAsync(CurrentUserId, provider);
            if (config != null)
            {
                _configurations[provider] = config;
            }
        }
    }

    private ApiConfiguration? GetConfiguration(string provider)
    {
        return _configurations.GetValueOrDefault(provider);
    }

    private async Task SaveApiKey(string provider, string apiKey)
    {
        try
        {
            await ConfigService.SetUserApiKeyAsync(CurrentUserId, provider, apiKey);
            await LoadConfigurations(); // Reload to get updated config
            Toast.ShowSuccess($"{provider} API key saved successfully");
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Failed to save API key: {ex.Message}");
        }
    }

    private async Task<bool> TestConnection(string provider, string apiKey)
    {
        try
        {
            var result = await ConfigService.TestConnectionAsync(provider, apiKey);
            if (result)
            {
                Toast.ShowSuccess($"{provider} connection successful!");
            }
            else
            {
                Toast.ShowWarning($"{provider} connection failed");
            }
            return result;
        }
        catch (Exception ex)
        {
            Toast.ShowError($"Test failed: {ex.Message}");
            return false;
        }
    }

    private async Task RemoveApiKey(string provider)
    {
        var confirmed = await JS.InvokeAsync<bool>("confirm",
            $"Are you sure you want to remove your {provider} API key?");

        if (confirmed)
        {
            await ConfigService.DeactivateConfigurationAsync(CurrentUserId, provider);
            _configurations.Remove(provider);
            Toast.ShowInfo($"{provider} API key removed");
        }
    }
}
```

File: `src/DigitalMe.Web/Components/ProviderCard.razor`

```razor
@inject IJSRuntime JS

<div class="card provider-card @(IsConfigured ? "border-success" : "")">
    <div class="card-header">
        <div class="d-flex justify-content-between align-items-center">
            <h5 class="mb-0">
                <i class="@GetProviderIcon()"></i> @Provider
            </h5>
            @if (IsConfigured)
            {
                <span class="badge bg-success">Configured</span>
            }
        </div>
    </div>

    <div class="card-body">
        @if (!_isEditing && IsConfigured)
        {
            <!-- Display mode -->
            <div class="mb-3">
                <label class="form-label small text-muted">Current Key</label>
                <div class="input-group">
                    <input type="text"
                           class="form-control"
                           value="@MaskedKey"
                           readonly />
                    <button class="btn btn-outline-secondary"
                            @onclick="() => _isEditing = true">
                        <i class="bi bi-pencil"></i>
                    </button>
                </div>
            </div>

            <div class="mb-3">
                <small class="text-muted">
                    Last validated: @(Configuration?.LastValidatedAt?.ToString("g") ?? "Never")
                </small>
            </div>

            <div class="btn-group w-100">
                <button class="btn btn-outline-primary" @onclick="TestCurrentKey">
                    Test Connection
                </button>
                <button class="btn btn-outline-danger" @onclick="RemoveKey">
                    Remove Key
                </button>
            </div>
        }
        else
        {
            <!-- Edit mode -->
            <div class="mb-3">
                <label class="form-label">API Key</label>
                <div class="input-group">
                    <input type="@(_showKey ? "text" : "password")"
                           class="form-control @(_keyError ? "is-invalid" : "")"
                           @bind="_apiKey"
                           @bind:event="oninput"
                           placeholder="@GetPlaceholder()" />
                    <button class="btn btn-outline-secondary"
                            @onclick="ToggleKeyVisibility">
                        <i class="bi bi-eye@(_showKey ? "-slash" : "")"></i>
                    </button>
                </div>
                @if (_keyError)
                {
                    <div class="invalid-feedback d-block">
                        @_keyErrorMessage
                    </div>
                }
                <small class="form-text text-muted">
                    @GetKeyFormatHint()
                </small>
            </div>

            <div class="btn-group w-100">
                <button class="btn btn-primary"
                        @onclick="SaveKey"
                        disabled="@(!IsValidKey())">
                    <i class="bi bi-save"></i> Save
                </button>
                <button class="btn btn-secondary"
                        @onclick="TestKey"
                        disabled="@(!IsValidKey())">
                    <i class="bi bi-link-45deg"></i> Test
                </button>
                @if (IsConfigured)
                {
                    <button class="btn btn-outline-secondary"
                            @onclick="CancelEdit">
                        Cancel
                    </button>
                }
            </div>
        }

        @if (_testResult != null)
        {
            <div class="alert @(_testResult.Success ? "alert-success" : "alert-danger") mt-3">
                <i class="bi @(_testResult.Success ? "bi-check-circle" : "bi-x-circle")"></i>
                @_testResult.Message
            </div>
        }
    </div>
</div>

@code {
    [Parameter] public string Provider { get; set; } = "";
    [Parameter] public ApiConfiguration? Configuration { get; set; }
    [Parameter] public EventCallback<string> OnSave { get; set; }
    [Parameter] public EventCallback<string> OnTest { get; set; }
    [Parameter] public EventCallback OnRemove { get; set; }

    private string _apiKey = "";
    private bool _showKey = false;
    private bool _isEditing = false;
    private bool _keyError = false;
    private string _keyErrorMessage = "";
    private TestResult? _testResult;

    private bool IsConfigured => Configuration != null;
    private string MaskedKey => Configuration != null
        ? $"****{Configuration.KeyFingerprint}"
        : "";

    private void ToggleKeyVisibility() => _showKey = !_showKey;

    private void CancelEdit()
    {
        _isEditing = false;
        _apiKey = "";
        _testResult = null;
    }

    private async Task SaveKey()
    {
        if (IsValidKey())
        {
            await OnSave.InvokeAsync(_apiKey);
            _apiKey = "";
            _isEditing = false;
            _testResult = null;
        }
    }

    private async Task TestKey()
    {
        var success = await OnTest.InvokeAsync(_apiKey);
        _testResult = new TestResult
        {
            Success = success,
            Message = success
                ? "Connection successful!"
                : "Connection failed. Please check your API key."
        };
    }

    private async Task TestCurrentKey()
    {
        if (Configuration != null)
        {
            // Test with existing configured key
            _testResult = new TestResult { Message = "Testing..." };
            var success = await OnTest.InvokeAsync("");
            _testResult = new TestResult
            {
                Success = success,
                Message = success
                    ? "Connection verified!"
                    : "Connection failed. Key may be invalid or expired."
            };
        }
    }

    private async Task RemoveKey()
    {
        await OnRemove.InvokeAsync();
        _isEditing = false;
    }

    private bool IsValidKey()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return false;

        var isValid = Provider switch
        {
            "Anthropic" => _apiKey.StartsWith("sk-ant-"),
            "OpenAI" => _apiKey.StartsWith("sk-"),
            _ => _apiKey.Length > 10
        };

        _keyError = !isValid && _apiKey.Length > 0;
        _keyErrorMessage = isValid ? "" : $"Invalid {Provider} key format";

        return isValid;
    }

    private string GetPlaceholder() => Provider switch
    {
        "Anthropic" => "sk-ant-...",
        "OpenAI" => "sk-...",
        _ => "Enter API key"
    };

    private string GetKeyFormatHint() => Provider switch
    {
        "Anthropic" => "Anthropic keys start with 'sk-ant-'",
        "OpenAI" => "OpenAI keys start with 'sk-'",
        _ => "Enter your API key for this service"
    };

    private string GetProviderIcon() => Provider switch
    {
        "Anthropic" => "bi bi-robot",
        "OpenAI" => "bi bi-cpu",
        "Slack" => "bi bi-slack",
        "GitHub" => "bi bi-github",
        _ => "bi bi-key"
    };

    private class TestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}
```

### Acceptance Criteria
- ✅ All providers displayed
- ✅ Key masking working
- ✅ Test connection functional
- ✅ Save functionality working
- ✅ Responsive design verified

---

## Task 6.2: Create Usage Display Component

**Status**: PENDING
**Priority**: MEDIUM
**Estimated**: 60 minutes
**Dependencies**: Task 6.1

### Usage Display Component

File: `src/DigitalMe.Web/Components/UsageDisplay.razor`

```razor
@inject IApiUsageTracker UsageTracker
@inject IQuotaManager QuotaManager
@implements IDisposable

<div class="card">
    <div class="card-header">
        <h5>
            <i class="bi bi-graph-up me-2"></i>API Usage Statistics
        </h5>
    </div>
    <div class="card-body">
        @if (_isLoading)
        {
            <div class="text-center">
                <div class="spinner-border" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        }
        else if (_usageStats != null)
        {
            <div class="row mb-3">
                <div class="col-md-3">
                    <div class="text-center">
                        <h6>Total Requests</h6>
                        <h4>@_usageStats.RequestCount</h4>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="text-center">
                        <h6>Total Tokens</h6>
                        <h4>@_usageStats.TotalTokens.ToString("N0")</h4>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="text-center">
                        <h6>Total Cost</h6>
                        <h4>$@_usageStats.TotalCost.ToString("F2")</h4>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="text-center">
                        <h6>Success Rate</h6>
                        <h4>@_usageStats.SuccessRate.ToString("F1")%</h4>
                    </div>
                </div>
            </div>

            @foreach (var provider in _quotaStatuses)
            {
                <div class="mb-3">
                    <label>@provider.Key Daily Quota</label>
                    <div class="progress">
                        <div class="progress-bar @GetProgressBarClass(provider.Value.PercentUsed)"
                             style="width: @(provider.Value.PercentUsed)%"
                             role="progressbar">
                            @provider.Value.Used.ToString("N0") / @provider.Value.DailyLimit.ToString("N0") tokens
                        </div>
                    </div>
                    <small class="text-muted">
                        Resets at @provider.Value.ResetsAt.ToString("t")
                    </small>
                </div>
            }
        }
    </div>
</div>

@code {
    [Parameter] public string UserId { get; set; } = "";

    private UsageStats? _usageStats;
    private Dictionary<string, QuotaStatus> _quotaStatuses = new();
    private bool _isLoading = true;
    private Timer? _refreshTimer;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
        // Refresh every 30 seconds
        _refreshTimer = new Timer(async _ => await LoadData(), null,
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private async Task LoadData()
    {
        _isLoading = true;

        _usageStats = await UsageTracker.GetUsageStatsAsync(
            UserId, DateTime.Today, DateTime.Now);

        foreach (var provider in new[] { "Anthropic", "OpenAI" })
        {
            var quota = await QuotaManager.GetQuotaStatusAsync(UserId, provider);
            if (quota != null)
            {
                _quotaStatuses[provider] = quota;
            }
        }

        _isLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private string GetProgressBarClass(decimal percentUsed) => percentUsed switch
    {
        >= 90 => "bg-danger",
        >= 80 => "bg-warning",
        _ => "bg-success"
    };

    public void Dispose()
    {
        _refreshTimer?.Dispose();
    }
}
```

### Acceptance Criteria
- ✅ Usage statistics displayed
- ✅ Quota progress shown
- ✅ Cost estimates visible
- ✅ Real-time updates working
- ✅ Responsive layout

---

## Phase Completion Checklist

- [ ] Settings page created
- [ ] Provider cards functional
- [ ] Key masking secure
- [ ] Connection testing works
- [ ] Usage display accurate
- [ ] Quota visualization clear
- [ ] Responsive design verified
- [ ] Accessibility tested
- [ ] Toast notifications working
- [ ] 80%+ test coverage

---

## Output Artifacts

1. **Pages**: `ApiConfiguration.razor`
2. **Components**: `ProviderCard.razor`, `UsageDisplay.razor`
3. **Styles**: `api-configuration.css`
4. **Scripts**: `api-configuration.js` (if needed)
5. **Tests**: Component test suite

---

## Next Phase Dependencies

Phase 7 (Performance & Security) depends on:
- UI components complete
- All features functional
- Integration tested