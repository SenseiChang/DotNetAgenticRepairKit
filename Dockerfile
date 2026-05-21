FROM mcr.microsoft.com/dotnet/sdk:9.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends git \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /workspace

COPY . .

RUN dotnet restore DotNetAgenticRepairKit.sln \
    && dotnet build DotNetAgenticRepairKit.sln --configuration Release --no-restore

ENTRYPOINT ["dotnet", "run", "--project", "src/RepairKit.Agent", "--"]
