# خطة تطبيق المرحلة 03 — إدخال اللغة الآمن
# Phase 03 Implementation Plan — Secure Language Entry

**التاريخ:** 12 أغسطس 2026
**الخط الأساسي:** f91dbaf feat: add identity and tenant foundation
**الحالة:** بانتظار الموافقة (NO CODING YET)

---

## 1. ملخص المرحلة 03

### الهدف
بناء بنية التوطين والمصادقة الآمنة مع دعم اللغات الإنجليزية والبولندية.

### نطاق المرحلة
✅ صفحة اختيار اللغة (Language Selector)
✅ بنية بيانات الثقافة والتوطين (Localization Infrastructure)
✅ صفحة تسجيل الدخول (Login) مع تحقق من البيانات مترجمة
✅ تسجيل الخروج (Logout)
✅ صفحة Access Denied مترجمة
✅ لوحة معلومات محمية أولية (Protected Dashboard)
✅ سياسات التفويض (Authorization Policies)
✅ واجهة مستخدم استجابية (Responsive UI)
✅ اختبارات شاملة

❌ إدارة المدارس (School Management) — المرحلة 04
❌ تسجيل جديد عام (Public Registration)
❌ تغييرات قاعدة البيانات (Database Schema Changes) — غير مطلوبة

---

## 2. متطلبات المواصفات الموثوقة

### 10.1 تدفق اللغات الأولي
```
Language Selector
→ User chooses Polski or English
→ Login page appears entirely in selected language
→ Application appears entirely in selected language
→ Sign out clears selected language
→ Return to language selector
```

### 10.2 صفحة اختيار اللغة (Language Selector)
```
Edulytics logo/name

[ 🇵🇱 Polski ]
[ 🇬🇧 English ]
```

**القواعد:**
- بطاقتان فقط
- بدون نص مرحب طويل
- بدون شرح إضافي
- هي الصفحة الثنائية اللغة الوحيدة بقصد

### 10.3 قاعدة التوطين الصارمة

بعد اختيار اللغة، يجب أن تستخدم كل نص مرئي وقابل للوصول اللغة المختارة فقط.

**يشمل:**
- عناوين الصفحات (Page titles)
- العناوين الكبرى (Headings)
- العناوين الفرعية (Subheadings)
- التسميات (Labels)
- الأزرار (Buttons)
- الروابط (Links)
- نصوص الحقول (Placeholders)
- رسائل التحقق (Validation messages)
- أخطاء تسجيل الدخول (Login errors)
- أخطاء التفويض (Authorization errors)
- التنبيهات (Alerts)
- حالات فارغة (Empty states)
- تسميات الحالة (Status labels)
- رؤوس الجداول (Table headings)
- نصوص النوافذ المنبثقة (Modal text)
- رسائل التأكيد (Confirmation messages)
- رسائل النجاح (Success messages)
- ARIA labels و screen-reader text
- Tooltips

**المتطلب الوظيفي:**
- صفحة البولندية لا تظهر fallback إنجليزي
- صفحة الإنجليزية لا تظهر fallback بولندي

### 11. متطلبات واجهة المستخدم الاستجابية

يجب أن تعمل كل صفحة على:
```
320px, 375px, 480px, 768px, 1024px, 1280px, 1440px+
```

**الصفحة غير مكتملة إذا كانت تحتوي على:**
- تمرير أفقي (Horizontal scrolling)
- نص مقطوع (Clipped text)
- واجهة مستخدم متداخلة (Overlapping UI)
- إجراء أساسي مخفي (Hidden primary action)
- أخطاء غير قابلة للقراءة (Unreadable errors)
- نماذج غير صالحة للاستخدام (Unusable forms)
- جداول محمول مكسورة (Broken mobile tables)
- سلوك تغيير حجم سيء (Poor resizing behavior)

**معايير الإكمال:**
```
Functionality
+ selected-language completeness
+ responsive behavior
+ validation/error states
+ manual browser verification
```

---

## 3. المتطلبات غير الوظيفية

### 3.1 الأمان
- لا توجد تسجيلات عامة جديدة (No public registration)
- المصادقة الآمنة (Secure authentication cookies)
- Anti-forgery على جميع POSTs التي تغير الحالة
- No state-changing GETs
- تحقق من التفويض على جميع endpoints المحمية
- لا توجد تسريبات بيانات المستخدم عبر الأخطاء

