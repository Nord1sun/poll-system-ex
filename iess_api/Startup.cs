using System;
using System.IO;
using System.Text;
using iess_api.Models;
using Newtonsoft.Json;
using FluentValidation;
using System.Xml.XPath;
using System.Reflection;
using iess_api.Managers;
using iess_api.Validation;
using iess_api.Extensions;
using iess_api.Interfaces;
using iess_api.Repositories;
using NLog.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace iess_api
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment environment, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            Environment = environment;
            loggerFactory.ConfigureNLog($"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}Extensions{Path.DirectorySeparatorChar}nlog.config");
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment Environment { get; }
        public string ApiName => "IESS API";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddFluentValidation(options =>
            {
                options.RegisterValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            }).AddJsonOptions(options => {
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.Configure<MongoDbSettings>(options =>
            {
                options.ConnectionString = Environment.IsDevelopment() ? Configuration["MongoConnection:DevelopmentConnectionString"] :
                    Configuration["MongoConnection:ConnectionString"];        
                options.Database = Configuration["MongoConnection:Database"];
            });

            services.AddHttpContextAccessor();
            services.AddTransient<ITokenRepository, TokenRepository>();
            services.AddTransient<ITokenManager, TokenManager>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddTransient<IUserManager, UserManager>();
            services.AddScoped<IPollRepository, PollRepository>();
            services.AddScoped<IQuizRepository, QuizRepository>();
            services.AddScoped<IFileRepository, GridFsRepository>();
            services.AddTransient<IGroupRepository, GroupRepository>();
            services.AddTransient<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddTransient<IGroupManager, GroupManager>();
            services.AddTransient<IPictureManager, PictureManager>();
            services.AddTransient<IExceptionManager, ExceptionManager>();
            services.AddTransient<IExceptionRepository, ExceptionRepository>();
            services.AddSingleton<ILoggerManager, LoggerManager>();

            services.AddTransient<IValidator<Poll>, PollValidator>();
            services.AddTransient<IValidator<Quiz>, QuizValidator>();
            services.AddTransient<IValidator<PollAnswersUnit>, PollAnswersUnitValidator>();
            services.AddTransient<IValidator<QuizAnswersUnit>, QuizAnswersUnitValidator>();
            services.AddTransient<IValidator<AssessTextAnswerModel>, AssessTextAnswerModelValidator>();
            services.AddTransient<IValidator<string>, StringObjectIdValidator>();
            services.AddTransient<IValidator<UserModel>, UserModelValidator>();
            services.AddTransient<IValidator<GroupModel>, GroupValidator>();
            services.AddTransient<IValidator<PageInfo>, PageInfoValidator>();
            

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAnyPolicy",
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });

            var authPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build();
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.TokenValidationParameters.ValidateIssuer = true;
                o.TokenValidationParameters.ValidIssuer = "http://www.iess.com";
                o.TokenValidationParameters.ValidateIssuerSigningKey = true;
                o.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JWT:key"]));
                o.TokenValidationParameters.ValidateAudience = false;
                o.TokenValidationParameters.ValidateLifetime = true;
                o.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
            });
            services.AddAuthorization(auth => auth.AddPolicy("Bearer", authPolicy));

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Info
                {
                    Title = ApiName,
                    Description = $"{ApiName} (ASP.NET Core 2.1)",
                    Version = "2.0.BUILD_NUMBER"
                });

                options.DescribeAllEnumsAsStrings();

                var comments =
                    new XPathDocument($"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}{Environment.ApplicationName}.xml");
                options.OperationFilter<XmlCommentsOperationFilter>(comments);

                options.AddSecurityDefinition("Bearer", new ApiKeyScheme()
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });
                options.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    { "Bearer", new string[] { } }
                });
                options.AddFluentValidationRules();
            });

        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerManager logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.ConfigureExceptionHandler(logger);
            app.UseCors("AllowAnyPolicy");
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseMvc();
            
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", ApiName));
        }
    }
}
