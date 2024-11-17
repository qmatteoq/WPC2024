using ShareSnapAPI.Requests;
using ShareSnapAPI.Scenarios;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.AllowAnyHeader();
            policy.AllowAnyMethod();
            policy.AllowAnyOrigin();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

app.MapPost("/processDocument", async (DocumentProcessRequest document) =>
{
    string prompt = @$"Summarize the following article and create an engaging post to share it on {document.SocialNetwork} as a news: {document.Document}.;
    You have access to a tool that you can use to share the post on the most appropriate channel, based on the user selection.";

    BlogAuthorScenario scenario = new BlogAuthorScenario();
    scenario.InitializeScenario(false);
    try
    {
        await scenario.ExecuteScenario(document.Document);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.InternalServerError();
    }
})
.WithName("ProcessDocument");

app.Run();