### 3.2 الأداء
- لا توجد استدعاءات قاعدة بيانات غير ضرورية
- Localization resources مخزنة مؤقتاً في الذاكرة
- Cookie culture مؤقتة على المتصفح

### 3.3 الاختبار
- اختبارات الوحدة لـ localization service
- اختبارات التكامل لـ authentication flow
- اختبارات سياسات التفويض (Authorization policies)
- تغطية رسائل الخطأ المترجمة

---

## 4. بنية الملفات والمشاريع

### بدون تغييرات قاعدة البيانات
لا توجد migrations جديدة مطلوبة. المستخدم والأدوار موجودة من المرحلة 02.

### الملفات الجديدة المطلوبة

#### 4.1 Edulytics.Core
```
src/Edulytics.Core/
├── Constants/
│   ├── CultureConstants.cs (جديد)
│   └── RoleNames.cs (موجود)
└── Enums/ (موجود)
```

**CultureConstants.cs:**
- `DefaultCulture = "en-US"`
- `SupportedCultures = { "en-US", "pl-PL" }`
- `CultureCookieName = "Edulytics.Culture"`
- `CultureCookieExpiration = 365 days`

#### 4.2 Edulytics.Services
```
src/Edulytics.Services/
├── Localization/
│   └── LocalizationService.cs (جديد)
└── Abstractions/
    └── ILocalizationService.cs (جديد)
```

**ILocalizationService:**
```csharp
public interface ILocalizationService
{
    string? GetCultureFromRequest(HttpContext context);
    void SetCultureCookie(HttpResponse response, string culture);
    void ClearCultureCookie(HttpResponse response);
    CultureInfo GetCultureInfo(string? culture);
    bool IsSupportedCulture(string? culture);
}
```

#### 4.3 Edulytics.Data
```
(بدون تغييرات)
```

#### 4.4 Edulytics.Web
```
src/Edulytics.Web/
├── Controllers/
│   ├── HomeController.cs (معدّل)
│   ├── AccountController.cs (جديد)
│   └── PlatformController.cs (جديد)
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml (صفحة Language Selector - جديد)
│   │   └── AccessDenied.cshtml (جديد)
│   ├── Account/
│   │   ├── Login.cshtml (جديد)
│   │   └── Logout.cshtml (جديد)
│   └── Platform/
│       └── SuperAdminDashboard.cshtml (جديد)
├── ViewModels/
│   ├── Account/
│   │   └── LoginViewModel.cs (جديد)
│   └── Home/
│       └── IndexViewModel.cs (جديد)
├── Resources/
│   ├── Localization/
│   │   ├── en-US/
│   │   │   ├── Account.en-US.resx (جديد)
│   │   │   ├── Home.en-US.resx (جديد)
│   │   │   ├── Platform.en-US.resx (جديد)
│   │   │   ├── Common.en-US.resx (جديد)
│   │   │   └── Validation.en-US.resx (جديد)
│   │   └── pl-PL/
│   │       ├── Account.pl-PL.resx (جديد)
│   │       ├── Home.pl-PL.resx (جديد)
│   │       ├── Platform.pl-PL.resx (جديد)
│   │       ├── Common.pl-PL.resx (جديد)
│   │       └── Validation.pl-PL.resx (جديد)
├── Middleware/
│   └── CultureMiddleware.cs (جديد)
├── Extensions/
│   ├── ServiceCollectionExtensions.cs (معدّل)
│   └── LocalizationExtensions.cs (جديد)
├── wwwroot/
│   ├── css/
│   │   └── site.css (معدّل - responsive design)
│   └── js/
│       └── site.js (جديد إذا لزم الأمر)
├── Views/Shared/
│   ├── _Layout.cshtml (معدّل)
│   ├── _LoginPartial.cshtml (جديد)
│   └── Error.cshtml (معدّل)
└── Program.cs (معدّل)
```

#### 4.5 Edulytics.Tests
```
tests/Edulytics.Tests/
├── Localization/
│   ├── LocalizationServiceTests.cs (جديد)
│   └── LocalizationResourcesTests.cs (جديد)
├── Controllers/
│   ├── AccountControllerTests.cs (جديد)
│   ├── HomeControllerTests.cs (جديد)
│   └── PlatformControllerTests.cs (جديد)
├── Authorization/
│   └── PlatformAdministrationPolicyTests.cs (جديد)
└── Integration/
    └── LanguageFlowIntegrationTests.cs (جديد)
```

