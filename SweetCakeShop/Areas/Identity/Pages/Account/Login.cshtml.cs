// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SweetCakeShop.Services.Chat;

namespace SweetCakeShop.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(
            string returnUrl = null,
            [FromServices] IChatTokenMergeService? chatMerge = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    // Re-issue a persistent cookie explicitly to ensure cookie includes Expires when RememberMe is checked.
                    // PasswordSignInAsync normally does this, but re-signing ensures persistence in environments where the initial cookie
                    // might be treated as session-only.
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        var authProps = new AuthenticationProperties
                        {
                            IsPersistent = Input.RememberMe,
                            ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddYears(100) : (DateTimeOffset?)null
                        };
                        await _signInManager.SignInAsync(user, authProps);

                        if (chatMerge != null)
                            await chatMerge.MergeOnLoginAsync(user.Id);

                        var dbCartService = HttpContext.RequestServices.GetService(typeof(SweetCakeShop.Services.IDbCartService)) as SweetCakeShop.Services.IDbCartService;
                        var notifService = HttpContext.RequestServices.GetService(typeof(SweetCakeShop.Services.INotificationService)) as SweetCakeShop.Services.INotificationService;
                        var sessionCartService = HttpContext.RequestServices.GetService(typeof(SweetCakeShop.Services.CartService)) as SweetCakeShop.Services.CartService;
                        
                        if (dbCartService != null && notifService != null && sessionCartService != null)
                        {
                            var sessionCart = sessionCartService.GetCart();
                            if (sessionCart.Items.Count > 0)
                            {
                                int mergedItems = sessionCart.Items.Count;
                                await dbCartService.MergeSessionCartAsync(user.Id, sessionCart);
                                sessionCartService.ClearCart();

                                await notifService.CreateAsync(
                                    user.Id,
                                    null,
                                    "CartMerged",
                                    "Giỏ hàng đã được đồng bộ",
                                    $"Chúng tôi đã tìm thấy {mergedItems} sản phẩm bạn chọn trước khi đăng nhập và đã thêm vào giỏ hàng của bạn.",
                                    null
                                );
                            }
                        }
                    }

                    // THÊM DÒNG NÀY ĐỂ KÍCH HOẠT THÔNG BÁO
                    TempData["LoginSuccess"] = "True";

                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
