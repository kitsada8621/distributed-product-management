using Catalog.Api.Common;
using Catalog.Api.Endpoints;
using Catalog.Api.Workers;
using Catalog.Application.Categories;
using Catalog.Application.Common;
using Catalog.Application.Messaging;
using Catalog.Application.Products;
using Catalog.Application.Reservations;
using Catalog.Application.Stock;
using Catalog.Infrastructure.Kafka;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddOpenApi();
services.AddExceptionHandler<CatalogExceptionHandler>();
services.AddExceptionHandler<UnhandledExceptionHandler>();
// Outside Development a malformed body would otherwise get a bare 400 with no envelope (API-SPEC 2.4).
services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
services.AddJwtBearerAuth();

// Settings are read when first resolved (not here) so test and container overrides always apply.
BindSection<JwtOptions>("Jwt");
BindSection<KafkaOptions>("Kafka");

services.AddSingleton(TimeProvider.System);
services.AddDbContext<CatalogDbContext>((serviceProvider, options) => options
    .UseSqlite(serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Catalog"))
    .AddInterceptors(new SqlitePragmaInterceptor()));
services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<CatalogDbContext>());

services.AddScoped<ICategoryRepository, CategoryRepository>();
services.AddScoped<IProductRepository, ProductRepository>();
services.AddScoped<IOutboxRepository, OutboxRepository>();
services.AddScoped<IInboxRepository, InboxRepository>();
services.AddScoped<IReservationRepository, ReservationRepository>();
services.AddScoped<IStockRepository, StockRepository>();
services.AddScoped<ITransactionFactory>(serviceProvider => serviceProvider.GetRequiredService<CatalogDbContext>());

services.AddScoped<ListCategories>();
services.AddScoped<CreateCategory>();
services.AddScoped<UpdateCategory>();
services.AddScoped<DeleteCategory>();
services.AddScoped<ListProducts>();
services.AddScoped<GetProduct>();
services.AddScoped<CreateProduct>();
services.AddScoped<UpdateProduct>();
services.AddScoped<DeleteProduct>();
services.AddScoped<SetProductStock>();
services.AddScoped<ReserveStock>();
services.AddScoped<CommitReservation>();
services.AddScoped<ReleaseReservation>();
services.AddScoped<SendToDeadLetter>();

services.AddSingleton<ConsumedMessageProcessor>();
// Read here, so tests turn Kafka off with UseSetting (host settings are visible this early).
if (builder.Configuration.GetValue("Kafka:Enabled", true))
{
    services.AddHostedService<OrderEventsConsumerWorker>();
    services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
    services.AddScoped<PublishPendingEvents>();
    services.AddHostedService<OutboxRelayWorker>();
}

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
// Empty handler: our IExceptionHandler writes the envelope, so no re-execution path is needed.
app.UseExceptionHandler(_ => { });
// An unmatched route (or a failed route constraint) gets an empty 404; API-SPEC 2.4 wants the envelope on every response.
app.UseStatusCodePages(async statusContext =>
{
    if (statusContext.HttpContext.Response.StatusCode == StatusCodes.Status404NotFound)
    {
        var response = ApiResponse.Fail(StatusCodes.Status404NotFound, "NOT_FOUND", "Resource not found");
        await statusContext.HttpContext.Response.WriteAsJsonAsync(response);
    }
});
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthEndpoints();
app.MapCategoryEndpoints();
app.MapProductEndpoints();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await database.Database.MigrateAsync();
}

app.Run();

void BindSection<T>(string section) where T : class, new()
{
    services.AddSingleton(serviceProvider =>
        serviceProvider.GetRequiredService<IConfiguration>().GetSection(section).Get<T>() ?? new T());
}

public partial class Program;
