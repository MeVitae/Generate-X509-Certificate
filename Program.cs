using Colorful;
using GenerateCertificate.Helpers;
using GenerateCertificate.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Console = Colorful.Console;

namespace GenerateCertificate
{
    class Program
    {
        static void GenerateCreateInfo((X509Certificate2 certificate, string publicCertFile, string privateCertFile) certificateResult, string passwordFileResult)
        {
            Console.WriteLine();

            Console.WriteLine("-------------------------------------------------------------------------");
            Console.WriteLine("Generated Info");
            Console.WriteLine("Certificate Name ");
            Console.WriteLine("---" + StaticFields.CertificateName);
            Console.WriteLine("Certificate Password ");
            Console.WriteLine("---" + StaticFields.CertificatePassword);
            Console.WriteLine("Certificate Thumbprint ");
            Console.WriteLine("---" + certificateResult.certificate.Thumbprint);
            Console.WriteLine("Certificate Public Cert File ");
            Console.WriteLine("---" + certificateResult.publicCertFile);
            Console.WriteLine("Certificate Private Cert File ");
            Console.WriteLine("---" + certificateResult.privateCertFile);
            Console.WriteLine("Certificate Password File ");
            Console.WriteLine("---" + passwordFileResult);
            Console.WriteLine("-------------------------------------------------------------------------");
            Console.WriteLine();
        }

        static void Create(List<X509Certificate2> certificates, ref bool collectCertificates)
        {
            string requestedCertificateName = "";
            while (string.IsNullOrEmpty(requestedCertificateName))
            {
                Console.Write("Please enter the certificate name: ");
                requestedCertificateName = Console.ReadLine();
            }

            var searchCertificateName = StaticFields.certificatePrefix + requestedCertificateName;

            var foundCertificate = certificates.FirstOrDefault(m => m.Subject.ToLower().Trim() ==
                    searchCertificateName.ToLower().Trim());

            if (foundCertificate == null)
            {
                StaticFields.CertificateName = requestedCertificateName;

                var binFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

                var certificatesFolder = Directory.CreateDirectory(Path.Combine(binFolder, StaticFields.CertificatesFolder));

                var passwordFileResult = StaticMethods.CreatePasswordFile(certificatesFolder, 128);

                // Currently using wincompat
                var certificateResult = StaticMethods.CreateCertificate(certificatesFolder, passwordFileResult, useWinCompat: true);

                if (certificateResult.certificate != null)
                {
                    GenerateCreateInfo(certificateResult, passwordFileResult);

                    collectCertificates = true;
                }
                else Console.WriteLine("Certificate creation failed");
            }
            else
            {
                Console.WriteLine("Certificate already exists with the same name and the thumbprint is " +
                    Environment.NewLine + foundCertificate.Thumbprint + Environment.NewLine);
                collectCertificates = false;
            }
        }

        static void Delete(List<X509Certificate2> certificates, ref bool collectCertificates)
        {
            string deleteThumbprint = "";
            while (string.IsNullOrEmpty(deleteThumbprint))
            {
                Console.Write("Please enter the certificate thumbprint: ");
                deleteThumbprint = Console.ReadLine().Trim();
            }

            var foundCertificate = certificates.FirstOrDefault(m => m.Thumbprint.ToLower().Trim() ==
                deleteThumbprint.ToLower().Trim());

            if (foundCertificate != null)
            {
                StaticMethods.DeleteCertificate(deleteThumbprint);
                collectCertificates = true;
            }
            else
            {
                Console.WriteLine("Certificate does not exist " + Environment.NewLine);
                collectCertificates = false;
            }
        }

        static void Main(string[] args)
        {
            Console.WriteAscii("Certificate Generator");

            var collectCertificates = true;
            var certificates = new List<X509Certificate2>();

            while (true)
            {
                if (collectCertificates)
                    certificates = StaticMethods.GetCertificates(30);

                Console.Write("Create(c) or Delete(d) a Certificate and to Reset(r) (c/d/r): ");
                var selectedChar = Console.ReadKey();
                Console.WriteLine();

                if (selectedChar.Key == ConsoleKey.C)
                    Create(certificates, ref collectCertificates);
                else if (selectedChar.Key == ConsoleKey.D)
                    Delete(certificates, ref collectCertificates);
                else if (selectedChar.Key == ConsoleKey.R)
                {
                    Console.Clear();
                    collectCertificates = true;
                }
                else
                {
                    Console.WriteLine("Unrecognised input" + Environment.NewLine);
                    collectCertificates = false;
                }
            }
            
        }
    }
}
