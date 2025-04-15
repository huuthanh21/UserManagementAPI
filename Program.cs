using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Error-handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An unhandled exception occurred.");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "Internal server error." });
    }
});

// Authentication middleware
app.Use(async (context, next) =>
{
    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

    if (string.IsNullOrEmpty(token) || !IsValidToken(token))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
        return;
    }

    await next();
});

bool IsValidToken(string token)
{
    // Replace this with your actual token validation logic
    return token == "valid-token";
}

// Logging middleware
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var path = context.Request.Path;

    await next();

    var statusCode = context.Response.StatusCode;
    logger.LogInformation("HTTP {Method} {Path} responded with {StatusCode}", method, path, statusCode);
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "Hello, World!");

var users = new List<User>
{
    new User { Id = 1, Name = "Alice Johnson", Email = "alice.johnson@example.com" },
    new User { Id = 2, Name = "Bob Smith", Email = "bob.smith@example.com" },
    new User { Id = 3, Name = "Charlie Brown", Email = "charlie.brown@example.com" },
    new User { Id = 4, Name = "Diana Prince", Email = "diana.prince@example.com" },
    new User { Id = 5, Name = "Eve Adams", Email = "eve.adams@example.com" }
};
var usersLock = new object();

app.MapGet("/users", (int? page, int? pageSize) =>
{
    const int defaultPageSize = 10;
    page ??= 1;
    pageSize ??= defaultPageSize;

    var paginatedUsers = users
        .Skip((page.Value - 1) * pageSize.Value)
        .Take(pageSize.Value)
        .ToList();

    return Results.Ok(paginatedUsers);
});

app.MapGet("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null)
    {
        logger.LogWarning("User with ID {Id} not found.", id);
        return Results.NotFound();
    }
    return Results.Ok(user);
});

app.MapPost("/users", (User user) =>
{
    if (string.IsNullOrWhiteSpace(user.Name) || string.IsNullOrWhiteSpace(user.Email))
    {
        return Results.BadRequest("Name and Email are required.");
    }

    if (!IsValidEmail(user.Email))
    {
        return Results.BadRequest("Invalid email format.");
    }

    lock (usersLock)
    {
        user.Id = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;
        users.Add(user);
    }
    return Results.Created($"/users/{user.Id}", user);
});

app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    if (string.IsNullOrWhiteSpace(updatedUser.Name) || string.IsNullOrWhiteSpace(updatedUser.Email))
    {
        return Results.BadRequest("Name and Email are required.");
    }

    if (!IsValidEmail(updatedUser.Email))
    {
        return Results.BadRequest("Invalid email format.");
    }

    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    return Results.Ok(user);
});

app.MapDelete("/users/{id}", (int id) =>
{
    lock (usersLock)
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        if (user is null)
        {
            logger.LogWarning("User with ID {Id} not found.", id);
            return Results.NotFound();
        }

        users.Remove(user);
    }
    return Results.NoContent();
});

bool IsValidEmail(string email)
{
    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
    }
    catch
    {
        return false;
    }
}

app.Run();