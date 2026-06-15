using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Data;
using SweetCakeShop.Services;
using Stripe;

namespace SweetCakeShop
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // Configure Identity cookie so "Remember me" creates a persistent cookie
            builder.Services.ConfigureApplicationCookie(options =>
            {
                // How long the persistent cookie (when RememberMe = true) will persist
                // Set to a very long duration (100 years) to effectively remove a practical expiration.
                // Note: keeping authentication cookies without reasonable expiry is a security and privacy risk.
                options.ExpireTimeSpan = TimeSpan.FromDays(36500);
                options.SlidingExpiration = true;

                // Useful paths (adjust if your identity routes differ)
                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";

                options.Cookie.HttpOnly = true;
                // options.Cookie.IsEssential = true; // uncomment if you want cookie to be considered essential for GDPR scenarios
            });

            builder.Services.AddControllersWithViews();

            // Session and cart registration
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<CartService>();
            builder.Services.AddScoped<OrderService>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
            builder.Services.AddScoped<IRevenueService, RevenueService>();
            builder.Services.AddScoped<IExportService, ExportService>();
            
            // New ASOS Services
            builder.Services.AddScoped<IVietnameseNormalizerService, VietnameseNormalizerService>();
            builder.Services.AddScoped<ISmartSearchService, SmartSearchService>();
            builder.Services.AddScoped<IDbCartService, DbCartService>();
            builder.Services.AddScoped<IReviewService, SweetCakeShop.Services.ReviewService>();
            builder.Services.AddScoped<ICouponService, SweetCakeShop.Services.CouponService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IChatPageContextService, ChatPageContextService>();
            builder.Services.AddScoped<IChatSessionService, ChatSessionService>();
            builder.Services.AddScoped<IIntentRecognitionService, IntentRecognitionService>();
            builder.Services.AddScoped<IChatQueryExecutor, ChatQueryExecutor>();
            builder.Services.AddScoped<IAiResponseComposer, AiResponseComposer>();
            builder.Services.AddScoped<IIntentContextService, IntentContextService>();
            builder.Services.AddScoped<IAiBusinessDataService, AiBusinessDataService>();
            builder.Services.AddScoped<ICustomerDataService, CustomerDataService>();
            builder.Services.AddScoped<ITopicFilterService, TopicFilterService>();
            builder.Services.AddScoped<IChatSecurityService, ChatSecurityService>();
            builder.Services.AddScoped<IPromptBuilderService, PromptBuilderService>();

            builder.Services.AddScoped<SweetCakeShop.Services.AI.Rag.IQueryPlannerService, SweetCakeShop.Services.AI.Rag.QueryPlannerService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.Rag.IRagRetrieverService, SweetCakeShop.Services.AI.Rag.RagRetrieverService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.Rag.IConsultantResponseService, SweetCakeShop.Services.AI.Rag.ConsultantResponseService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.IWebsiteKnowledgeService, SweetCakeShop.Services.AI.WebsiteKnowledgeService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.IChatEnrichmentService, SweetCakeShop.Services.AI.ChatEnrichmentService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.ISemanticFunctionRouterService, SweetCakeShop.Services.AI.SemanticFunctionRouterService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.IAiFunctionExecutorService, SweetCakeShop.Services.AI.AiFunctionExecutorService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.IProductAnalyticsService, SweetCakeShop.Services.AI.ProductAnalyticsService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.IRevenueAnalyticsService, SweetCakeShop.Services.AI.RevenueAnalyticsService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.IInventoryService, SweetCakeShop.Services.AI.InventoryService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.IOrderAnalyticsService, SweetCakeShop.Services.AI.OrderAnalyticsService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.IRecommendationService, SweetCakeShop.Services.AI.RecommendationService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.IConversationMemoryService, SweetCakeShop.Services.AI.ConversationMemoryService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.IAIContextBuilderService, SweetCakeShop.Services.AI.AIContextBuilderService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.IStructuredAiResponseService, SweetCakeShop.Services.AI.StructuredAiResponseService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.ILlmCompletionService, SweetCakeShop.Services.AI.LlmCompletionService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.IChatRoleGuardService, SweetCakeShop.Services.AI.ChatRoleGuardService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.ICartIntentService, SweetCakeShop.Services.AI.CartIntentService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.IOrderHandoffService, SweetCakeShop.Services.AI.OrderHandoffService>();
            builder.Services.AddScoped<SweetCakeShop.Services.AI.IStoreKnowledgeService, SweetCakeShop.Services.AI.StoreKnowledgeService>();
            builder.Services.AddScoped<IAiChatService, AiChatService>();

            builder.Services.AddScoped<SweetCakeShop.Services.Chat.IChatIdentityService, SweetCakeShop.Services.Chat.ChatIdentityService>();
            builder.Services.AddScoped<SweetCakeShop.Services.Chat.IChatHistoryService, SweetCakeShop.Services.Chat.ChatHistoryService>();
            builder.Services.AddScoped<SweetCakeShop.Services.Chat.IChatTokenMergeService, SweetCakeShop.Services.Chat.ChatTokenMergeService>();
            builder.Services.AddScoped<SweetCakeShop.Services.Chat.IProductCatalogForAiService, SweetCakeShop.Services.Chat.ProductCatalogForAiService>();
            builder.Services.AddScoped<SweetCakeShop.Services.Chat.IProductIntentResolver, SweetCakeShop.Services.Chat.ProductIntentResolver>();
            builder.Services.AddScoped<SweetCakeShop.Services.Chat.Gemini.IGeminiChatApiService, SweetCakeShop.Services.Chat.Gemini.GeminiChatApiService>();
            builder.Services.AddScoped<SweetCakeShop.Services.Chat.OpenAi.IOpenAiChatApiService, SweetCakeShop.Services.Chat.OpenAi.OpenAiChatApiService>();
            builder.Services.AddScoped<SweetCakeShop.Services.Chat.ICustomerProductChatService, SweetCakeShop.Services.Chat.CustomerProductChatService>();

            builder.Services.AddHttpClient<IPaymentService, PaymentService>();
            builder.Services.AddHttpClient("OpenAI", c => c.Timeout = TimeSpan.FromSeconds(60));
            builder.Services.AddHttpClient("Gemini", c => c.Timeout = TimeSpan.FromSeconds(60));

            // Configure Stripe API key from configuration or environment
            // Put your secret key into environment variable or user secrets: "Stripe:SecretKey"
            var stripeSecret = builder.Configuration["Stripe:SecretKey"]
                               ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
            if (!string.IsNullOrWhiteSpace(stripeSecret))
            {
                StripeConfiguration.ApiKey = stripeSecret;
            }

            var app = builder.Build();

            // Seed database với dữ liệu mẫu
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    Console.WriteLine("Đang khởi tạo seed data...");
                    SeedData.Initialize(services);
                    IdentitySeed.SeedAdminAsync(services).GetAwaiter().GetResult();
                    Console.WriteLine("Seed data hoàn tất!");
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "LỖI KHI SEED DỮ LIỆU: {Message}", ex.Message);
                    throw;
                }

            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();               

            app.UseSession(); // <- enable session

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllers();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();
            app.MapRazorPages().WithStaticAssets();

            app.Run();
        }
    }
}
