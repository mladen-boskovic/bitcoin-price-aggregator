using Application.Aggregation;
using Application.Behaviours;
using Application.Providers;
using Application.Repository;
using Application.UseCases.Queries.GetAggregatedPrice;
using DataAccess;
using FluentValidation;
using Implementation.Aggregation;
using Implementation.Providers;
using Implementation.Repository;
using Implementation.UseCases.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000;
});

builder.Services.AddScoped<IPriceProvider, BitstampPriceProvider>();

builder.Services.AddScoped<IPriceProvider, BitfinexPriceProvider>();

builder.Services.AddScoped<IPriceRepository, PriceRepository>();

builder.Services.AddScoped<IAggregationStrategy, AverageAggregationStrategy>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetAggregatedPriceQueryHandler).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(GetAggregatedPriceQueryValidator).Assembly);
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

var bitstampUrl = builder.Configuration["PriceProviders:Bitstamp:BaseUrl"]
    ?? throw new InvalidOperationException("PriceProviders:Bitstamp:BaseUrl is not configured.");

var bitfinexUrl = builder.Configuration["PriceProviders:Bitfinex:BaseUrl"]
    ?? throw new InvalidOperationException("PriceProviders:Bitfinex:BaseUrl is not configured.");

builder.Services.AddHttpClient("Bitstamp", client =>
{
    client.BaseAddress = new Uri(bitstampUrl);
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("Bitfinex", client =>
{
    client.BaseAddress = new Uri(bitfinexUrl);
})
.AddStandardResilienceHandler();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var blazorOrigin = builder.Configuration["AllowedOrigins:BlazorFrontend"] ?? throw new InvalidOperationException("AllowedOrigins:BlazorFrontend is not configured.");
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorFrontend", policy => policy.WithOrigins(blazorOrigin).AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseMiddleware<API.Middleware.GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("BlazorFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
