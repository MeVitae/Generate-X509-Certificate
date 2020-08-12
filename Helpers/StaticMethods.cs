using GenerateCertificate.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace GenerateCertificate.Helpers
{
    public class StaticMethods
    {


        // https://referencesource.microsoft.com/#System.Web/Security/Membership.cs,fe744ec40cace139,references
        public static string GeneratePassword(int length, int numberOfNonAlphanumericCharacters)
        {
            if (length < 1 || length > 128)
            {
                throw new ArgumentException(nameof(length));
            }

            if (numberOfNonAlphanumericCharacters > length || numberOfNonAlphanumericCharacters < 0)
            {
                throw new ArgumentException(nameof(numberOfNonAlphanumericCharacters));
            }

            string password;
            int index;
            byte[] buf;
            char[] cBuf;
            int count;

            do
            {
                buf = new byte[length];
                cBuf = new char[length];
                count = 0;

                (new RNGCryptoServiceProvider()).GetBytes(buf);

                for (int iter = 0; iter < length; iter++)
                {
                    int i = (int)(buf[iter] % 87);
                    if (i < 10)
                        cBuf[iter] = (char)('0' + i);
                    else if (i < 36)
                        cBuf[iter] = (char)('A' + i - 10);
                    else if (i < 62)
                        cBuf[iter] = (char)('a' + i - 36);
                    else
                    {
                        cBuf[iter] = StaticFields.Punctuations[i - 62];
                        count++;
                    }
                }

                if (count < numberOfNonAlphanumericCharacters)
                {
                    int j, k;
                    Random rand = new Random();

                    for (j = 0; j < numberOfNonAlphanumericCharacters - count; j++)
                    {
                        do
                        {
                            k = rand.Next(0, length);
                        }
                        while (!Char.IsLetterOrDigit(cBuf[k]));

                        cBuf[k] = StaticFields.Punctuations[rand.Next(0, StaticFields.Punctuations.Length)];
                    }
                }

                password = new string(cBuf);
            }
            while (CrossSiteScriptingValidation.IsDangerousString(password, out index));

            return password;
        }

        public static SecureString ConvertToSecureString(string password)
        {
            if (password == null)
                throw new ArgumentNullException("password");

            var securePassword = new SecureString();

            foreach (char c in password)
                securePassword.AppendChar(c);

            securePassword.MakeReadOnly();
            return securePassword;
        }


        /// <summary>
        /// For Default, please set T as PSObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="scriptCommand"></param>
        /// <returns></returns>
        public static (List<T> returnValues, Runspace mainRunspace, Runspace remoteRunspace)  Run<T>(PSCommand command, bool useBaseObject = true,
            bool closeRunSpace = true, Runspace MainRunSpace = null, Runspace RemoteRunSpace = null)
        {
            List<T> returnValues = new List<T>();
            if (typeof(T) == typeof(PSObject))
                useBaseObject = false;

            Runspace runspace = null;

            if (MainRunSpace == null)
            {
                InitialSessionState initial = InitialSessionState.CreateDefault();
                initial.ThreadOptions = PSThreadOptions.UseNewThread;
                initial.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Unrestricted;
                MainRunSpace = RunspaceFactory.CreateRunspace(initial);
                MainRunSpace.Open();

                runspace = MainRunSpace;
            }
            else if (RemoteRunSpace != null)
                runspace = RemoteRunSpace;

            using PowerShell powerShell = PowerShell.Create(runspace);
            powerShell.Commands = command;
            // Execute PowerShell script
            var results = powerShell.Invoke();


            foreach (var error in powerShell.Streams.Error)
                Console.WriteLine(error.Exception.Message);

            foreach (var result in results)
            {
                try
                {
                    if (useBaseObject)
                        returnValues.Add((T)result.BaseObject);
                    //(T)Convert.ChangeType(result, typeof(T))
                    else
                    {
                        // Circular conversion
                        var serialiseItems = JsonConvert.SerializeObject(result.Properties.ToDictionary(k => k.Name, v => v.Value));
                        returnValues.Add(JsonConvert.DeserializeObject<T>(serialiseItems));
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            if (closeRunSpace && runspace != null)
                runspace.Close();


            return (returnValues: returnValues, 
                mainRunspace : MainRunSpace,
                remoteRunspace: RemoteRunSpace);
        }


        public static List<X509Certificate2> GetCertificates(int displaySizeLimit = 10)
        {
            Console.WriteLine("");
            Console.WriteLine("Collecting all Certificates (likely to take up to a minute)");
            Console.WriteLine("");

            var getCertificatesCommand = new PSCommand();
            getCertificatesCommand.AddScript(StaticFields.GetCertificates);

            var certificates = StaticMethods.Run<X509Certificate2>(getCertificatesCommand);

            foreach (var certificate in certificates.returnValues)
            {
                var displaySize = certificate.SubjectName.Name.Length > displaySizeLimit ? displaySizeLimit : certificate.SubjectName.Name.Length;
                Console.WriteLine($"Thumbprint {certificate.Thumbprint}");
                Console.WriteLine($"Subject {certificate.SubjectName.Name.Substring(0, displaySize)}");
                Console.WriteLine();
            }

            Console.WriteLine();

            return certificates.returnValues;
        }

        public static (Runspace mainRunspace, Runspace remoteRunspace, bool closeRunspace) GetWincompatRunspace()
        {
            // Powershell 7 exports signature without private key 
            // Current hack is to use powershell 5 through powershell 7 using 
            Console.WriteLine("Getting WinPSCompatSession ");
            var createPowerShell5Session = new PSCommand();
            createPowerShell5Session.AddScript(StaticFields.ImportPKI).AddParameter(StaticFields.PowerShell5WinSessionParameter);
            createPowerShell5Session.AddScript(StaticFields.GetPSSession);
            // Manually shutdownsession run space when done
            var winSession = StaticMethods.Run<PSSession>(createPowerShell5Session, true, false);
            if (winSession.returnValues?.Any() == false)
            {
                Console.WriteLine("No Session Detected");
                throw new Exception("Session creation failed");
            }

            return (mainRunspace: winSession.mainRunspace, remoteRunspace: winSession.returnValues[0].Runspace, false);
        }

        /// <summary>
        /// Powershell 7 exports certificate without private key 
        /// 
        /// </summary>
        /// <param name="certificatesFolder"></param>
        /// <param name="useWinCompat"></param>
        /// <returns></returns>
        public static (X509Certificate2 certificate, string publicCertFile, string privateCertFile) CreateCertificate(DirectoryInfo certificatesFolder, string passwordFile, bool useWinCompat = true)
        {
            X509Certificate2 certificate = null;
            Runspace mainRunspace = null, remoteRunspace = null;
            bool closeRunspace = true;

            if (useWinCompat)
            {
                var winRunspace = StaticMethods.GetWincompatRunspace();

                mainRunspace = winRunspace.mainRunspace;
                remoteRunspace = winRunspace.remoteRunspace;
                closeRunspace = winRunspace.closeRunspace;
            }

            Console.WriteLine("Creating certificate " + StaticFields.CertificateName);
            var createCertificateCommand = new PSCommand();
            createCertificateCommand.AddScript($"{StaticFields.CreateCertificate.Replace(StaticFields.ReplaceItem, StaticFields.CertificateName)}");
            var certificateResult = StaticMethods.Run<X509Certificate2>(createCertificateCommand, true, closeRunspace, mainRunspace, remoteRunspace);

            string publicCertFile = "", privateCertFile = "";
            if (certificateResult.returnValues?.Any() == true)
            {
                certificate = certificateResult.returnValues.FirstOrDefault();
                Console.WriteLine("Exporting public certificate " + certificate.Thumbprint);

                var exportPublicCertificateCommand = new PSCommand();
                exportPublicCertificateCommand.AddScript($"{StaticFields.GetCertificateVariable}{StaticFields.GetCertificates}\\{certificate.Thumbprint}");
                var publicCertificateFile = Path.Combine(certificatesFolder.FullName, StaticFields.PublicCertificateFile);
                exportPublicCertificateCommand.AddScript($"{StaticFields.ExportPublicCertificate.Replace(StaticFields.ReplaceItem, publicCertificateFile)}");
                var publicFileInfo = StaticMethods.Run<CustomFileInfoModels>(exportPublicCertificateCommand, false);
                if (publicFileInfo.returnValues?.Any() == true) publicCertFile = publicFileInfo.returnValues[0].FullName;

                Console.WriteLine("Exporting private certificate " + certificate.Thumbprint);

                var exportPrivateCertificateCommand = new PSCommand();
                exportPrivateCertificateCommand.AddScript($"{StaticFields.GetCertificateVariable}{StaticFields.GetCertificates}\\{certificate.Thumbprint}");
                exportPrivateCertificateCommand.AddScript($"{StaticFields.CreateSecurePassword.Replace(StaticFields.ReplaceItem, passwordFile)}");
                var privateCertificateFile = Path.Combine(certificatesFolder.FullName, StaticFields.PrivateCertificateFile);
                exportPrivateCertificateCommand.AddScript($"{StaticFields.ExportPrivateCertificate.Replace(StaticFields.ReplaceItem, privateCertificateFile)}");

                var privateFileInfo = StaticMethods.Run<CustomFileInfoModels>(exportPrivateCertificateCommand, false, closeRunspace, mainRunspace, remoteRunspace);
                if (privateFileInfo.returnValues?.Any() == true) privateCertFile = privateFileInfo.returnValues[0].FullName;
            }

            if(useWinCompat)
            {
                if (mainRunspace != null)
                    mainRunspace.Close();
                if (remoteRunspace != null)
                    remoteRunspace.Close();
            }

            return (certificate, publicCertFile, privateCertFile);
        }


        public static void DeleteCertificate(string deleteThumbprint)
        {
            Console.WriteLine("Deleting certificate " + deleteThumbprint);
            var deleteCertificateCommand = new PSCommand();
            deleteCertificateCommand.AddScript($"{StaticFields.DeleteCertificate.Replace(StaticFields.ReplaceItem, deleteThumbprint)}");
            StaticMethods.Run<PSObject>(deleteCertificateCommand);
        }

        public static string CreatePasswordFile(DirectoryInfo certificatesFolder, int passwordSize)
        {
            try
            {
                Console.WriteLine("Generating Password " + passwordSize);

                StaticFields.CertificatePassword = StaticMethods.GeneratePassword(passwordSize, 0);

                var securePassword = StaticMethods.ConvertToSecureString(StaticFields.CertificatePassword);
                StaticFields.CertificatePassword = new System.Net.NetworkCredential(string.Empty, securePassword).Password;
                string passwordFile = Path.Combine(certificatesFolder.FullName, StaticFields.PasswordFile);
                File.WriteAllText(passwordFile, StaticFields.CertificatePassword);

                return passwordFile;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
