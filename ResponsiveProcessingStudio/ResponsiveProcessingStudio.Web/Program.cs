using ResponsiveProcessingStudio.Core.Abstractions;
using ResponsiveProcessingStudio.Core.Factories;
using ResponsiveProcessingStudio.Core.Pipeline;
using ResponsiveProcessingStudio.Core.Processing;
using ResponsiveProcessingStudio.Core.Retry;
using ResponsiveProcessingStudio.Core.Simulation;
using ResponsiveProcessingStudio.Core.State;
using ResponsiveProcessingStudio.Web.Hubs;
using ResponsiveProcessingStudio.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddSingleton<BankServiceFactory>();
builder.Services.AddSingleton<IErrorSimulator, ErrorSimulator>();
builder.Services.AddSingleton<IPipelineMetrics, PipelineMetrics>();
builder.Services.AddSingleton<IRequestStateStore, RequestStateStore>();
builder.Services.AddSingleton<IRequestClassifier, RequestClassifier>();
builder.Services.AddSingleton<IRequestValidator, RequestValidator>();
builder.Services.AddSingleton<IRequestAssigner, RequestAssigner>();
builder.Services.AddSingleton<IRequestProcessor, RequestProcessor>();
builder.Services.AddSingleton<IRetryPolicy, RetryPolicy>();
builder.Services.AddSingleton<ISupportRequestPipeline, SupportRequestPipeline>();
builder.Services.AddSingleton<TestDataGeneratorService>();
builder.Services.AddHostedService<PipelineBroadcastService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<PipelineHub>("/hubs/pipeline");

app.Run();
