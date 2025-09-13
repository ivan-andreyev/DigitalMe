using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Text.Json;

namespace DigitalMe.Services.WebNavigation;

/// <summary>
/// Implementation of web navigation service using Microsoft Playwright
/// Provides Ivan-Level web interaction capabilities with robust error handling
/// Following Clean Architecture patterns with dependency injection
/// </summary>
public class WebNavigationService : IWebNavigationService, IAsyncDisposable, IDisposable
{
    private readonly ILogger<WebNavigationService> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _currentPage;
    private bool _initialized = false;
    private bool _disposed = false;

    public WebNavigationService(ILogger<WebNavigationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WebNavigationResult> InitializeBrowserAsync(BrowserOptions? options = null)
    {
        try
        {
            if (_initialized)
            {
                return WebNavigationResult.SuccessResult(null, "Browser already initialized");
            }

            _logger.LogInformation("Initializing browser with Playwright");

            options ??= new BrowserOptions();

            // Install Playwright browsers if needed
            _playwright = await Playwright.CreateAsync();
            
            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = options.Headless,
                ExecutablePath = options.ExecutablePath
            };

            _browser = await _playwright.Chromium.LaunchAsync(launchOptions);
            
            // Create initial page with viewport
            _currentPage = await _browser.NewPageAsync();
            await _currentPage.SetViewportSizeAsync(options.ViewportWidth, options.ViewportHeight);

            if (!string.IsNullOrEmpty(options.UserAgent))
            {
                await _currentPage.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
                {
                    ["User-Agent"] = options.UserAgent
                });
            }

            _initialized = true;
            _logger.LogInformation("Browser initialized successfully");

            return WebNavigationResult.SuccessResult(new { Headless = options.Headless, Viewport = $"{options.ViewportWidth}x{options.ViewportHeight}" }, 
                "Browser initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize browser");
            return WebNavigationResult.ErrorResult($"Browser initialization failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<WebNavigationResult> NavigateToAsync(string url, string? waitForSelector = null, int timeout = 30000)
    {
        try
        {
            if (!_initialized || _currentPage == null)
            {
                return WebNavigationResult.ErrorResult("Browser not initialized. Call InitializeBrowserAsync first.");
            }

            _logger.LogInformation("Navigating to URL: {Url}", url);

            var response = await _currentPage.GotoAsync(url, new PageGotoOptions 
            { 
                Timeout = timeout,
                WaitUntil = WaitUntilState.NetworkIdle
            });

            if (!string.IsNullOrEmpty(waitForSelector))
            {
                await _currentPage.WaitForSelectorAsync(waitForSelector, new PageWaitForSelectorOptions 
                { 
                    Timeout = timeout 
                });
            }

            var pageInfo = new
            {
                Url = _currentPage.Url,
                Title = await _currentPage.TitleAsync(),
                StatusCode = response?.Status ?? 0,
                LoadedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully navigated to {Url}, title: {Title}", pageInfo.Url, pageInfo.Title);

            return WebNavigationResult.SuccessResult(pageInfo, $"Successfully navigated to {url}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation to {Url} failed", url);
            return WebNavigationResult.ErrorResult($"Navigation failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<WebNavigationResult> ClickElementAsync(string selector, ClickOptions? options = null)
    {
        try
        {
            if (!_initialized || _currentPage == null)
            {
                return WebNavigationResult.ErrorResult("Browser not initialized. Call InitializeBrowserAsync first.");
            }

            _logger.LogInformation("Clicking element with selector: {Selector}", selector);

            options ??= new ClickOptions();

            var clickOptions = new LocatorClickOptions
            {
                Button = options.Button switch
                {
                    MouseButton.Left => Microsoft.Playwright.MouseButton.Left,
                    MouseButton.Right => Microsoft.Playwright.MouseButton.Right,
                    MouseButton.Middle => Microsoft.Playwright.MouseButton.Middle,
                    _ => Microsoft.Playwright.MouseButton.Left
                },
                ClickCount = options.ClickCount
            };

            if (options.Position != null)
            {
                clickOptions.Position = new Microsoft.Playwright.Position { X = (float)options.Position.X, Y = (float)options.Position.Y };
            }

            if (options.Modifiers != KeyModifiers.None)
            {
                var modifiers = new List<KeyboardModifier>();
                if (options.Modifiers.HasFlag(KeyModifiers.Alt))
                {
                    modifiers.Add(KeyboardModifier.Alt);
                }
                if (options.Modifiers.HasFlag(KeyModifiers.Control))
                {
                    modifiers.Add(KeyboardModifier.Control);
                }
                if (options.Modifiers.HasFlag(KeyModifiers.Meta))
                {
                    modifiers.Add(KeyboardModifier.Meta);
                }
                if (options.Modifiers.HasFlag(KeyModifiers.Shift))
                {
                    modifiers.Add(KeyboardModifier.Shift);
                }
                clickOptions.Modifiers = modifiers;
            }

            var element = _currentPage.Locator(selector);
            await element.ClickAsync(clickOptions);

            _logger.LogInformation("Successfully clicked element: {Selector}", selector);

            return WebNavigationResult.SuccessResult(new { Selector = selector, Options = options }, 
                $"Successfully clicked element: {selector}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to click element: {Selector}", selector);
            return WebNavigationResult.ErrorResult($"Click operation failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<WebNavigationResult> FillInputAsync(string selector, string text, FillOptions? options = null)
    {
        try
        {
            if (!_initialized || _currentPage == null)
            {
                return WebNavigationResult.ErrorResult("Browser not initialized. Call InitializeBrowserAsync first.");
            }

            _logger.LogInformation("Filling input element: {Selector} with text length: {Length}", selector, text.Length);

            options ??= new FillOptions();

            var element = _currentPage.Locator(selector);
            
            if (options.Clear)
            {
                await element.ClearAsync();
            }

            if (options.Delay > 0)
            {
                // Use PressSequentiallyAsync instead of deprecated TypeAsync
                await element.PressSequentiallyAsync(text, new LocatorPressSequentiallyOptions { Delay = options.Delay });
            }
            else
            {
                await element.FillAsync(text);
            }

            _logger.LogInformation("Successfully filled input: {Selector}", selector);

            return WebNavigationResult.SuccessResult(new { Selector = selector, TextLength = text.Length, Options = options },
                $"Successfully filled input: {selector}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fill input: {Selector}", selector);
            return WebNavigationResult.ErrorResult($"Fill operation failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<WebNavigationResult> ExtractTextAsync(string selector, bool multiple = false)
    {
        try
        {
            if (!_initialized || _currentPage == null)
            {
                return WebNavigationResult.ErrorResult("Browser not initialized. Call InitializeBrowserAsync first.");
            }

            _logger.LogInformation("Extracting text from selector: {Selector}, multiple: {Multiple}", selector, multiple);

            if (multiple)
            {
                var elements = _currentPage.Locator(selector);
                var count = await elements.CountAsync();
                var texts = new List<string>();

                for (int i = 0; i < count; i++)
                {
                    var text = await elements.Nth(i).TextContentAsync();
                    if (!string.IsNullOrEmpty(text))
                    {
                        texts.Add(text.Trim());
                    }
                }

                _logger.LogInformation("Extracted text from {Count} elements", texts.Count);
                return WebNavigationResult.SuccessResult(texts, $"Extracted text from {texts.Count} elements");
            }
            else
            {
                var element = _currentPage.Locator(selector);
                var text = await element.TextContentAsync();
                text = text?.Trim() ?? string.Empty;

                _logger.LogInformation("Extracted text length: {Length}", text.Length);
                return WebNavigationResult.SuccessResult(text, $"Extracted text from element: {selector}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from selector: {Selector}", selector);
            return WebNavigationResult.ErrorResult($"Text extraction failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<WebNavigationResult> TakeScreenshotAsync(string? selector = null, ScreenshotOptions? options = null)
    {
        try
        {
            if (!_initialized || _currentPage == null)
            {
                return WebNavigationResult.ErrorResult("Browser not initialized. Call InitializeBrowserAsync first.");
            }

            _logger.LogInformation("Taking screenshot, selector: {Selector}", selector ?? "full page");

            options ??= new ScreenshotOptions();
            byte[] screenshot;

            if (string.IsNullOrEmpty(selector))
            {
                // Full page screenshot
                var screenshotOptions = new PageScreenshotOptions
                {
                    FullPage = options.FullPage,
                    Type = options.Format == ScreenshotFormat.Jpeg ? ScreenshotType.Jpeg : ScreenshotType.Png
                };

                if (options.Format == ScreenshotFormat.Jpeg)
                {
                    screenshotOptions.Quality = options.Quality;
                }

                screenshot = await _currentPage.ScreenshotAsync(screenshotOptions);
            }
            else
            {
                // Element screenshot
                var element = _currentPage.Locator(selector);
                var screenshotOptions = new LocatorScreenshotOptions
                {
                    Type = options.Format == ScreenshotFormat.Jpeg ? ScreenshotType.Jpeg : ScreenshotType.Png
                };

                if (options.Format == ScreenshotFormat.Jpeg)
                {
                    screenshotOptions.Quality = options.Quality;
                }

                screenshot = await element.ScreenshotAsync(screenshotOptions);
            }

            _logger.LogInformation("Screenshot taken, size: {Size} bytes", screenshot.Length);

            var result = new
            {
                Screenshot = screenshot,
                Format = options.Format.ToString(),
                Size = screenshot.Length,
                Selector = selector,
                Timestamp = DateTime.UtcNow
            };

            return WebNavigationResult.SuccessResult(result, 
                $"Screenshot taken successfully ({screenshot.Length} bytes)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to take screenshot");
            return WebNavigationResult.ErrorResult($"Screenshot operation failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<WebNavigationResult> WaitForElementAsync(string selector, ElementState state = ElementState.Visible, int timeout = 30000)
    {
        try
        {
            if (!_initialized || _currentPage == null)
            {
                return WebNavigationResult.ErrorResult("Browser not initialized. Call InitializeBrowserAsync first.");
            }

            _logger.LogInformation("Waiting for element: {Selector}, state: {State}", selector, state);

            var waitForSelectorState = state switch
            {
                ElementState.Attached => WaitForSelectorState.Attached,
                ElementState.Detached => WaitForSelectorState.Detached,
                ElementState.Visible => WaitForSelectorState.Visible,
                ElementState.Hidden => WaitForSelectorState.Hidden,
                _ => WaitForSelectorState.Visible
            };

            await _currentPage.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
            {
                State = waitForSelectorState,
                Timeout = timeout
            });

            _logger.LogInformation("Element found in expected state: {Selector}", selector);

            return WebNavigationResult.SuccessResult(new { Selector = selector, State = state.ToString() },
                $"Element found in expected state: {selector}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to wait for element: {Selector}", selector);
            return WebNavigationResult.ErrorResult($"Wait operation failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<WebNavigationResult> ExecuteScriptAsync(string script, params object[] args)
    {
        try
        {
            if (!_initialized || _currentPage == null)
            {
                return WebNavigationResult.ErrorResult("Browser not initialized. Call InitializeBrowserAsync first.");
            }

            _logger.LogInformation("Executing JavaScript script");

            var result = await _currentPage.EvaluateAsync(script, args);

            _logger.LogInformation("JavaScript executed successfully");

            return WebNavigationResult.SuccessResult(result, "JavaScript executed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute JavaScript");
            return WebNavigationResult.ErrorResult($"Script execution failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<WebNavigationResult> GetPageInfoAsync()
    {
        try
        {
            if (!_initialized || _currentPage == null)
            {
                return WebNavigationResult.ErrorResult("Browser not initialized. Call InitializeBrowserAsync first.");
            }

            var pageInfo = new
            {
                Url = _currentPage.Url,
                Title = await _currentPage.TitleAsync(),
                ViewportSize = _currentPage.ViewportSize,
                UserAgent = await _currentPage.EvaluateAsync<string>("navigator.userAgent"),
                LoadedAt = DateTime.UtcNow
            };

            return WebNavigationResult.SuccessResult(pageInfo, "Page information retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get page information");
            return WebNavigationResult.ErrorResult($"Failed to get page info: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<WebNavigationResult> DisposeBrowserAsync()
    {
        try
        {
            if (!_initialized)
            {
                return WebNavigationResult.SuccessResult(null, "Browser was not initialized");
            }

            _logger.LogInformation("Disposing browser resources");

            if (_currentPage != null)
            {
                await _currentPage.CloseAsync();
                _currentPage = null;
            }

            if (_browser != null)
            {
                await _browser.CloseAsync();
                _browser = null;
            }

            if (_playwright != null)
            {
                _playwright.Dispose();
                _playwright = null;
            }

            _initialized = false;
            _logger.LogInformation("Browser resources disposed successfully");

            return WebNavigationResult.SuccessResult(null, "Browser disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispose browser");
            return WebNavigationResult.ErrorResult($"Browser disposal failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsBrowserReadyAsync()
    {
        return await Task.FromResult(_initialized && _browser != null && _currentPage != null);
    }

    /// <summary>
    /// Dispose pattern implementation
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await DisposeBrowserAsync();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // For sync disposal, use async-over-sync pattern
            // This is not ideal but necessary for IDisposable contract
            try
            {
                DisposeBrowserAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during synchronous disposal of WebNavigationService");
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}