---

## 5. تفاصيل المكونات

### 5.1 صفحة اختيار اللغة (Language Selector)

**الملف:** `Views/Home/Index.cshtml`

**المتطلبات:**
- ثنائية اللغة فقط
- بطاقتان: Polski و English
- شعار/اسم Edulytics
- بدون نص إضافي
- Responsive: 320px - 1440px+

**المنطق:**
```
GET /
→ If culture cookie exists → Redirect to login with culture
→ Else → Show language selector
```

**التصميم:**
- Container centered
- Two cards side-by-side على desktop
- Stacked على mobile
- أيقونات الأعلام (🇵🇱 🇬🇧)
- حجم ملائم للتوجيه

**الإجراء:**
```
Click [ 🇵🇱 Polski ]
→ POST /set-culture?culture=pl-PL
→ Set cookie "Edulytics.Culture"
→ Redirect to /account/login

Click [ 🇬🇧 English ]
→ POST /set-culture?culture=en-US
→ Set cookie "Edulytics.Culture"
→ Redirect to /account/login
```

### 5.2 صفحة تسجيل الدخول (Login)

**الملف:** `Views/Account/Login.cshtml`

**المتطلبات:**
- بلغة واحدة فقط بناءً على culture cookie
- No public registration link
- عنوان مترجم: "تسجيل الدخول" (pl) / "Sign In" (en)
- حقول: Email و Password
- زر: "تسجيل الدخول" (pl) / "Sign In" (en)
- رابط: "هل نسيت كلمة المرور؟" (pl) / "Forgot password?" (en) — مخفي للمرحلة 03
- رسائل خطأ مترجمة:
  - "البريد الإلكتروني أو كلمة المرور غير صحيحة" (pl)
  - "Email or password is incorrect" (en)
  - "حسابك معطّل" (pl) / "Your account is disabled" (en)
  - "محاولات دخول متعددة. يرجى المحاولة لاحقاً" (pl) / "Too many login attempts. Please try again later." (en)
- Anti-forgery token
- Responsive: 320px - 1440px+

**ViewModel:**
```csharp
public class LoginViewModel
{
    [Required(ErrorMessage = "EmailRequired")]
    [EmailAddress(ErrorMessage = "EmailInvalid")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "PasswordRequired")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Display(Name = "RememberMe")]
    public bool RememberMe { get; set; }
}
```

**المنطق:**
```
GET /account/login
→ Check culture cookie
→ If not set → Redirect to /
→ Render login form in selected language

POST /account/login
→ Validate CSRF token
→ Validate email/password format
→ Call UserManager.FindByEmailAsync
→ If not found → Localized error "Invalid credentials"
→ Call SignInManager.PasswordSignInAsync
→ If success → Redirect to dashboard or return URL
→ If failed → Localized error based on reason
    - IsLockedOut → Lockout error
    - !IsEmailConfirmed → Email confirmation required
    - Else → Invalid credentials
→ If no culture cookie → Redirect to /
```

### 5.3 تسجيل الخروج (Logout)

**الملف:** `Views/Account/Logout.cshtml` أو POST controller action

**المتطلبات:**
- Clear authentication cookie
- Clear culture cookie
- Redirect to language selector

**المنطق:**
```
GET /account/logout
→ Call SignInManager.SignOutAsync()
→ Clear "Edulytics.Culture" cookie
→ Redirect to /

POST /account/logout
→ (Same as above, with Anti-forgery validation)
```

### 5.4 صفحة الوصول المرفوض (Access Denied)

**الملف:** `Views/Home/AccessDenied.cshtml`

**المتطلبات:**
- رسالة مترجمة: "لا توجد صلاحيات كافية" (pl) / "Access Denied" (en)
- وصف: "أنت لا تملك الأذونات المطلوبة للوصول إلى هذا المورد" (pl) / "You do not have permission to access this resource." (en)
- رابط للعودة إلى الصفحة السابقة أو الرئيسية
- Responsive: 320px - 1440px+

**المنطق:**
```
GET /access-denied
→ Check if user is authenticated
→ If not → Redirect to login
→ Render access denied page in user's culture
```

### 5.5 لوحة معلومات المسؤول العام (SuperAdmin Dashboard)

**الملف:** `Views/Platform/SuperAdminDashboard.cshtml`

