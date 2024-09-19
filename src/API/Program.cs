using System.Data;
using API.DataAccess;
using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();  // Register controllers
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Dapper and database connection (MySQL) from appsettings.json
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    var mySqlConnectionBuilder = new MySqlConnectionStringBuilder(connectionString)
    {
        UserID = builder.Configuration["ConnectionStrings:DefaultConnection:DB_USER"],
        Password = builder.Configuration["ConnectionStrings:DefaultConnection:DB_PASSWORD"]
    };
    return new MySqlConnection(mySqlConnectionBuilder.ConnectionString);
});

builder.Services.AddScoped<Routes>();

builder.Services.AddRouting(routingServices =>
{
    routingServices.LowercaseUrls = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();  // Map the controllers

app.Run();