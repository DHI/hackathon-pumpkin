namespace WebAPI
{
    using System.IO;
    using System.Net.Http.Extensions.Compression.Core.Compressors;
    using System.Net.Http.Formatting;
    using System.Net.Http.Headers;
    using System.Web;
    using System.Web.Http;
    using System.Web.Http.Routing;
    using DHI.Services;
    using DHI.Services.Accounts;
    using DHI.Services.Web;
    using Microsoft.AspNet.WebApi.Extensions.Compression.Server;
    using Microsoft.Web.Http;
    using Microsoft.Web.Http.Versioning;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;
    using Properties;
    using Thinktecture.IdentityModel.WebApi.Authentication.Handler;
    using AccountRepository = DHI.Services.Web.AccountRepository;
    using AccountServiceConnection = DHI.Services.Web.AccountServiceConnection;
    using ConnectionTypeService = DHI.Services.Web.ConnectionTypeService;
    using HostRepository = DHI.Services.Web.HostRepository;
    using HostServiceConnection = DHI.Services.Web.HostServiceConnection;
    using ServiceId = DHI.Services.Web.ServiceId;

    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // CORS support
            config.EnableCors();

            // JSON configuration
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple;
            ((DefaultContractResolver)config.Formatters.JsonFormatter.SerializerSettings.ContractResolver).IgnoreSerializableAttribute = true;
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new IsoDateTimeConverter());
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new KeyValuePairConverter());

#warning Depending on which domain services you install in this project, you might have to register domain-specific JSON converters for these services
            // GIS service JSON converters
            //config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new PositionConverter());
            //config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new GeometryConverter());
            //config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new AttributeConverter());
            //config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new FeatureConverter());
            //config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new FeatureCollectionConverter());
            //config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new GeometryCollectionConverter());

            // Timeseries services JSON converters
            //config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new DataPointConverter<double, int?>());
            //config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new TimeSeriesDataConverter<double, Dictionary<string, object>>());
            //config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new TimeSeriesDataConverter<double, int?>());
            //config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new TimeSeriesDataConverter<Vector<double>, int?>());

            // BSON support
            config.Formatters.Add(new BsonMediaTypeFormatter());

            // Custom route constraints
            var constraintResolver = new DefaultInlineConstraintResolver();
            constraintResolver.ConstraintMap.Add("date", typeof(DateTimeConstraint));

            // System services
            ServiceLocator.Register(new ConnectionTypeService(HttpRuntime.BinDirectory), ServiceId.ConnectionTypes);

            // Custom services
            var connectionsFolder = Path.Combine(HttpRuntime.AppDomainAppPath, "App_Data");
            Services.Configure(new ConnectionRepository(Path.Combine(connectionsFolder, "connections.json")), Settings.Default.LazyCreation);

            // Default account service
            if (!Services.Connections.Exists(ServiceId.Accounts))
            {
                var accountServiceConnection = new AccountServiceConnection("Accounts", "Accounts connection")
                {
                    ConnectionString = "accounts.json",
                    RepositoryType = typeof(AccountRepository).AssemblyQualifiedName
                };

                ServiceLocator.Register(accountServiceConnection.Create(), ServiceId.Accounts);
            }

            // Default job host service
            if (!Services.Connections.Exists(ServiceId.Hosts))
            {
                var hostServiceConnection = new HostServiceConnection("Hosts", "Hosts connection")
                {
                    ConnectionString = "hosts.json",
                    RepositoryType = typeof(HostRepository).AssemblyQualifiedName
                };

                ServiceLocator.Register(hostServiceConnection.Create(), ServiceId.Hosts);
            }

            // Security
            var authenticationConfiguration = new AuthenticationConfiguration { RequireSsl = false };
            var accountService = Services.Get<AccountService>(ServiceId.Accounts);
            authenticationConfiguration.AddBasicAuthentication((userName, password) => accountService.Validate(userName, password), username => accountService.GetRoles(username), "DHI Web API");
            config.MessageHandlers.Add(new AuthenticationHandler(authenticationConfiguration));

            // Routing
            config.MapHttpAttributeRoutes(constraintResolver);
            config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{id}", new { id = RouteParameter.Optional });

            // Compression support
            var serverCompressionHandler = new ServerCompressionHandler(Settings.Default.CompressionThresshold, new GZipCompressor(), new DeflateCompressor());
            GlobalConfiguration.Configuration.MessageHandlers.Insert(0, serverCompressionHandler);

            // Versioning support
            config.AddApiVersioning(o =>
            {
                o.ApiVersionReader = new MediaTypeApiVersionReader("api-version");
                o.ReportApiVersions = true;
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.DefaultApiVersion = new ApiVersion(1, 0);
            });
        }
    }
}