**المتطلبات:**
- فقط للمستخدمين بـ SuperAdmin role
- عنوان مترجم: "لوحة معلومات المسؤول" (pl) / "Platform Dashboard" (en)
- ترحيب: "مرحباً بك في Edulytics" (pl) / "Welcome to Edulytics" (en)
- معلومات بسيطة:
  - عدد المدارس (Schools count)
  - عدد المستخدمين (Users count)
  - الإجراء الأخير (Last action)
- رابط تسجيل الخروج
- Responsive: 320px - 1440px+

**المنطق:**
```
GET /platform/dashboard
→ Check if user.SchoolId == null (SuperAdmin)
→ If not → Redirect to /access-denied
→ Query: Count(Schools), Count(Users), Last audit log
→ Render dashboard in user's culture
```

### 5.6 سياسات التفويض (Authorization Policies)

**الملف:** `Extensions/ServiceCollectionExtensions.cs` (معدّل)

**السياسات:**
```csharp
services.AddAuthorization(options =>
{
    // Existing: PlatformAdministration
    options.AddPolicy("PlatformAdministration", policy =>
        policy.RequireRole(RoleNames.SuperAdmin));

    // New: CanAccessPlatformAdminArea
    options.AddPolicy("CanAccessPlatformAdminArea", policy =>
        policy
            .RequireAuthenticatedUser()
            .RequireRole(RoleNames.SuperAdmin));
});
```

**استخدام في الـ Controller:**
```csharp
[Authorize(Policy = "CanAccessPlatformAdminArea")]
public class PlatformController : Controller
{
    [HttpGet("dashboard")]
    public IActionResult SuperAdminDashboard()
    {
        // Implementation
    }
}
```

### 5.7 Localization Resources (.resx files)

**المجموعات المطلوبة:**

#### Common.resx
```
Strings:
- AppName = "Edulytics"
- LogOut = "تسجيل الخروج" / "Log Out"
- Login = "تسجيل الدخول" / "Sign In"
- AccessDenied = "الوصول مرفوض" / "Access Denied"
- GoHome = "العودة إلى الرئيسية" / "Go Home"
```

#### Account.resx
```
Strings:
- SignInTitle = "تسجيل الدخول" / "Sign In"
- Email = "البريد الإلكتروني" / "Email"
- Password = "كلمة المرور" / "Password"
- RememberMe = "تذكري" / "Remember me"
- SignInButton = "دخول" / "Sign In"
- ForgotPassword = "هل نسيت كلمة المرور؟" / "Forgot your password?"
- InvalidCredentials = "البريد الإلكتروني أو كلمة المرور غير صحيحة" / "Invalid email or password"
- AccountLocked = "حسابك مقفول. الرجاء محاولة لاحقاً" / "Your account is locked. Please try again later."
- TooManyAttempts = "محاولات دخول متعددة جداً. الرجاء المحاولة لاحقاً" / "Too many login attempts. Please try again later."
- AccountDisabled = "حسابك معطّل" / "Your account is disabled"
- EmailConfirmationRequired = "يجب تأكيد البريد الإلكتروني قبل تسجيل الدخول" / "Email confirmation is required before signing in"
```

#### Home.resx
```
Strings:
- LanguageSelectorTitle = "اختر اللغة" / "Choose Language"
- English = "English"
- Polish = "Polski"
```

#### Platform.resx
```
Strings:
- DashboardTitle = "لوحة معلومات المسؤول" / "Platform Dashboard"
- Welcome = "مرحباً بك في Edulytics" / "Welcome to Edulytics"
- TotalSchools = "إجمالي المدارس" / "Total Schools"
- TotalUsers = "إجمالي المستخدمين" / "Total Users"
- ManageSchools = "إدارة المدارس" / "Manage Schools"
```

#### Validation.resx
```
Strings:
- EmailRequired = "البريد الإلكتروني مطلوب" / "Email is required"
- EmailInvalid = "البريد الإلكتروني غير صحيح" / "Invalid email"
- PasswordRequired = "كلمة المرور مطلوبة" / "Password is required"
- PasswordTooShort = "كلمة المرور قصيرة جداً" / "Password is too short"
```

### 5.8 Middleware للثقافة (CultureMiddleware)

**الملف:** `Middleware/CultureMiddleware.cs`

**الهدف:**
- قراءة culture cookie من الطلب
- تعيين CurrentCulture و CurrentUICulture في الخيط
- التعامل مع الثقافات غير المدعومة

