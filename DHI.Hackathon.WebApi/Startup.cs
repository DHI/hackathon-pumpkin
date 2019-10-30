using DHI.Services.TimeSeries;
using DHI.Services.TimeSeries.WebApi;
using DHI.Spatial.GeoJson;

namespace DHI.Hackathon.WebApi
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using DHI.Services;
    using DHI.Services.Accounts;
    using DHI.Services.WebApi;
    using DHI.Services.WebApiCore;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Versioning;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IdentityModel.Tokens;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;
    using Swashbuckle.AspNetCore.Swagger;
    using Swashbuckle.AspNetCore.SwaggerUI;
    using AccountRepository = DHI.Services.WebApiCore.AccountRepository;
    using AccountServiceConnection = DHI.Services.WebApi.AccountServiceConnection;
    using ConnectionRepository = DHI.Services.WebApiCore.ConnectionRepository;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Authentication
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration["Tokens:Issuer"],
                        ValidAudience = Configuration["Tokens:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Tokens:SecurityKey"].Resolve()))
                    };
                });

            // Authorization
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdministratorsOnly", policy => policy.RequireClaim(ClaimTypes.Role, "Administrator"));
                options.AddPolicy("EditorsOnly", policy => policy.RequireClaim(ClaimTypes.Role, "Editor"));
            });

            // API versioning
            services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new QueryStringApiVersionReader("api-version", "version", "ver"),
                    new HeaderApiVersionReader("api-version"));
            });

            // MVC
            services
                .AddSingleton(Configuration)
                .AddCors()
                .AddResponseCompression()
                .AddMvc(setupAction =>
                {
                    setupAction.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status401Unauthorized));
                    setupAction.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status403Forbidden));
                    setupAction.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status406NotAcceptable));
                    setupAction.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status500InternalServerError));
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
#warning By default, the Web API will format JSON responses using camelCasing. If you want to switch to PascalCasing, you should comment in the below line
                    //options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    options.SerializerSettings.Converters.Add(new IsoDateTimeConverter());
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    options.SerializerSettings.Converters.Add(new KeyValuePairConverter());

#warning Depending on which Web API packages you install in this project, you might have to register domain-specific JSON converters for these packages
                    // GIS service JSON converters
                    options.SerializerSettings.Converters.Add(new PositionConverter());
                    options.SerializerSettings.Converters.Add(new GeometryConverter());
                    options.SerializerSettings.Converters.Add(new AttributeConverter());
                    options.SerializerSettings.Converters.Add(new FeatureConverter());
                    options.SerializerSettings.Converters.Add(new FeatureCollectionConverter());
                    options.SerializerSettings.Converters.Add(new GeometryCollectionConverter());

                    // Timeseries services JSON converters
                    options.SerializerSettings.Converters.Add(new DataPointConverter<double, int?>());
                    options.SerializerSettings.Converters.Add(new TimeSeriesDataConverter<double, Dictionary<string, object>>());
                    options.SerializerSettings.Converters.Add(new TimeSeriesDataConverter<double, int?>());
                    options.SerializerSettings.Converters.Add(new TimeSeriesDataConverter<Vector<double>, int?>());

                });

            // HSTS
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.MaxAge = TimeSpan.FromDays(Configuration.GetValue<double>("AppConfiguration:HstsMaxAgeInDays"));
            });

            // Swagger
            services.AddSwaggerGen(setupAction =>
            {
                setupAction.SwaggerDoc(
                    Configuration["Swagger:SpecificationName"],
                    new Info
                    {
                        Title = Configuration["Swagger:DocumentTitle"],
                        Version = "1",
                        Description = File.ReadAllText(Configuration["Swagger:DocumentDescription"].Resolve())
                    });

                setupAction.EnableAnnotations();
                setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.WebApi.xml"));
                setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.WebApiCore.xml"));

#warning Depending on which Web API packages you install in this project, you need to register the XML-files from these packages
                //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Documents.WebApi.xml"));
                setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.GIS.WebApi.xml"));
                //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Jobs.WebApi.xml"));
                //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Rasters.WebApi.xml"));
                //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Spreadsheets.WebApi.xml"));
                //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.Tables.WebApi.xml"));
                setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.TimeSeries.WebApi.xml"));
                //setupAction.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "DHI.Services.TimeSteps.WebApi.xml"));

                setupAction.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    Type = "apiKey",
                    In = "header",
                    Description = "Enter the word 'Bearer' followed by a space and the JWT.",
                    Name = "Authorization"
                });

                setupAction.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    {"Bearer", Enumerable.Empty<string>()}
                });
            });
        }

        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseAuthentication();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseHttpsRedirection();
            app.UseSwagger();
            app.UseSwaggerUI(setupAction =>
            {
                var specificationName = Configuration["Swagger:SpecificationName"];
                setupAction.SwaggerEndpoint($"../swagger/{specificationName}/swagger.json", Configuration["Swagger:DocumentName"]);
                setupAction.DocExpansion(DocExpansion.None);
            });
            app.UseExceptionHandling();
            app.UseResponseCompression();
            app.UseMvc();

            // Set the data directory (App_Data folder)
            var contentRootPath = Configuration.GetValue("AppConfiguration:ContentRootPath", env.ContentRootPath);
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(contentRootPath, "App_Data"));

            // System services
            ServiceLocator.Register(new ConnectionTypeService(AppContext.BaseDirectory), ServiceId.ConnectionTypes);

            // Custom services
            var lazyCreation = Configuration.GetValue("AppConfiguration:LazyCreation", true);
            Services.Configure(new ConnectionRepository("connections.json"), lazyCreation);

            // Default Account service
            if (!Services.Connections.Exists(ServiceId.Accounts))
            {
                var accountServiceConnection = new AccountServiceConnection("Accounts", "Accounts service connection")
                {
                    ConnectionString = "accounts.json",
                    RepositoryType = typeof(AccountRepository).AssemblyQualifiedName
                };

                ServiceLocator.Register(accountServiceConnection.Create(), ServiceId.Accounts);
            }

            // Default Authentication service
            if (!Services.Connections.Exists(ServiceId.Authentication))
            {
                var authenticationServiceConnection = new AuthenticationServiceConnection("Authentication", "Authentication service connection")
                {
                    ConnectionString = "accounts.json",
                    AuthenticationProviderType = typeof(AccountRepository).AssemblyQualifiedName
                };

                ServiceLocator.Register(authenticationServiceConnection.Create(), ServiceId.Authentication);
            }

            // Default Host service
            if (!Services.Connections.Exists(ServiceId.Hosts))
            {
                var hostServiceConnection = new HostServiceConnection("Hosts", "Hosts service connection")
                {
                    ConnectionString = "hosts.json",
                    RepositoryType = typeof(HostRepository).AssemblyQualifiedName
                };

                ServiceLocator.Register(hostServiceConnection.Create(), ServiceId.Hosts);
            }
        }
    }
}