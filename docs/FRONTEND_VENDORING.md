# Frontend Vendor Governance

Edulytics vendors browser libraries under
`src/Edulytics.Web/wwwroot/lib`.

## Current versions

| Library | Version | Evidence |
| --- | ---: | --- |
| Bootstrap | 5.3.3 | `bootstrap/dist/css/bootstrap.css` banner |
| jQuery | 3.7.1 | `jquery/dist/jquery.js` banner |
| jQuery Validation | 1.21.0 | `jquery-validation/dist/jquery.validate.js` banner |
| jQuery Validation Unobtrusive | 4.0.0 | `jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.js` banner |
| ASP.NET Core SignalR JS client | 10.0.11 | `signalr/VERSION.txt` |

## Governance

Vendored browser libraries are independent of normal NuGet update flow.

For each vendor upgrade:

1. verify the upstream version and license;
2. review relevant security advisories;
3. replace normal and minified assets consistently;
4. run dependency and security gates;
5. run EN/PL browser regression;
6. verify CSP compatibility;
7. record the exact version change in the pull request.

Review vendored browser dependencies during dependency/security maintenance
and immediately when a relevant security advisory is issued.

Do not silently replace vendored files from an unverified CDN or copy.
