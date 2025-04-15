using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.MapGet("/", () => "Hello, World!");

var users = new List<User>
{
    new User { Id = 1, Name = "John Doe", Email = "john.doe@example.com" },
    new User { Id = 2, Name = "Jane Smith", Email = "jane.smith@example.com" }
};

app.MapGet("/users", () => users);

app.MapGet("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.MapPost("/users", (User user) =>
{
    user.Id = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;
    users.Add(user);
    return Results.Created($"/users/{user.Id}", user);
});

app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();

    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    return Results.Ok(user);
});

app.MapDelete("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound();

    users.Remove(user);
    return Results.NoContent();
});

app.Run();