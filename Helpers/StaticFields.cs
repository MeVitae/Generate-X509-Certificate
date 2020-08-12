using System;
using System.Collections.Generic;
using System.Text;

namespace GenerateCertificate.Helpers
{
    public class StaticFields
    {
        // Folders & Files
        public const string CertificatesFolder = @"Certificates";
        public const string PasswordFile = @"Password.txt";
        public const string PublicCertificateFile = @"PublicCertificate.cer";
        public const string PrivateCertificateFile = @"PrivateCertificate.pfx";

        // ApplicationInfo
        public const string certificatePrefix = @"CN=";
        public static string CertificateName = "TestCertificate";
        public static string CertificatePassword = "";


        // Random Password
        public static readonly char[] Punctuations = "!@#$%^&*()_-+=[{]};:>|./?".ToCharArray();

        // Powershell Scripts
        public const string ReplaceItem = @"REPLACEITEM";
        public const string ExecutionPolicy = @"Set-ExecutionPolicy Unrestricted";
        public const string GetPSSession = @"Get-PSSession -Name WinPSCompatSession";
        public const string PowerShell5WinSessionParameter = @"UseWindowsPowerShell";
        public const string ImportPKI = @"Import-Module PKI";
        public const string GetCertificateVariable = @"$cert = ";
        public const string GetCertificates = @"Get-ChildItem -Path cert:\CurrentUser\My";
        public const string RemoveCertificate = @"Remove-Item -Path (""cert:\CurrentUser\My\""" + ReplaceItem + ")";
        public const string CreateCertificate = @"New-SelfSignedCertificate -Subject """ + certificatePrefix + ReplaceItem  + @""" -CertStoreLocation cert:\CurrentUser\My -Provider ""Microsoft Strong Cryptographic Provider"" -KeyExportPolicy Exportable";
        public const string CreateSecurePassword = @"$password = Get-Content """ + ReplaceItem + @""" | ConvertTo-SecureString -AsPlainText -Force";
        public const string ExportPublicCertificate = @"Export-Certificate -Type CERT -Cert $cert -FilePath """ + ReplaceItem + @"""";
        public const string ExportPrivateCertificate = @"Export-PfxCertificate -Cert $cert -Password $password -FilePath """ + ReplaceItem + @"""";
        public const string DeleteCertificate = @"Remove-Item -Path (""cert:\CurrentUser\My\" + ReplaceItem + @""")";
    }
}
