FROM mcr.microsoft.com/dotnet/sdk:5.0

WORKDIR /app/JsonByExampleGenerator.Generator
COPY ./JsonByExampleGenerator.Generator/JsonByExampleGenerator.Generator.csproj ./
RUN dotnet restore

WORKDIR /app/JsonByExampleGenerator.Example
COPY ./JsonByExampleGenerator.Example/JsonByExampleGenerator.Example.csproj ./
RUN dotnet restore

WORKDIR /app

COPY . ./

RUN dotnet build ./JsonByExampleGenerator.Example/JsonByExampleGenerator.Example.csproj --no-restore

CMD dotnet run --project ./JsonByExampleGenerator.Example/JsonByExampleGenerator.Example.csproj