# JsonByExampleGenerator

![publish to nuget](https://github.com/hermanussen/JsonByExampleGenerator/workflows/publish%20to%20nuget/badge.svg) [![Nuget](https://img.shields.io/nuget/v/JsonByExampleGenerator)](https://www.nuget.org/packages/JsonByExampleGenerator/) [![Nuget](https://img.shields.io/nuget/dt/JsonByExampleGenerator?label=nuget%20downloads)](https://www.nuget.org/packages/JsonByExampleGenerator/) [![Twitter URL](https://img.shields.io/twitter/url?style=social&url=https%3A%2F%2Ftwitter.com%2Fknifecore%2F)](https://twitter.com/knifecore)

Generate classes based on example json files in your project. Uses a C# 9 source generator.

# Installation

1. Install the NuGet package in your project. Run the following command in the NuGet package manager console
```
Install-Package JsonByExampleGenerator
```
or using the .NET cli
```
dotnet add package JsonByExampleGenerator
```
2. Ensure the json files that you want to use as examples are added as `AdditionalFiles` in your `.csproj` file. E.g.:
```xml
<ItemGroup>
  <!-- Files must have the .json extension -->
  <AdditionalFiles Include="products.json" />
</ItemGroup>
```
3. You can now use the generated classes in your code. Add a using statement for `[your_dll_name_without_extension].Json`. E.g.:
```csharp
using MyCompany.MyProject.Json;
```

# Example usage

## Use a json file to generate classes

[![Json configuration feature example](Media/jsonbyexample_simple.gif)]

Given the following `products.json` file:
```json
[
  {
    "id": 12,
    "name": "Example product",
    "colorVariants": [
      {
        "variantId": 12,
        "color": "Red"
      },
      {
        "variantId": 10,
        "color": "Green"
      }
    ]
  }
]
```

You can then use the generated code as follows:

```csharp
var product = new Product()
    {
        Id = 16,
        Name = "Violin"
        ColorVariants = new List<ColorVariant>()
        {
            new ColorVariant()
            {
                VariantId = 17,
                Color = "Blue"
            }
        }
    };
```

## Get json configuration without the need for magic strings

[![Json configuration feature example](Media/jsonbyexample_config.gif)]

If you are using json configuration providers, you can do the following:

1. Ensure that the following NuGet packages are installed: `Microsoft.Extensions.Configuration.Json` and `Microsoft.Extensions.Configuration.Binder`.
2. Ensure that the `appsettings.json` (or any other configuration files) are included in the compilation as `AdditionalFiles` (as mentioned in the installation instructions). A typical example from your project file would look like this:
```xml
<AdditionalFiles Include="appsettings.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</AdditionalFiles>
``` 

Now, given the following configuration file:
```json
{
  "AppSettings": {
    "exampleSetting": "example value"
  }
}
```

You would normally do this:
```csharp
// outputs "example value"
config.GetSection("Something").GetSection("SomeValue").Value
```
```csharp
// outputs "example value"
Appsetting.FromConfig(config).Something.SomeValue
```