# JsonByExampleGenerator

[![Twitter URL](https://img.shields.io/twitter/url?style=social&url=https%3A%2F%2Ftwitter.com%2Fknifecore%2F)](https://twitter.com/knifecore)

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
                };

product.ColorVariants.Add(new ColorVariant()
    {
        VariantId = 17,
        Color = "Blue"
    });
```