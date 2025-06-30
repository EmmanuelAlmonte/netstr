# Netstr Setup Guide

## Database Configuration

This project uses Supabase PostgreSQL. You need to configure your database connection locally.

### Local Development Setup

1. **Create local configuration file:**
   ```bash
   cp src/Netstr/appsettings.local.json.example src/Netstr/appsettings.local.json
   ```

2. **Update your Supabase credentials in `appsettings.local.json`:**
   ```json
   {
     "ConnectionStrings": {
       "NetstrDatabase": "Host=db.YOUR-PROJECT-REF.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR-PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
     }
   }
   ```

3. **Get your Supabase connection details:**
   - Go to your Supabase project dashboard
   - Navigate to Settings → Database
   - Copy the connection string or individual components

### Production Deployment

Use environment variables for production:

```bash
export ConnectionStrings__NetstrDatabase="Host=db.YOUR-REF.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR-PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
```

### Docker Environment

In your `docker-compose.yml` or deployment:

```yaml
environment:
  - ConnectionStrings__NetstrDatabase=Host=db.YOUR-REF.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR-PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

### Security Notes

- Never commit `appsettings.local.json` to version control
- Use different databases for development/staging/production
- Consider using managed secrets in production (Azure Key Vault, AWS Secrets Manager, etc.)
- Rotate database passwords regularly

## Running the Application

1. Configure your database connection (see above)
2. Run the application:
   ```bash
   dotnet run --project src/Netstr
   ```
3. The application will automatically run Entity Framework migrations on startup
