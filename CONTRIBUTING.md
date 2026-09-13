# Contributing

Thanks for helping improve Game Touch Companion.

## Before opening a pull request

- Keep changes focused and avoid committing files under `bin/`, `obj/`, `artifacts/`, or local application data.
- Do not include WebView2 profiles, credentials, private URLs, logs, or signing material.
- Update permanent documentation when user-facing behavior changes.
- Run the normal checks from the repository root:

```powershell
dotnet restore
dotnet build -c Release -p:Platform=x64
dotnet test -c Release -p:Platform=x64 --filter "Category!=BrowserRuntime"
```

The CI workflow intentionally excludes tests tagged `BrowserRuntime`, which require a real WebView2 desktop environment. Hardware and physical touch checks must be validated separately.

Use a clear commit or pull request title, describe what changed and how it was tested, and call out any known limitations.
