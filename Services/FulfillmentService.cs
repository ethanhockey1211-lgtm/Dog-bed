using DogBed.Models;
using Microsoft.Playwright;

namespace DogBed.Services;

/// <summary>
/// Uses Playwright to log into AliExpress and place the customer's order automatically.
///
/// FIRST-TIME SETUP:
///   1. Set AliExpress:Headless = false in appsettings.json
///   2. Run the app and place a test order
///   3. A browser will open — log in manually and complete any CAPTCHA
///   4. The session is saved to aliexpress-session.json automatically
///   5. Set AliExpress:Headless = true — future orders run silently
///
/// IMPORTANT: Keep aliexpress-session.json out of source control (.gitignore).
/// </summary>
public class FulfillmentService
{
    private readonly IConfiguration _config;
    private readonly ILogger<FulfillmentService> _logger;
    private const string SessionFile = "aliexpress-session.json";

    public FulfillmentService(IConfiguration config, ILogger<FulfillmentService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task FulfillAsync(FulfillmentOrder order, CancellationToken ct)
    {
        order.Status = FulfillmentStatus.Processing;
        _logger.LogInformation("Starting fulfillment for order {Id}", order.Id);

        using var playwright = await Playwright.CreateAsync();

        var headless = _config.GetValue<bool>("AliExpress:Headless");
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = headless,
            SlowMo   = headless ? 400 : 700,
            Args     = ["--disable-blink-features=AutomationControlled", "--no-sandbox"]
        });

        var ctxOpts = new BrowserNewContextOptions
        {
            UserAgent    = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                           "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            ViewportSize = new() { Width = 1440, Height = 900 }
        };
        if (File.Exists(SessionFile)) ctxOpts.StorageStatePath = SessionFile;

        await using var ctx  = await browser.NewContextAsync(ctxOpts);
        await ctx.AddInitScriptAsync(
            "Object.defineProperty(navigator,'webdriver',{get:()=>undefined})");

        var page = await ctx.NewPageAsync();