**المنطق:**
```csharp
public class CultureMiddleware
{
    public async Task InvokeAsync(
        HttpContext context,
        ILocalizationService localizationService)
    {
        string? culture = localizationService.GetCultureFromRequest(context);

        if (!localizationService.IsSupportedCulture(culture))
        {
            culture = CultureConstants.DefaultCulture;
        }

        var cultureInfo = localizationService.GetCultureInfo(culture);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        context.Items["Culture"] = culture;

        await _next(context);
    }
}
```

### 5.9 خدمة التوطين (LocalizationService)

**الملف:** `Services/Localization/LocalizationService.cs`

**الواجهة:**
```csharp
public interface ILocalizationService
{
    string? GetCultureFromRequest(HttpContext context);
    void SetCultureCookie(HttpResponse response, string culture);
    void ClearCultureCookie(HttpResponse response);
    CultureInfo GetCultureInfo(string? culture);
    bool IsSupportedCulture(string? culture);
}
```

**التنفيذ:**
```csharp
public class LocalizationService : ILocalizationService
{
    public string? GetCultureFromRequest(HttpContext context)
    {
        return context.Request.Cookies
            .TryGetValue(CultureConstants.CultureCookieName, out var culture)
            ? culture
            : null;
    }

    public void SetCultureCookie(HttpResponse response, string culture)
    {
        response.Cookies.Append(
            CultureConstants.CultureCookieName,
            culture,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(365),
                IsEssential = true,
                SameSite = SameSiteMode.Strict
            });
    }

    public void ClearCultureCookie(HttpResponse response)
    {
        response.Cookies.Delete(CultureConstants.CultureCookieName);
    }

    public CultureInfo GetCultureInfo(string? culture)
    {
        try
        {
            return new CultureInfo(culture ?? CultureConstants.DefaultCulture);
        }
        catch
        {
            return new CultureInfo(CultureConstants.DefaultCulture);
        }
    }

    public bool IsSupportedCulture(string? culture)
    {
        return !string.IsNullOrEmpty(culture)
            && CultureConstants.SupportedCultures.Contains(culture);
    }
}
```

---

## 6. تعديلات البرنامج الرئيسي (Program.cs)

### الإضافات:
```csharp
// Add localization
var supportedCultures = new[]
{
    new CultureInfo("en-US"),
    new CultureInfo("pl-PL")
};
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Add localization service
builder.Services.AddScoped<ILocalizationService, LocalizationService>();

// Add authentication
builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/access-denied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// Build app
var app = builder.Build();

// Add middleware
app.UseRequestLocalization();
app.Use(async (context, next) =>
{
    var localizationService = context.RequestServices
        .GetRequiredService<ILocalizationService>();
    var culture = localizationService.GetCultureFromRequest(context)
        ?? "en-US";
    CultureInfo.CurrentCulture = new CultureInfo(culture);
    CultureInfo.CurrentUICulture = new CultureInfo(culture);
    await next.Invoke();
});

app.UseAuthentication();
app.UseAuthorization();

// Map routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

---

## 7. Controllers المطلوبة

### 7.1 HomeController

```csharp
public class HomeController : Controller
{
    private readonly ILocalizationService _localizationService;

    public HomeController(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    [HttpGet("/")]
    public IActionResult Index()
    {
        // If culture cookie exists, redirect to login
        var culture = _localizationService.GetCultureFromRequest(HttpContext);
        if (!string.IsNullOrEmpty(culture))
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }

    [HttpPost("/set-culture")]
    public IActionResult SetCulture(string culture)
    {
        if (!_localizationService.IsSupportedCulture(culture))
        {
            return BadRequest();
        }

        _localizationService.SetCultureCookie(Response, culture);
        return RedirectToAction("Login", "Account");
    }

    [HttpGet("/access-denied")]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
```

### 7.2 AccountController

```csharp
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILocalizationService _localizationService;

    [HttpGet("/account/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        var culture = _localizationService.GetCultureFromRequest(HttpContext);
        if (string.IsNullOrEmpty(culture))
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("/account/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var culture = _localizationService.GetCultureFromRequest(HttpContext);
        if (string.IsNullOrEmpty(culture))
        {
            return RedirectToAction("Index", "Home");
        }

        var user = await _userManager.FindByEmailAsync(model.Email!);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty,
                _localizer["InvalidCredentials"]);
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty,
                _localizer["AccountDisabled"]);
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password!,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty,
                _localizer["AccountLocked"]);
        }
        else
        {
            ModelState.AddModelError(string.Empty,
                _localizer["InvalidCredentials"]);
        }

