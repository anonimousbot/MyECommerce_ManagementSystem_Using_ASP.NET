# Deploying EMS To Render

This project is now set up for Docker-based deployment on Render.

## Files added

- `Dockerfile`
- `.dockerignore`
- `render.yaml`

## What changed in the app

- The app now honors Render's `PORT` environment variable.
- Forwarded proxy headers are enabled so HTTPS works correctly behind Render.
- Upload storage can be moved out of `wwwroot/uploads` with `Storage:UploadsRoot`.
- MySQL connections now retry transient failures automatically.

## Required Render environment variables

Set these in the Render dashboard for your web service:

- `ConnectionStrings__EMSContext`
- `Google__ClientId`
- `Google__ClientSecret`
- `Paystack__SecretKey`
- `Paystack__PublicKey`
- `ASPNETCORE_ENVIRONMENT=Production`
- `Storage__UploadsRoot=/var/data/uploads`

## Recommended Render service settings

- Runtime: `Docker`
- Health check path: `/health`
- Persistent disk mount path: `/var/data`

## Important app-specific setup

### 1. Google login

Update your Google OAuth redirect URI to:

```text
https://YOUR-RENDER-SERVICE.onrender.com/signin-google
```

### 2. Paystack callback

This app builds the Paystack callback URL from the incoming request. With forwarded headers enabled, the production callback will use the Render HTTPS URL correctly.

### 3. Uploads and images

Item images should be stored on the Render disk using `Storage__UploadsRoot=/var/data/uploads`.

Without a persistent disk, uploaded files can disappear when the container restarts or redeploys.

## Deploy flow

1. Push this repository to GitHub.
2. In Render, create a new Web Service from the repo.
3. Choose the `Docker` runtime, or let Render detect `render.yaml`.
4. Add the required environment variables in the Render dashboard.
5. Confirm the persistent disk is attached at `/var/data`.
6. Deploy the service.
7. Visit `/health` after deploy to confirm the app and database are healthy.

## Notes

- The app runs EF Core migrations on startup, so the target database schema will be updated automatically.
- If secrets remain in committed config files, rotate them and move to Render environment variables only.