        try
        {
            await EnsureLoggedInAsync(page, ctx);

            foreach (var item in order.Items)
            {
                for (int q = 0; q < item.Quantity; q++)
                {
                    await PlaceOneItemAsync(page, order, item);
                    if (q < item.Quantity - 1)
                        await page.WaitForTimeoutAsync(3000);
                }
            }

            await ctx.StorageStateAsync(new() { Path = SessionFile });
            order.Status = FulfillmentStatus.Fulfilled;
            _logger.LogInformation("Order {Id} fulfilled — AliExpress ID: {AliId}",
                order.Id, order.AliExpressOrderId ?? "unknown");
        }
        catch (Exception ex)
        {
            order.Status      = FulfillmentStatus.Failed;
            order.ErrorMessage = ex.Message;

            var shot = $"error-{order.Id}.png";
            await page.ScreenshotAsync(new() { Path = shot, FullPage = true });
            _logger.LogError(ex, "Fulfillment error — screenshot: {Shot}", shot);
            throw;
        }
    }

    // ── Login ────────────────────────────────────────────────────────────────
    private async Task EnsureLoggedInAsync(IPage page, IBrowserContext ctx)
    {
        await page.GotoAsync("https://www.aliexpress.com",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForTimeoutAsync(2500);

        // If account icon is visible, already logged in
        var loggedIn = await page.QuerySelectorAsync(
            "[class*='account--'], [class*='user-account'], #nav-user-account, " +
            "[class*='my-account'], [data-role='user-account']");
        if (loggedIn != null)
        {
            _logger.LogInformation("Already logged in to AliExpress");
            return;
        }

        _logger.LogInformation("Logging in to AliExpress…");
        await page.GotoAsync("https://login.aliexpress.com/",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForTimeoutAsync(3000);

        var email = _config["AliExpress:Email"]!;
        var pwd   = _config["AliExpress:Password"]!;

        var emailInput = await WaitForFirstAsync(page,
            "#fm-login-id", "input[name='loginId']", "input[type='email']");
        await emailInput.FillAsync(email);
        await page.WaitForTimeoutAsync(600);

        var pwdInput = await WaitForFirstAsync(page,
            "#fm-login-password", "input[name='password']", "input[type='password']");
        await pwdInput.FillAsync(pwd);
        await page.WaitForTimeoutAsync(600);

        var submit = await WaitForFirstAsync(page,
            "#login-submit", "button[type='submit']", "input[type='submit']");
        await submit.ClickAsync();
        await page.WaitForTimeoutAsync(5000);

        // If still on login/captcha page, AliExpress needs manual intervention
        var url = page.Url;
        if (url.Contains("login") || url.Contains("captcha") || url.Contains("verify"))
        {
            if (!_config.GetValue<bool>("AliExpress:Headless"))
            {
                _logger.LogWarning(
                    "AliExpress requires CAPTCHA or 2FA — please complete it in the browser. " +
                    "Waiting up to 90 seconds…");
                await page.WaitForURLAsync("**aliexpress.com/**", new() { Timeout = 90_000 });
            }
            else
            {
                throw new InvalidOperationException(
                    "AliExpress login blocked (CAPTCHA/2FA). " +
                    "Set AliExpress:Headless=false, run once to log in manually, " +
                    "then set it back to true.");
            }
        }

        await ctx.StorageStateAsync(new() { Path = SessionFile });
        _logger.LogInformation("Login successful — session saved");
    }

    // ── Place one item ───────────────────────────────────────────────────────
    private async Task PlaceOneItemAsync(IPage page, FulfillmentOrder order, CartItem item)
    {
        var productUrl = _config["AliExpress:ProductUrl"]!;
        _logger.LogInformation("Going to product page: {Url}", productUrl);

        await page.GotoAsync(productUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForTimeoutAsync(3000);

        await SelectSizeAsync(page, item.Size);
        await page.WaitForTimeoutAsync(800);

        await ClickBuyNowAsync(page);
        await page.WaitForTimeoutAsync(5000);

        await FillShippingAddressAsync(page, order);
        await page.WaitForTimeoutAsync(1200);

        var aliId = await ClickPlaceOrderAsync(page);
        order.AliExpressOrderId = order.AliExpressOrderId == null
            ? aliId
            : order.AliExpressOrderId + "," + aliId;
    }

    // ── Select size ──────────────────────────────────────────────────────────
    private async Task SelectSizeAsync(IPage page, string ourSize)
    {
        var label = _config[$"AliExpress:SizeMap:{ourSize}"] ?? ourSize;
        _logger.LogInformation("Selecting size: {Label}", label);

        // Wait for SKU options to render
        try
        {
            await page.WaitForSelectorAsync(
                "[class*='sku-property'], [class*='product-sku'], [class*='SKU']",
                new() { Timeout = 15_000 });
        }
        catch
        {
            _logger.LogWarning("SKU selector timed out — proceeding anyway");
        }
        await page.WaitForTimeoutAsync(1000);

        // Try four strategies in order
        if (await TryClickAsync(page, $".sku-property-item:has-text('{label}')")) return;
        if (await TryClickAsync(page, $"[class*='sku-property'] span:text-is('{label}')")) return;
        if (await TryClickAsync(page, $"[class*='sku'] span:text-is('{label}')")) return;
        if (await TryClickByTextAsync(page, label, "[class*='sku'] span, [class*='SKU'] span")) return;

        throw new Exception(
            $"Could not find size '{label}' on the product page. " +
            $"Check AliExpress:SizeMap in appsettings.json and verify the product URL.");
    }

    // ── Buy Now ──────────────────────────────────────────────────────────────
    private async Task ClickBuyNowAsync(IPage page)
    {
        _logger.LogInformation("Clicking Buy Now");
        var selectors = new[]
        {
            "[data-pl='buy-now']", ".buy-now--btn", "#buyNowBtn",
            "button:has-text('Buy Now')", "a:has-text('Buy Now')",
            "[class*='buy-now']", "button:has-text('Buy it now')"
        };
        foreach (var sel in selectors)
            if (await TryClickAsync(page, sel)) return;

        throw new Exception("Could not find the 'Buy Now' button on the product page.");
    }

    // ── Fill shipping address ────────────────────────────────────────────────
    private async Task FillShippingAddressAsync(IPage page, FulfillmentOrder order)
    {
        _logger.LogInformation("Filling shipping address for {Name}", order.CustomerName);
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await page.WaitForTimeoutAsync(2000);

        // Try to open address entry form
        var openSelectors = new[]
        {
            "button:has-text('Add new address')", "a:has-text('Add new address')",
            "button:has-text('New Address')",     "[class*='add-address']",
            "button:has-text('Add Address')"
        };
        var opened = false;
        foreach (var sel in openSelectors)
            if (await TryClickAsync(page, sel)) { opened = true; break; }

        if (!opened)
        {
            // Try "Change" to open address picker, then find "Add new"
            if (await TryClickAsync(page, "button:has-text('Change')"))
            {
                await page.WaitForTimeoutAsync(1500);
                foreach (var sel in openSelectors)
                    if (await TryClickAsync(page, sel)) { opened = true; break; }
            }
        }

        await page.WaitForTimeoutAsync(1500);

        // Fill each field — try multiple possible selectors per field
        await FillAsync(page, order.CustomerName,
            "#input-name",   "input[name='fullname']",
            "input[placeholder*='name' i]",     "input[id*='contact' i]");

        await FillAsync(page, order.Phone,
            "#input-mobile", "input[name='mobile']",
            "input[name='phone']",              "input[placeholder*='phone' i]");

        await FillAsync(page, order.Address,
            "#input-address", "input[name='address']",
            "input[placeholder*='street' i]",   "input[placeholder*='address' i]");

        await FillAsync(page, order.City,
            "#input-city",   "input[name='city']",
            "input[placeholder*='city' i]");

        await FillAsync(page, order.State,
            "#input-province", "input[name='province']",
            "input[name='state']",              "input[placeholder*='state' i]",
            "input[placeholder*='province' i]");

        await FillAsync(page, order.ZipCode,
            "#input-zip",    "input[name='zip']",
            "input[name='zipCode']",            "input[placeholder*='zip' i]",
            "input[placeholder*='postal' i]");

        await page.WaitForTimeoutAsync(500);

        // Save the address
        var saveSels = new[]
        {
            "button:has-text('Save')", "button:has-text('Confirm')",
            "button:has-text('OK')",   "[class*='save-btn']",
            "input[type='submit']"
        };
        foreach (var sel in saveSels)
            if (await TryClickAsync(page, sel)) break;

        await page.WaitForTimeoutAsync(2000);
    }

    // ── Place order & capture ID ─────────────────────────────────────────────
    private async Task<string?> ClickPlaceOrderAsync(IPage page)
    {
        _logger.LogInformation("Placing order");
        var selectors = new[]
        {
            ".place-order--btn",              "button:has-text('Place Order')",
            "button:has-text('Confirm Order')", "[class*='place-order']",
            "[class*='confirm-order']",       "button:has-text('Submit Order')"
        };
        foreach (var sel in selectors)
            if (await TryClickAsync(page, sel)) break;

        await page.WaitForTimeoutAsync(6000);

        // Try to extract AliExpress order number from URL or page
        var orderId = ExtractOrderId(page.Url)
                   ?? ExtractOrderId(await page.ContentAsync());

        _logger.LogInformation("AliExpress order ID: {Id}", orderId ?? "(not captured)");
        return orderId;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static string? ExtractOrderId(string text)
    {
        var m = System.Text.RegularExpressions.Regex.Match(text, @"orderId[=_/](\d{8,20})");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static async Task<bool> TryClickAsync(IPage page, string selector)
    {
        try
        {
            var el = await page.QuerySelectorAsync(selector);
            if (el == null) return false;
            await el.ScrollIntoViewIfNeededAsync();
            await el.ClickAsync(new() { Timeout = 4000 });
            return true;
        }
        catch { return false; }
    }

    private static async Task<bool> TryClickByTextAsync(
        IPage page, string text, string containerSel)
    {
        try
        {
            foreach (var el in await page.QuerySelectorAllAsync(containerSel))
            {
                var t = await el.TextContentAsync();
                if (string.Equals(t?.Trim(), text, StringComparison.OrdinalIgnoreCase))
                {
                    await el.ClickAsync(new() { Timeout = 4000 });
                    return true;
                }
            }
            return false;
        }
        catch { return false; }
    }

    private static async Task FillAsync(IPage page, string value, params string[] selectors)
    {
        foreach (var sel in selectors)
        {
            try
            {
                var el = await page.QuerySelectorAsync(sel);
                if (el == null) continue;
                await el.FillAsync(value);
                return;
            }
            catch { /* try next */ }
        }
    }

    private static async Task<IElementHandle> WaitForFirstAsync(
        IPage page, params string[] selectors)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            foreach (var sel in selectors)
            {
                var el = await page.QuerySelectorAsync(sel);
                if (el != null) return el;
            }
            await page.WaitForTimeoutAsync(500);
        }
        throw new Exception(
            $"None of the selectors appeared: {string.Join(", ", selectors)}");
    }
}
