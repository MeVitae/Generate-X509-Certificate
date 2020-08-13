# Generate X509 Certificate

[![MeVitae](https://raw.githubusercontent.com/MeVitae/Generate-X509-Certificate/master/Resources/Github%20Powered%20By%20MeVitae.png)](https://www.mevitae.com/)

Create certificates using powershell core 7 and .net core 3.1 for IdentityServer/Windows store apps etc

Certificates made using powershell 7 seem to export null private key, hence we use wincompat sessions to export
https://github.com/PowerShell/WindowsCompatibility/issues/76

This app is based on
https://docs.microsoft.com/en-gb/archive/blogs/kaevans/using-powershell-with-certificates