        return View(model);
    }

    [HttpPost("/account/logout")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _localizationService.ClearCultureCookie(Response);
        return RedirectToAction("Index", "Home");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("SuperAdminDashboard", "Platform");
    }
}
```

### 7.3 PlatformController

```csharp
[Authorize(Policy = "PlatformAdministration")]
public class PlatformController : Controller
{
    private readonly EdulyticsDbContext _context;

    [HttpGet("/platform/dashboard")]
    public async Task<IActionResult> SuperAdminDashboard()
    {
        var schoolCount = await _context.Schools.CountAsync();
        var userCount = await _context.Users.CountAsync();

        return View(new PlatformDashboardViewModel
        {
            SchoolCount = schoolCount,
            UserCount = userCount
        });
    }
}
```

---

## 8. اختبارات (Tests)

### 8.1 LocalizationServiceTests

```csharp
public class LocalizationServiceTests
{
    [Fact]
    public void GetCultureFromRequest_ReturnsCultureFromCookie()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers.Add("Cookie",
            $"{CultureConstants.CultureCookieName}=pl-PL");
        var service = new LocalizationService();

        // Act
        var culture = service.GetCultureFromRequest(context);

        // Assert
        Assert.Equal("pl-PL", culture);
    }

    [Fact]
    public void IsSupportedCulture_ReturnsTrueForEnglish()
    {
        var service = new LocalizationService();
        Assert.True(service.IsSupportedCulture("en-US"));
    }

    [Fact]
    public void IsSupportedCulture_ReturnsTrueForPolish()
    {
        var service = new LocalizationService();
        Assert.True(service.IsSupportedCulture("pl-PL"));
    }

    [Fact]
    public void IsSupportedCulture_ReturnsFalseForUnsupported()
    {
        var service = new LocalizationService();
        Assert.False(service.IsSupportedCulture("fr-FR"));
    }
}
```

### 8.2 AuthenticationControllerTests

```csharp
public class AccountControllerTests : IAsyncLifetime
{
    private readonly TestServer _server;
    private readonly HttpClient _client;

    [Fact]
    public async Task Login_WithoutCultureCookie_RedirectsToHome()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/account/login");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task Login_WithValidCulture_DisplaysLoginForm()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Cookie",
            $"{CultureConstants.CultureCookieName}=en-US");

        // Act
        var response = await _client.GetAsync("/account/login");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Sign In", content);
    }
}
```

### 8.3 AuthorizationPolicyTests

```csharp
public class PlatformAdministrationPolicyTests
{
    [Fact]
    public async Task SuperAdmin_CanAccessPlatformDashboard()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Email = "admin@test.com",
            SchoolId = null
        };
        var context = new AuthorizationHandlerContext(
            new[] { new DummyRequirement() },
            new ClaimsPrincipal(),
            null);

        // Act & Assert
        // Verify that SuperAdmin role allows access
    }
}
```

---

## 9. متطلبات التصميم الاستجابي

### جميع الصفحات يجب أن تدعم:

#### Mobile (320px - 480px)
- Stacked layout
- Full-width inputs
- Centered content
- Large touch targets (44px minimum)

#### Tablet (768px - 1024px)
- Two-column where appropriate
- Balanced spacing

#### Desktop (1280px+)
- Three-column layout option
- Maximum content width
- Proper margins

### Tools المستخدمة:
- Bootstrap 5 أو Tailwind CSS
- CSS Grid & Flexbox
- Media queries: 320px, 375px, 480px, 768px, 1024px, 1280px, 1440px+

---

## 10. متطلبات الاختبار اليدوي (Manual Testing)

### 10.1 Language Selector Flow

**في المتصفح:**
```
1. GO TO: http://localhost:5000/
2. VERIFY: Language selector displays (two cards: Polski, English)
3. VERIFY: No extra text or explanations
4. CLICK: [ 🇵🇱 Polski ]
5. VERIFY: Redirected to /account/login
6. VERIFY: Page is entirely in Polish
7. VERIFY: Cookie "Edulytics.Culture" = "pl-PL"
8. GO TO: / (home)
9. VERIFY: Redirects directly to /account/login (cookie exists)
10. VERIFY: Login form still in Polish
11. CLICK: Logout
12. VERIFY: Redirected to /
13. VERIFY: Cookie "Edulytics.Culture" cleared
14. VERIFY: Language selector displays again
15. CLICK: [ 🇬🇧 English ]
16. VERIFY: Login form entirely in English
17. VERIFY: No Polish text visible
```

### 10.2 Login Validation

**في المتصفح:**
```
1. SELECT: English language
2. VERIFY: "Sign In" heading (English)
3. CLICK: Sign In without email
4. VERIFY: "Email is required" (English, localized)
5. CLICK: Sign In without password
6. VERIFY: "Password is required" (English, localized)
7. ENTER: Invalid email
8. VERIFY: "Invalid email" (English, localized)
9. ENTER: Valid email but wrong password
10. VERIFY: "Invalid email or password" (English, localized)
11. ENTER: Valid SuperAdmin email and password (from bootstrap)
12. VERIFY: Redirected to /platform/dashboard
13. VERIFY: Dashboard in English, greeting "Welcome to Edulytics"
```

### 10.3 Responsive Design

**في المتصفح:**
```
1. OPEN: DevTools (F12)
2. SELECT: Responsive Design Mode
3. TEST: 320px viewport
   - VERIFY: Language selector stacked
   - VERIFY: No horizontal scroll
   - VERIFY: Buttons clickable
