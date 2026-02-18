using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using GamesFinder.Orchestrator.Repositories;
using GamesFinder.Orchestrator.Domain.Enums;
using GamesFinder.Orchestrator.Domain.Classes;
using GamesFinder.Domain.Interfaces.Repositories;
using GamesFinder.Orchestrator.Repositories.Repositories;
using GamesFinder.Orchestrator.Publisher.RabbitMQ;
using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using GamesFinder.Orchestrator.Publisher.Redis;
using StackExchange.Redis;
using GamesFinder.Orchestrator.Publisher;
using GamesFinder.Domain.Enums;
using GamesFinder.Orchestrator.Consumers;
using GamesFinder.Orchestrator.Services.DomainServices;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;
using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using GamesFinder.Orchestrator.Services.ApplicationServices;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenApi();

builder.Services.Configure<MongoDBSettings>(
	builder.Configuration.GetSection("MongoDb"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
	var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
	return new MongoClient(settings.ConnectionString);
});

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
	var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
	var client = sp.GetRequiredService<IMongoClient>();
	return client.GetDatabase(settings.Database);
});

var twp = new TokenValidationParameters
{
	RoleClaimType = ClaimTypes.Role,
	NameClaimType = ClaimTypes.Name,
	ValidateIssuer = true,
	ValidateAudience = true,
	ValidateIssuerSigningKey = true,
	ValidIssuer = builder.Configuration.GetValue<string>("Security:JWTIssuer"),
	ValidAudience = builder.Configuration.GetValue<string>("Security:JWTAudience"),
	IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
			builder.Configuration.GetValue<string>("Security:JWTSecret")!
	)),
	ClockSkew = TimeSpan.FromMinutes(1)
};

var requireJwt = builder.Configuration.GetValue<bool>("Security:RequireJWT");

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options => {options.TokenValidationParameters = twp;});
builder.Services.AddAuthorization(options =>
{
	if (!requireJwt)
	{
		options.AddPolicy("DevPolicy", policy => policy.RequireAssertion(_ => true));
	}
});


builder.Services.AddSingleton(new SteamOptions(
	domainName: builder.Configuration.GetValue<string>("SteamApi:Name")!,
	apiKey: builder.Configuration.GetValue<string>("SteamApi:Key")!
));
builder.Services.AddSingleton(new WorkersOptions(
	instantGamingWorkerCount: builder.Configuration.GetValue<int>("Workers:InstantGamingWorkerCount")!,
	instantGamingSkipFirstIds: builder.Configuration.GetValue<int>("Workers:InstantGamingSkipFirstIds")!
));

builder.Services.AddSingleton(new RabbitMqConfig(
	hostName: builder.Configuration.GetValue<string>("RabbitMQ:HostName")!,
	port: builder.Configuration.GetValue<int>("RabbitMQ:Port"),
	defaultQueue: builder.Configuration.GetValue<string>("RabbitMQ:DefaultQueue")!,
	steamRequestsQueue: builder.Configuration.GetValue<string>("RabbitMQ:SteamRequestsQueue")!,
	steamResultsQueue: builder.Configuration.GetValue<string>("RabbitMQ:SteamResultsQueue")!,
	instantGamingRequestsQueue: builder.Configuration.GetValue<string>("RabbitMQ:InstantGamingRequestsQueue")!,
	instantGamingResultsQueue: builder.Configuration.GetValue<string>("RabbitMQ:InstantGamingResultsQueue")!,
	userName: builder.Configuration.GetValue<string>("RabbitMQ:Username")!,
	password: builder.Configuration.GetValue<string>("RabbitMQ:Password")!
));
builder.Services.AddSingleton(new RedisConfig(
	host: builder.Configuration.GetValue<string>("Redis:Host")!,
	port: builder.Configuration.GetValue<int>("Redis:Port"),
	password: builder.Configuration.GetValue<string>("Redis:Password"),
	database: builder.Configuration.GetValue<int>("Redis:Database")
));
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
	var config = sp.GetRequiredService<RedisConfig>();
	var configurationOptions = new ConfigurationOptions
	{
		EndPoints = { $"{config.Host}:{config.Port}" },
		Password = config.Password,
		DefaultDatabase = config.Database
	};
	return ConnectionMultiplexer.Connect(configurationOptions);
});

builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IGameOfferRepository, GameOfferRepository>();
builder.Services.AddScoped<IGamesWithOffersService, GamesWithOffersService>();
builder.Services.AddScoped<ISteamService, SteamService>();
builder.Services.AddScoped<IInstantGamingService, InstantGamingService>();
builder.Services.AddScoped<ILoggerFactory, LoggerFactory>();

builder.Services.AddSingleton<RedisCacheDB>();
builder.Services.AddSingleton<IBrockerPublisher, RabbitMqPublisher>();
builder.Services.AddSingleton<PublisherFactory>();

builder.Services.AddHostedService<SteamWorkerConsumer>();
builder.Services.AddHostedService<InstantGamingWorkersConsumer>();


BsonSerializer.RegisterSerializer(typeof(ECurrency), new EnumSerializer<ECurrency>(BsonType.String));
BsonSerializer.RegisterSerializer(typeof(EVendor), new EnumSerializer<EVendor>(BsonType.String));

builder.WebHost.ConfigureKestrel(options =>
{
	options.Limits.MinRequestBodyDataRate = null;
});

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
	{
		policy.AllowAnyOrigin()
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

builder.Services.AddCors(options =>
{
	options.AddPolicy("Prod", policy =>
	{
		policy.WithOrigins(
				"http://localhost:3000",
				"http://localhost:8080",
				"http://localhost:27017",
				"http://localhost:80",
				"http:80"
				)
			.AllowAnyMethod()
			.AllowAnyHeader();
	});
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

if (builder.Environment.IsDevelopment())
{
	app.Use(async (context, next) =>
	{
		// Set default user
		var identity = new ClaimsIdentity(new[]
		{
			new Claim(ClaimTypes.Name, "DevUser"),
			new Claim(ClaimTypes.Role, "Admin")
		}, "Dev");
		context.User = new ClaimsPrincipal(identity);
		
		// Enable requests logging
		context.Request.EnableBuffering();
		using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
		var body = await reader.ReadToEndAsync();
		context.Request.Body.Position = 0;

		var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
		logger.LogInformation("Incoming Request Body: {Body}", body);
		
		// Accept
		await next();
	});
	
	app.UseCors("AllowAll");
}
else
{
	app.UseCors("Prod");
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();