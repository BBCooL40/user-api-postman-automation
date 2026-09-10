using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing",
    "Bracing",
    "Chilly",
    "Cool",
    "Mild",
    "Warm",
    "Balmy",
    "Hot",
    "Sweltering",
    "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5)
        .Select(index =>
            new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
        .ToArray();

    return forecast;
})
.WithName("GetWeatherForecast");

app.MapPost("/api/users", async (CreateUserRequest user, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(user.Name))
{
    return Results.UnprocessableEntity(new
    {
        error = "Name is required."
    });
}
if (string.IsNullOrWhiteSpace(user.Email))
{
    return Results.UnprocessableEntity(new
    {
        error = "Email is required."
    });
}
    if (!MailAddress.TryCreate(user.Email, out _))
{
    return Results.UnprocessableEntity(new
    {
        error = "Email format is invalid."
    });
}
    if (user.Age < 18 || user.Age > 65)
    {
        return Results.UnprocessableEntity(new
        {
            error = "Age must be between 18 and 65."
        });
    }
    bool emailExists = await db.Users.AnyAsync(existingUser =>
        existingUser.Email.ToLower() == user.Email.ToLower()
    );

    if (emailExists)
{
    return Results.Conflict(new
    {
        error = "Email already exists."
    });
}
    var id = Guid.NewGuid();

var newUser = new User(
    id,
    user.Name,
    user.Email,
    user.Age
);

    db.Users.Add(newUser);
    await db.SaveChangesAsync();

    return Results.Created($"/api/users/{id}", newUser);
})
.WithName("CreateUser");
app.MapGet("/api/users/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var foundUser = await db.Users.FindAsync(id);

    if (foundUser is null)
    {
        return Results.NotFound(new
        {
            error = "User not found."
        });
    }

    return Results.Ok(foundUser);
})
.WithName("GetUserById");
app.MapGet("/api/users", async (AppDbContext db) =>
{
    var users = await db.Users.ToListAsync();

    return Results.Ok(users);
})
.WithName("GetAllUsers");
app.MapDelete("/api/users/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var userToDelete = await db.Users.FindAsync(id);

    if (userToDelete is null)
    {
        return Results.NotFound(new
        {
            error = "User not found."
        });
    }

    db.Users.Remove(userToDelete);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeleteUser");
app.Run();

record WeatherForecast(
    DateOnly Date,
    int TemperatureC,
    string? Summary)
{
    public int TemperatureF =>
        32 + (int)(TemperatureC / 0.5556);
}

record CreateUserRequest(
    string Name,
    string Email,
    int Age);

public record User(
    Guid Id,
    string Name,
    string Email,
    int Age);