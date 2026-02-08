# Environment Setup Guide

## Backend Configuration (expensesBackend)

The backend uses `appsettings.Development.json` for development secrets. This file is gitignored.

### Setup Steps:

1. The `appsettings.Development.json` already exists with the following structure:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "Google": {
       "ClientId": "your-google-client-id",
       "ClientSecret": "your-google-client-secret"
     }
   }
   ```

2. Replace `your-google-client-id` and `your-google-client-secret` with your actual Google OAuth credentials from [Google Cloud Console](https://console.cloud.google.com/apis/credentials)

## Frontend Configuration (webapps)

The frontend uses `.env.local` for development secrets. This file is gitignored.

### Setup Steps:

1. Copy `.env.example` to `.env.local`:
   ```bash
   cd webapps
   cp .env.example .env.local
   ```

2. Edit `.env.local` and set your Google Client ID:
   ```
   VITE_GOOGLE_CLIENT_ID=your-google-client-id-here
   ```

## Getting Google OAuth Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com/apis/credentials)
2. Create a new OAuth 2.0 Client ID or use an existing one
3. Configure authorized origins and redirect URIs:
   - **Authorized JavaScript origins:** `http://localhost:3000`
   - **Authorized redirect URIs:** `http://localhost:3000`
4. Copy the Client ID and Client Secret
5. Add them to the respective configuration files as described above

## Security Notes

- **Never commit** `appsettings.Development.json` or `.env.local` files
- The `.gitignore` files are already configured to exclude these files
- Production secrets should be managed through secure environment variables or secret management services
