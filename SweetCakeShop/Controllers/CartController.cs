using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Constants;
using SweetCakeShop.Data;
using SweetCakeShop.Services;
using SweetCakeShop.Models;
using SweetCakeShop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Stripe.Checkout;
using Stripe;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace SweetCakeShop.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _sessionCartService; // Kept for anonymous users
        private readonly IDbCartService _dbCartService;   // New for authenticated users
        private readonly OrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly ICouponService _couponService;
        private readonly INotificationService _notificationService;

        public CartController(
            ApplicationDbContext context, 
            CartService sessionCartService, 
            IDbCartService dbCartService,
            OrderService orderService, 
            IPaymentService paymentService,
            ICouponService couponService,
            INotificationService notificationService)
        {
            _context = context;
            _sessionCartService = sessionCartService;
            _dbCartService = dbCartService;
            _orderService = orderService;  
            _paymentService = paymentService;
            _couponService = couponService;
            _notificationService = notificationService;
        }

        private async Task<CartViewModel> GetCurrentCartAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                return await _dbCartService.GetCartAsync(userId);
            }
            return _sessionCartService.GetCart();
        }

        public async Task<IActionResult> Index()
        {
            var cart = await GetCurrentCartAsync();
            
            // Restore coupon info from session if present
            var couponJson = HttpContext.Session.GetString("AppliedCoupon");
            if (!string.IsNullOrEmpty(couponJson))
            {
                var appliedCoupon = JsonConvert.DeserializeObject<CouponValidationResult>(couponJson);
                if (appliedCoupon != null && appliedCoupon.IsValid)
                {
                    // Revalidate coupon against current cart subtotal
                    var subtotal = cart.Items.Sum(i => i.Price * i.Quantity);
                    var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier)! : "";
                    
                    var revalidation = await _couponService.ValidateAsync(appliedCoupon.Coupon!.Code, subtotal, userId, cart.Items);
                    if (revalidation.IsValid)
                    {
                        cart.CouponCode = revalidation.Coupon?.Code;
                        cart.DiscountAmount = revalidation.DiscountAmount;
                    }
                    else
                    {
                        HttpContext.Session.Remove("AppliedCoupon");
                        TempData["Warning"] = "Mã giảm giá đã bị loại bỏ vì giỏ hàng không còn đủ điều kiện: " + revalidation.Message;
                    }
                }
            }

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int id, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            if (User.Identity?.IsAuthenticated == true)
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                await _dbCartService.AddToCartAsync(userId, id, quantity);
                await _notificationService.CreateAsync(userId, null, "add_to_cart", "Đã thêm vào giỏ", $"{product.ProductName} đã thêm vào giỏ hàng!");
            }
            else
            {
                _sessionCartService.AddToCart(product, quantity);
                await _notificationService.CreateAsync(null, HttpContext.Session.Id, "add_to_cart", "Đã thêm vào giỏ", $"{product.ProductName} đã thêm vào giỏ hàng!");
            }

            return Json(new { success = true, message = $"{product.ProductName} đã thêm vào giỏ hàng!" });
        }

        [HttpPost]
        public async Task<IActionResult> Update(int productId, int quantity)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                await _dbCartService.UpdateQuantityAsync(userId, productId, quantity);
            }
            else
            {
                _sessionCartService.UpdateQuantity(productId, quantity);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int productId)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                await _dbCartService.RemoveFromCartAsync(userId, productId);
            }
            else
            {
                _sessionCartService.RemoveFromCart(productId);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Count()
        {
            int count;
            if (User.Identity?.IsAuthenticated == true)
            {
                string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                count = await _dbCartService.GetItemCountAsync(userId);
            }
            else
            {
                count = _sessionCartService.GetCart().Items.Sum(i => i.Quantity);
            }
            return Json(new { count });
        }

        // Apply coupon to cart
        [HttpPost]
        public async Task<IActionResult> ApplyCoupon(string code)
        {
            var cart = await GetCurrentCartAsync();
            var subtotal = cart.Items.Sum(i => i.Price * i.Quantity);
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier)! : "";

            var result = await _couponService.ValidateAsync(code, subtotal, userId, cart.Items);

            if (result.IsValid)
            {
                HttpContext.Session.SetString("AppliedCoupon", JsonConvert.SerializeObject(result));
                TempData["Success"] = result.Message;
                await _notificationService.CreateAsync(
                    User.Identity?.IsAuthenticated == true ? userId : null,
                    User.Identity?.IsAuthenticated == true ? null : HttpContext.Session.Id,
                    "discount_applied", "Áp dụng mã thành công", $"Giảm {result.DiscountAmount:N0}₫ cho đơn hàng.");
                return Json(new { success = true, message = result.Message });
            }
            else
            {
                return Json(new { success = false, message = result.Message });
            }
        }

        // Remove applied coupon
        [HttpPost]
        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.Remove("AppliedCoupon");
            TempData["Success"] = "Đã bỏ mã giảm giá.";
            return Json(new { success = true });
        }

        // Show checkout with shipping form
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = await GetCurrentCartAsync();
            if (!cart.Items.Any())
                return RedirectToAction("Index");

            // Require login to proceed to checkout
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["LoginMessage"] = "Bạn phải tiến hành đăng nhập để tiếp tục mua sản phẩm";
                var returnUrl = Url.Action("Checkout", "Cart");
                return Redirect($"/Identity/Account/Login?returnUrl={System.Net.WebUtility.UrlEncode(returnUrl ?? "/")}");
            }

            var model = new CheckoutViewModel();

            // Prefill when logged in
            model.CustomerEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            model.CustomerName = User.Identity?.Name ?? string.Empty;
            
            var couponJson = HttpContext.Session.GetString("AppliedCoupon");
            if (!string.IsNullOrEmpty(couponJson))
            {
                var appliedCoupon = JsonConvert.DeserializeObject<CouponValidationResult>(couponJson);
                if (appliedCoupon != null && appliedCoupon.IsValid)
                {
                    cart.CouponCode = appliedCoupon.Coupon?.Code;
                    cart.DiscountAmount = appliedCoupon.DiscountAmount;
                }
            }
            
            ViewData["Cart"] = cart;

            return View(model);
        }

        // Accept checkout from guests and authenticated users
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutConfirm(CheckoutViewModel checkout)
        {
            var cart = await GetCurrentCartAsync();
            if (!cart.Items.Any())
                return RedirectToAction("Index");

            string? userId = null;
            if (User.Identity?.IsAuthenticated == true)
                userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Re-validate coupon right before checkout
            CouponValidationResult? appliedCoupon = null;
            var couponJson = HttpContext.Session.GetString("AppliedCoupon");
            if (!string.IsNullOrEmpty(couponJson))
            {
                var cachedCoupon = JsonConvert.DeserializeObject<CouponValidationResult>(couponJson);
                if (cachedCoupon != null && cachedCoupon.IsValid && cachedCoupon.Coupon != null)
                {
                    var subtotal = cart.Items.Sum(i => i.Price * i.Quantity);
                    var revalidation = await _couponService.ValidateAsync(cachedCoupon.Coupon.Code, subtotal, userId ?? "", cart.Items);
                    if (revalidation.IsValid)
                    {
                        appliedCoupon = revalidation;
                    }
                }
            }

            var order = await _orderService.CreateOrderAsync(cart, checkout, userId);

            // Apply coupon to order
            if (appliedCoupon != null && appliedCoupon.Coupon != null)
            {
                order.CouponId = appliedCoupon.Coupon.CouponId;
                order.CouponCode = appliedCoupon.Coupon.Code;
                order.DiscountAmount = appliedCoupon.DiscountAmount;
                order.TotalPrice = Math.Max(0, order.TotalPrice - appliedCoupon.DiscountAmount);
                
                await _context.SaveChangesAsync();
                await _couponService.RecordUsageAsync(appliedCoupon.Coupon.CouponId, userId ?? "", order.OrderId, appliedCoupon.DiscountAmount);
            }

            // Clear cart
            if (User.Identity?.IsAuthenticated == true)
            {
                await _dbCartService.ClearCartAsync(userId!);
            }
            else
            {
                _sessionCartService.ClearCart();
            }
            
            HttpContext.Session.Remove("AppliedCoupon");

            // After creating order, redirect to Payment selection page
            return RedirectToAction("Payment", new { orderId = order.OrderId });
        }

        // Payment selection & result page
        [HttpGet]
        public async Task<IActionResult> Payment(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            var model = new PaymentViewModel
            {
                Order = order
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int orderId, string method)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            if (method == "COD")
            {
                OrderStatuses.ApplyConfirmed(order);
                await _context.SaveChangesAsync();

                return RedirectToAction("Success", new { orderId = order.OrderId });
            }
            else if (method == "Online")
            {
                // Build success/cancel URLs that Stripe will redirect to.
                // Use Stripe's placeholder {CHECKOUT_SESSION_ID} so we can verify the session on return.
                var baseSuccessUrl = Url.Action("Success", "Cart", new { orderId = order.OrderId }, Request.Scheme) ?? string.Empty;
                var successUrl = baseSuccessUrl + (baseSuccessUrl.Contains("?") ? "&session_id={CHECKOUT_SESSION_ID}" : "?session_id={CHECKOUT_SESSION_ID}");
                var cancelUrl = Url.Action("Payment", "Cart", new { orderId = order.OrderId }, Request.Scheme) ?? string.Empty;

                // Create Stripe Checkout Session via service (provides session.Url)
                var payment = await _paymentService.CreatePaymentAsync(order, successUrl, cancelUrl);

                // Mark order awaiting online payment
                order.Status = "AwaitingPayment";
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(payment.PaymentUrl))
                {
                    return Redirect(payment.PaymentUrl); // send browser to Stripe Checkout
                }

                // fallback: show Payment view with bank-transfer info
                var model = new PaymentViewModel
                {
                    Order = order,
                    PaymentResult = payment
                };

                return View("Payment", model);
            }

            // unexpected method
            TempData["Error"] = "Phương thức thanh toán không hợp lệ.";
            return RedirectToAction("Payment", new { orderId = order.OrderId });
        }

        // Internal page that displays your payment image/QR code in the middle
        [HttpGet]
        public async Task<IActionResult> OnlinePayment(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return NotFound();

            var model = new PaymentViewModel
            {
                Order = order
            };

            return View(model); // Views/Cart/OnlinePayment.cshtml
        }

        // User clicks "I have paid" on internal page to confirm manually
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOnlinePayment(int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                return NotFound();

            // mark as awaiting manual confirmation (you can change to Confirmed if you prefer)
            order.Status = "AwaitingConfirmation";
            await _context.SaveChangesAsync();

            return RedirectToAction("Success", new { orderId = order.OrderId });
        }

        // Success: can be reached from Stripe redirect (contains session_id) or internal flows.
        [HttpGet]
        public async Task<IActionResult> Success(int orderId, string? session_id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                return NotFound();

            // If Stripe returned a session_id, verify payment status server-side (recommended)
            if (!string.IsNullOrEmpty(session_id))
            {
                try
                {
                    var sessionService = new SessionService();
                    var session = await sessionService.GetAsync(session_id);

                    if (session != null && session.PaymentStatus == "paid")
                    {
                        OrderStatuses.ApplyConfirmed(order);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // payment not confirmed yet — keep status or mark accordingly
                        order.Status = "PaymentFailed";
                        await _context.SaveChangesAsync();
                    }
                }
                catch
                {
                    // if verification fails, don't throw to user; keep current order status
                }
            }

            ViewData["OrderId"] = orderId;
            return View();
        }
    }
}