
# ==========================================
# STAGE 1: BUILD THE APPLICATION
# ==========================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy project definition first for better build caching.
COPY ["SchoolAdmission.Api/SchoolAdmission.Api.csproj", "SchoolAdmission.Api/"]

# Restore NuGet dependencies.
RUN dotnet restore "SchoolAdmission.Api/SchoolAdmission.Api.csproj"

# Copy remaining backend source files.
COPY . .

# Enter the .NET project directory.
WORKDIR /src/SchoolAdmission.Api

# Compile and publish the application.
RUN dotnet publish "SchoolAdmission.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# ==========================================
# STAGE 2: RUN THE APPLICATION
# ==========================================

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

# Copy only the published application.
COPY --from=build /app/publish .

# ASP.NET Core listens on port 8080.
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]
