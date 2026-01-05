using CCenter.Data;
using CCenter.Services;
using CCenter.Services.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using CCenter.Api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

var rawCs = builder.Configuration.GetConnectionString("CCenter")
           ?? throw new InvalidOperationException("Missing ConnectionStrings:CCenter");

var csb = new SqlConnectionStringBuilder(rawCs);

if (string.IsNullOrWhiteSpace(csb.Password))
{
    var pwd = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD");
    if (string.IsNullOrWhiteSpace(pwd))
        throw new InvalidOperationException("Missing MSSQL_SA_PASSWORD env var (needed for local dev & EF migrations).");

    csb.Password = pwd;
}

builder.Services.AddDbContext<CCenterDbContext>(opt =>
    opt.UseSqlServer(csb.ConnectionString));

builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ApiExceptionMiddleware>();

app.MapControllers();
app.Run();
