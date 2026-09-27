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
using GamesFinder.Orchestrator.Repositories.Repositories;
using GamesFinder.Orchestrator.Publisher.RabbitMQ;
using GamesFinder.Orchestrator.Domain.Interfaces.Infrastructure;
using GamesFinder.Orchestrator.Publisher.Redis;
using StackExchange.Redis;
using GamesFinder.Orchestrator.Publisher;
using GamesFinder.Orchestrator.Consumers;
using GamesFinder.Orchestrator.Services.DomainServices;
using GamesFinder.Orchestrator.Domain.Interfaces.DomainServices;
using GamesFinder.Orchestrator.Domain.Interfaces.Services.ApplicationServices;
using GamesFinder.Orchestrator.Services.ApplicationServices;
using GamesFinder.Orchestrator.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers()
	.AddJsonOptions(options =>
    {
			options.JsonSerializerOptions.Converters.Add(
				new System.Text.Json.Serialization.JsonStringEnumConverter()
			);
	});
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
	ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
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
	if (requireJwt)
    {
			options.FallbackPolicy = new AuthorizationPolicyBuilder()
				.RequireAuthenticatedUser()
				.Build();
    }
});


builder.Services.AddSingleton(new SteamOptions(
	domainName: builder.Configuration.GetValue<string>("SteamApi:Name")!,
	apiKey: builder.Configuration.GetValue<string>("SteamApi:Key")!,
	maxRequests: builder.Configuration.GetValue<int>("SteamApi:MaxRequests")!,
	tagsRquestsDelay: builder.Configuration.GetValue<int>("SteamApi:TagsRquestsDelay")!,
	cooldownMilliseconds: builder.Configuration.GetValue<int>("SteamApi:CooldownMilliseconds")!
));
builder.Services.AddSingleton(new WorkersOptions(
	instantGamingWorkerCount: builder.Configuration.GetValue<int>("Workers:InstantGamingWorkerCount")!,
	instantGamingOverrideBatchSize: builder.Configuration.GetValue<int?>("Workers:InstantGamingOverrideBatchSize")
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
builder.Services.AddScoped<IUserDataRepository, UserDataRepository>();
builder.Services.AddScoped<IGamesWithOffersService, GamesWithOffersService>();
builder.Services.AddScoped<ISteamService, SteamService>();
builder.Services.AddScoped<IInstantGamingService, InstantGamingService>();

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
	options.AddPolicy("Prod", policy =>
	{
		policy.WithOrigins(builder.Configuration
				.GetSection("Security:CORS:Origins")
				.Get<string[]>() ?? [])
			.AllowAnyMethod()
			.AllowAnyHeader();
	});
});

if (builder.Environment.IsDevelopment())
{
	builder.Services.AddCors(options =>
	{
		options.AddPolicy("DevCors", policy =>
		{
			policy
				.AllowAnyOrigin()
				.AllowAnyHeader()
				.AllowAnyMethod();
		});
	});
}
else
{
	builder.Services.AddCors(options =>
    {
			options.AddPolicy("ProdCors", policy =>
			{
				policy
					.WithOrigins("https://your-frontend.example")
					.AllowAnyHeader()
					.AllowAnyMethod();
			});
    });
}

//TODO: Add userData seeding for default users
var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
	app.Use(async (context, next) =>
	{
		app.MapOpenApi();

		// Enable requests logging
		context.Request.EnableBuffering();
		using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
		var body = await reader.ReadToEndAsync();
		context.Request.Body.Position = 0;

		var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
		logger.LogInformation($"From: {context.Request.Path}/{context.Request.QueryString}\nIncoming Request Body: {body}");
		
		// Accept
		await next();
	});
	
	app.UseCors("DevCors");
}
else
{
	app.UseCors("ProdCors");
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();