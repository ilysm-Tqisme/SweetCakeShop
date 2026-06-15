namespace SweetCakeShop.Services.AI
{
    public interface IWebsiteKnowledgeService
    {
        string GetCustomerCapabilities();
        string GetAdminCapabilities();
    }

    /// <summary>Describes real SweetCakeShop features so the AI guides users like staff.</summary>
    public class WebsiteKnowledgeService : IWebsiteKnowledgeService
    {
        public string GetCustomerCapabilities() => """
            SWEETCAKESHOP CUSTOMER FEATURES (use to guide users):
            - Browse products: /Products/IndexPro — search, filter by category, sort by price.
            - Product details: /Products/Details/{id} — description, similar cakes in same category.
            - Cart (session): /Cart — view items, update quantity, remove.
            - Add to cart: POST /Cart/Add with product id (AI can add via AddToCart function).
            - Checkout: /Cart/Checkout — requires login; guest must register/login first.
            - Order flow: Cart → Checkout (shipping info) → Payment page → COD or Stripe Online.
            - Payment: COD confirms immediately; Online redirects to Stripe Checkout.
            - Order success: /Cart/Success/{orderId}.
            - Account: /Identity/Account/Login, Register; logged-in users see orders in Account/Manage/Orders.
            - Contact: /Home/Contact.
            - Categories managed by admin; products have Category, Name, Price, Description, Image.
            - Promotions: seasonal on website; hotline 1900-SWEET for current offers.
            - Delivery: inner-city shipping, 2-3 business days after order confirmation.
            """;

        public string GetAdminCapabilities() => """
            SWEETCAKESHOP ADMIN FEATURES:
            - Dashboard: /AdminDashboard — revenue charts (ConfirmedAt, Confirmed/Completed orders), filters, PDF/Excel export.
            - Orders: /Admin/Orders — update status Pending/Confirmed/Shipped/Delivered/Cancelled.
            - Products & stock: /Admin/Products.
            - Categories: /Admin/Categories.
            - Ingredients & recipes: /Admin/Ingredients.
            - Top selling report: /Admin/TopSellingProducts.
            - Revenue uses ConfirmedAt on Confirmed/Completed orders only.
            """;
    }
}
