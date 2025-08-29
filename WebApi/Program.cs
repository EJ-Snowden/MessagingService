using MessagingService.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonEnumSupport();

builder.Services
    .AddSwaggerDocs()
    .AddAppServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseProviderConfig();

app.MapControllers();

app.Run();