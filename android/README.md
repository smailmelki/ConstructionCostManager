# Android preview — Construction Cost Manager

This is a lightweight .NET MAUI Android WebView shell for the existing ASP.NET Core/Blazor application.

## Important architecture note

The Android app does **not** move the Blazor Server runtime or SQLite database onto the phone. It opens the existing web application hosted on a reachable server. The database remains on that server, so the Android client and web client use the same data.

- For a temporary LAN test, run the web project on the PC listening on `0.0.0.0:5000`, then enter the PC's Wi-Fi IPv4 address in the app, e.g. `http://192.168.1.10:5000`.
- For use away from the PC/network, deploy the web project to an HTTPS host and enter its HTTPS address.
- This preview permits HTTP traffic to support local testing. Before a production release, restrict cleartext traffic and use HTTPS only.

## Build the APK

Install the .NET 10 MAUI Android workload on a Windows machine with the Android SDK configured:

```powershell
dotnet workload install maui-android
dotnet restore .\android\ConstructionCostManager.Android\ConstructionCostManager.Android.csproj
dotnet build .\android\ConstructionCostManager.Android\ConstructionCostManager.Android.csproj -c Release
```

The Release APK is normally produced under:

`android\ConstructionCostManager.Android\bin\Release\net10.0-android\`

Check the exact generated APK filename in that folder.

This is a preview client; offline data entry and local SQLite synchronization are not implemented in this project.