4. TEST: 768px viewport
   - VERIFY: Two-column layout
5. TEST: 1440px viewport
   - VERIFY: Centered content
   - VERIFY: Proper margins
6. RESIZE: Dynamically
   - VERIFY: Smooth reflow
   - VERIFY: No text clipping
```

### 10.4 Localization Completeness

**في المتصفح (Polish):**
```
1. SELECT: Polski language
2. VERIFY: ALL text is Polish
   - Page title
   - Headings
   - Labels
   - Buttons
   - Placeholders
   - Error messages
3. VERIFY: NO English text visible (except maybe logo)
4. REPEAT: With English
```

### 10.5 Authorization

**في المتصفح:**
```
1. LOGIN: As SuperAdmin (SchoolId = null)
2. ACCESS: /platform/dashboard
3. VERIFY: Dashboard loads
4. LOGOUT: Sign out
5. TRY: Direct access to /platform/dashboard
6. VERIFY: Redirected to /account/login
```

---

## 11. معايير القبول (Acceptance Criteria)

### ✅ Language Selector
- [ ] صفحة واحدة فقط مع بطاقتين (Polski, English)
- [ ] لا توجد نصوص إضافية أو شروح
- [ ] ثنائية اللغة فقط (هذه الصفحة)
- [ ] Culture cookie مضبوط بعد الاختيار
- [ ] Redirect إلى login بعد الاختيار
- [ ] Responsive: 320px - 1440px+

### ✅ Login Page
- [ ] بلغة واحدة فقط (بناءً على culture cookie)
- [ ] Validates email format
- [ ] Validates password required
- [ ] Shows localized error messages
- [ ] No public registration link
- [ ] Anti-forgery token
- [ ] Responsive: 320px - 1440px+

### ✅ Logout
- [ ] Clears authentication
- [ ] Clears culture cookie
- [ ] Redirects to language selector
- [ ] Subsequent access to /login redirects to /

### ✅ Protected Dashboard
- [ ] Requires authentication
- [ ] Requires SuperAdmin role
- [ ] Displays in user's language
- [ ] Shows school/user counts
- [ ] Logout link
- [ ] Responsive: 320px - 1440px+

### ✅ Access Denied
- [ ] Localized message
- [ ] Link to home or previous page
- [ ] Responsive: 320px - 1440px+

### ✅ Localization Resources
- [ ] Common.resx (en-US & pl-PL)
- [ ] Account.resx (en-US & pl-PL)
- [ ] Home.resx (en-US & pl-PL)
- [ ] Platform.resx (en-US & pl-PL)
- [ ] Validation.resx (en-US & pl-PL)
- [ ] No missing translations
- [ ] No English fallback on Polish pages

### ✅ Authorization
- [ ] PlatformAdministration policy
- [ ] SuperAdmin-only dashboard
- [ ] Non-authenticated users redirected to login
- [ ] Non-admin users get access denied

### ✅ Tests
- [ ] Localization service tests: 5+ passing
- [ ] Account controller tests: 5+ passing
- [ ] Authorization policy tests: 3+ passing
- [ ] All tests pass: `dotnet test`

### ✅ Build & Quality
- [ ] `dotnet build` succeeds
- [ ] No compiler errors
- [ ] No security warnings
- [ ] `git diff --check` passes
- [ ] No vulnerable packages

### ✅ Manual UI Verification
- [ ] Language selector displays correctly (mobile & desktop)
- [ ] Polish login has NO English
- [ ] English login has NO Polish
- [ ] All validation errors localized
- [ ] Dashboard loads for SuperAdmin
- [ ] Logout works correctly
- [ ] Responsive behavior verified at all breakpoints

---

## 12. المتطلبات غير الوظيفية

### 12.1 الأداء
- Localization resources cached in memory
- Culture cookie cached on browser (365 days)
- Database queries minimal on login

### 12.2 الأمان
- No credentials logged
- CSRF protection on all POST
- Lockout after failed attempts
- Generic error messages (no user enumeration)
- Secure cookies (HttpOnly, Secure, SameSite)

### 12.3 الوصولية
- ARIA labels on all form inputs
- Screen reader support for error messages
- Keyboard navigation for all interactive elements
- Sufficient color contrast

---

## 13. دليل التنفيذ (Implementation Guide)

### المرحلة 1: البنية الأساسية (Foundation)
1. إنشء CultureConstants في Core
2. إنشء ILocalizationService في Services
3. إنشء LocalizationService في Services
4. إضافة Localization إلى Program.cs

### المرحلة 2: الـ Controllers والـ Views
1. تعديل HomeController
2. إنشء AccountController
3. إنشء PlatformController
4. إنشء Views (Index, Login, Logout, AccessDenied, Dashboard)

### المرحلة 3: الموارد المترجمة
1. إنشء .resx files للـ en-US
2. إنشء .resx files للـ pl-PL
3. التحقق من عدم وجود missing translations

### المرحلة 4: الاختبارات
1. كتابة localization tests
2. كتابة authentication tests
3. كتابة authorization tests
4. تشغيل جميع الاختبارات

### المرحلة 5: التحقق
1. بناء المشروع
2. تشغيل الاختبارات
3. التحقق اليدوي من واجهة المستخدم
4. تحقق من الاستجابة (responsiveness)

---

## 14. الملاحظات والنقاط المهمة

### ✅ ما يجب فعله
- Implement secure login without public registration
- Full localization support (en-US, pl-PL)
- Responsive design for all breakpoints
- Proper authorization policies
- Comprehensive testing
- Manual UI verification

### ❌ ما يجب تجنبه
- Public registration (Phase 04+)
- School management features (Phase 04)
- Database schema changes (not needed)
- Mixing languages on same page
- Hardcoded strings
- Client-side authorization
- Storing culture in anything other than cookie

### ⚠️ نقاط التركيز
- Language selector must be ONLY two cards
- After selection, NO fallback to English
- Culture cookie must persist 365 days
- Logout must clear culture cookie
- SuperAdmin-only dashboard for testing authorization
- All error messages must be localized

---

## 15. المراجع

**الملفات المرتبطة:**
- [Edulytics Full Specification](./Edulytics_From_Scratch_Full_Cursor_Spec.md)
- Phase 02 Implementation: f91dbaf

**الفصول ذات الصلة:**
- Section 8: User Roles
- Section 9: Tenant Model
- Section 10: Language and Localization
- Section 11: Responsive UI Requirements
- Section 19: Testing Strategy
- Section 20.3: Phase 03 Details

---

## الخلاصة

المرحلة 03 هي خطوة محورية في بناء Edulytics. تؤسس لأساس محكم للمصادقة والتوطين والتفويض.

**الأهداف الرئيسية:**
1. بناء تدفق لغة آمن ومرن
2. تنفيذ مصادقة آمنة بدون تسجيل عام
3. دعم اللغات الإنجليزية والبولندية بالكامل
4. واجهة مستخدم استجابية على جميع الأجهزة
5. سياسات تفويض قوية

**بعد إكمال المرحلة 03:**
- ننتقل إلى **Phase 04: School Management**
- سنبني عليها: إنشاء المدارس، التحرير، إدارة حالة المدرسة

**الحالة الحالية:**
- ✅ جاهز للموافقة
- ⏳ بانتظار الموافقة قبل البدء بالكود

---

**آخر تحديث:** 12 أغسطس 2026
**الحالة:** بانتظار الموافقة من المالك
