using CoffeeMachine.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "CoffeeMachine", Version = "v1" });
    o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "CoffeeMachine.xml"));
});

builder.Services.AddSingleton(TimeProvider.System);
// This is a singleton as it contains the count of brews
builder.Services.AddSingleton<ICoffeeService, CoffeeService>();

builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddOptions<WeatherServiceOptions>()
    .Bind(builder.Configuration.GetSection("WeatherService"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// </summary>
public partial class Program { }
