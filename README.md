# Options and Configuration

Install package Microsoft.Extensions.Configuration

## IConfiguration

```c#
// Get value
// return default value if key is not found
var value = _configuration.GetValue<bool>("section1:section2:key");

// Get section 
var section = _configuration.GetSection("section1:section2");
var value = section.GetValue<bool>("key");
```

Disadvantages of using IConfiguration:
- Repetive code (must type section and key for each value)
- Fragile naming

## Binding Configuration

```c#
public class FeatureOptions
{
	public bool EnableGreeting { get; set; }
	public string GreetingContent { get; set; }
}

var featureOptions = new FeatureOptions();
//_configuration.GetSection("Features:HomeEndpoint").Bind(featureOptions);
_configuration.Bind("Features:HomeEndpoint", featureOptions);
```

## Options Pattern

```c#
// must be concrete class 
// must have public parameterless constructor	
// properties must have public setter
public class FeatureConfiguration
{
	public bool EnableGreeting { get; set; }
	public string GreetingContent { get; set; }

}

public class HomeController
{
	private readonly FeatureConfiguration _featureConfiguration;

	public HomeController(IOptions<FeatureConfiguration> options)
	{
		__featureConfiguration = options.Value;
	}
}
```

Install package **Microsoft.Extensions.Options** to support option registration.

```c#
builder.Services.Configure<FeatureConfiguration>(_configuration.GetSection("Features:HomeEndpoint"));
```

## IOptions, IOptionsSnapshot, IOptionsMonitor

IOptions:
- **Does not support** reloading of configuration
- Registered as a **singleton** in D.I. container
- Values bound when first used
- Can be injected into all service lifetimes
- **Does not support** named options

IOptionsSnapshot:
- **Supports** reloading of configuration
- Registered as **scoped** in D.I. container
- Values may reload per request
- Can not be injected into singleton services
- **Supports** named options

IOptionsMonitor:
- **Supports** reloading of configuration
- Registered as **singleton** in D.I. container
- Values are reloaded immediately
- Can be injected into all service lifetimes
- **Supports** named options

```c#
public class SomeController
{
	private readonly IOptionsMonitor<FeatureConfiguration> _options;
	private FeatureConfiguration _featureConfiguration;

	public SomeController(
		IOptionsMonitor<FeatureConfiguration> options)
	{

		_options = options;
		// access current value via CurrentValue property
		// or you can do this
		_featureConfiguration = _options.CurrentValue;
		options.OnChange(config =>
		{
			_featureConfiguration = config;
			// some logging here
		});
	}

	
}
```

## Named Options

Use the same Options Class with different configuration sections.

```c#
builder.Services.Configure<MyConfiguration>(
							"OptionName", 
							_configuration.GetSection("Features:HomeEndpoint")
						);
```

```c#
public SomeController(
	IOptionsMonitor<MyConfiguration> options)
{
	var config = options.Get("OptionName");
}
```

## Options Validation

### Data Annotations

```c#
public class FeatureConfiguration
{
	[Required]
	public bool EnableGreeting { get; set; }
	[Required]
	public string GreetingContent { get; set; }
}
```

```c#
builder.Services.AddOptions<FeatureConfiguration>()
		.Bind(builder.Configuration.GetSection("SectionString"))
		.ValidateDataAnnotations()
		// We only get the validation error when the Option instance is first accessed.
		// so we need to add this line
		.ValidateOnStart();
```

### Advanced Validation

```c#
builder.Services.AddOptions<FeatureConfiguration>()
		.Bind(builder.Configuration.GetSection("SectionString"))
		.Validate(c => 
		{
			if (c.EnableGreeting && string.IsNullOrEmpty(c.GreetingContent))
			{
				return false;
			}

			return true;
		}, "Some error messages.")
		.ValidateOnStart();
```

```c#
public class FeatureConfigurationValidator : IValidateOptions<FeatureConfiguration>
{
	public ValidateOptionsResult Validate(string name, FeatureConfiguration options)
	{
		if (options.EnableGreeting && string.IsNullOrEmpty(options.GreetingContent))
		{
			return ValidateOptionsResult.Fail("Greeting content is required.");
		}

		return ValidateOptionsResult.Success;
	}
}
```

```c#
builder.Services.AddOptions<FeatureConfiguration>("OptionName")
		.Bind(builder.Configuration.GetSection("SectionString"))
		.ValidateOnStart();

builder.Services.TryAddEnumerable(
	ServiceDescriptor.Singleton<IValidateOptions<FeatureConfiguration>, FeatureConfigurationValidator>()
);
```

### Named Options Validation

```c#
public class FeatureConfigurationValidator : IValidateOptions<FeatureConfiguration>
{
	public ValidateOptionsResult Validate(string name, FeatureConfiguration options)
	{
		switch(name)
		{
			case "Option1":
				if (options.EnableGreeting && string.IsNullOrEmpty(options.GreetingContent))
				{
					return ValidateOptionsResult.Fail("Greeting content is required.");
				}
				break;
			case "Option2":
				if (options.EnableGreeting && string.IsNullOrEmpty(options.GreetingContent))
				{
					return ValidateOptionsResult.Fail("Greeting content is required.");
				}
				break;
			default:
				return ValidateOptionsResult.Skip;
		}
	}
}
```

## Using Interface

```c#
public class SomeServiceConfiguration : ISomeServiceConfiguration
{
	// properties here
}
```

```c#
public class SomeController
{
	private readonly ISomeServiceConfiguration _configuration;

	public SomeController(ISomeServiceConfiguration configuration)
	{
		_configuration = configuration;
	}
}
```

```c#
builder.Services.Configure<SomeServiceConfiguration>(_configuration.GetSection("SectionString"));

builder.Services.AddSingleton<ISomeServiceConfiguration>(sp => 
{
	return sp.GetRequiredService<IOptions<SomeServiceConfiguration>>().Value;
});	
```

## Unit Testing

### Options.Create()

```c#
var options = Options.Create(new FeatureConfiguration
{
	EnableGreeting = true,
	GreetingContent = "Hello"
});
```

### Moq

```c#
var mockOptions = new Mock<IOptions<FeatureConfiguration>>();
mockOptions.SetupGet(x => x.Value).Returns(new FeatureConfiguration
{
	EnableGreeting = true,
	GreetingContent = "Hello"
});
```

## Configuration Providers

- JSON
- Environment Variables
- Command Line Arguments
- Configuration Secrets
	- User Secrets -> Development
	- Azure Key Vault -> Production
- Cloud Services (AWS Parameter Store, etc.)
- Custom Configuration Providers

When the host for the application is built, it gets its initial configuration via 
loading configuration from enviroment variables.
During the host builder phase, the application configuration is loaded.


**Configuration sources are added in order. The later sources will override values
for configuration entries added by prior sources.** 

```c#
var configBuilder = new ConfigurationBuilder();
configBuilder.SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration config = configBuilder.Build();
```

```c#
builder.ConfigureAppConfiguration((hostingContext, config) =>
{
	var env = hostingContext.HostingEnvironment;

	config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
		  .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
	
	if (env.IsDevelopment() && !string.IsNullOrEmpty(env.ApplicationName))
	{
		var assembly = Assembly.Load(new AssemblyName(env.ApplicationName));
		if (assembly != null)
		{
			config.AddUserSecrets(assembly, optional: true);
		}
	}
	
	config.AddEnvironmentVariables();
	
	if (args != null)
	{
		config.AddCommandLine(args);
	}
});
```

### Environment Variables

```cli
// last argument is the scope
[Environment]::SetEnvironmentVariable("AAA__MyKey", "MyValue", "User") 
```

```json
// launchSettings.json	

"profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5239",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
		"AAA__MyKey": "MyValue"
      }
    }
}
```

### Command Line Arguments

```cli
dotnet run --AAA:MyKey "MyValue"

dotnet run AAA:MyKey="MyValue"
```

### User Secrets

```cli
dotnet user-secrets set "Section:Key" "Value"

dotnet user-secrets list
```

### Secure Secrets in Production

Install package Azure.Identity
Install package Azure.Extensions.AspNetCore.Configuration.Secrets

```c#
if (builder.Environment.IsProduction())
{
	builder.Services.AddAzureKeyVault(
		new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net"),
		new DefaultAzureCredential()
	);
}
```

### AWS Parameter Store

Install package Amazon.Extensions.Configuration.SystemsManager

```c#
builder.Configuration.AddSystemsManager(configure =>
{
	configure.Path = "";
	configure.ReloadAfter = TimeSpan.FromMinutes(5);
	configure.Optional = true;
});
```

### Custom the Order of Configuration Providers

```c#
builder.ConfigureAppConfiguration((hostingContext, config) =>
{
	configure.Sources.Clear();

	var env = hostingContext.HostingEnvironment;

	config.AddEnvironmentVariables("ASPNETCORE_");

	config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
		  .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
	
	if (env.IsDevelopment() && !string.IsNullOrEmpty(env.ApplicationName))
	{
		var assembly = Assembly.Load(new AssemblyName(env.ApplicationName));
		if (assembly != null)
		{
			config.AddUserSecrets(assembly, optional: true);
		}
	}
	
	if (args != null)
	{
		config.AddCommandLine(args);
	}

	config.AddEnvironmentVariables();
});
```

### Custom Configuration Providers

```c#
public class CustomConfigurationProvider : ConfigurationProvider
{
	public CustomConfigurationProvider(Action<DbContextOptionsBuilder> optionsAction)
	{
		OptionsAction = optionsAction;
	}
	public Action<DbContextOptionsBuilder> OptionsAction { get; init; }

	public override void Load()
	{
		var builder = new DbContextOptionsBuilder<AppDbContext>();
		OptionsAction(builder);

		using (var dbContext = new AppDbContext(builder.Options))
		{
			dbContext.Database.EnsureCreated();

			Data = dbContext.ConfigurationEntries().Any()
					? dbContext.ConfigurationEntries()
					.ToDictionary(e => e.Key, e => e.Value, StringComparer.OrdinalIgnoreCase)
				: new Dictionary<string, string>();
		}
	}
}
```

```c#
public class EntityFrameworkConfigurationSource : IConfigurationSource
{
	private readonly Action<DbContextOptionsBuilder> _optionsAction;

	public EntityFrameworkConfigurationSource(Action<DbContextOptionsBuilder> optionsAction)
	{
		_optionsAction = optionsAction;
	}

	public IConfigurationProvider Build(IConfigurationBuilder builder)
	{
		return new CustomConfigurationProvider(_optionsAction);
	}
}
```

```c#
public static class EntityFrameworkConfigurationExtensions
{
	public static IConfigurationBuilder AddEntityFrameworkConfiguration(
		this IConfigurationBuilder builder,
		Action<DbContextOptionsBuilder> optionsAction)
	{
		return builder.Add(new EntityFrameworkConfigurationSource(optionsAction));
	}
}
```

## Debugging Configuration

```c#
var debugView = builder.Configuration.GetDebugView();
```
