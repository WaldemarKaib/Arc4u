using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Arc4u.Security.Cryptography;

[Export(typeof(IX509CertificateLoader))]
public class X509CertificateLoader : IX509CertificateLoader
{
    public X509CertificateLoader(ILogger<X509CertificateLoader> logger)
    {
        _logger = logger;
    }

    private readonly ILogger<X509CertificateLoader>? _logger;
    /// <summary>
    /// Find a certificate in the system: Certificate store for Windows or Keychain...
    /// </summary>
    /// <param name="find">The certificate criteria to look for...</param>
    /// <param name="findType"><see cref="X509FindType"/>, default is FindBySubjectName</param>
    /// <param name="location"><see cref="StoreLocation"/>, default is LocalMachine</param>
    /// <param name="name"><see cref="StoreName"/>, default is My</param>
    /// <returns>The <see cref="X509Certificate2"/></returns>
    /// <exception cref="KeyNotFoundException">No certificate is found!</exception>
    public X509Certificate2 FindCertificate(string find, X509FindType findType = X509FindType.FindBySubjectName, StoreLocation location = StoreLocation.LocalMachine, StoreName name = StoreName.My)
    {
        var certificateStore = new X509Store(name, location);

        try
        {
            certificateStore.Open(OpenFlags.ReadOnly);

            var certificates = certificateStore.Certificates.Find(findType, find, false);

            return certificates.Count > 0 ? certificates[0] : throw new KeyNotFoundException("No certificate found for the given criteria.");
        }
        finally
        {
            certificateStore.Close();
        }
    }

    /// <summary>
    /// Just a wrapper class to encapsultate the search of a certificate in the system based on the OS.
    /// </summary>
    /// <param name="certificateInfo"></param>
    /// <returns></returns>
    public X509Certificate2 FindCertificate(CertificateInfo certificateInfo)
    {
        var certificate = FindCertificate(
                            certificateInfo.Name,
                            certificateInfo.FindType,
                            certificateInfo.Location,
                            certificateInfo.StoreName);

        return certificate;
    }

    /// <summary>
    /// Read the current section and identify if the section contains a CertificateStore entry.
    /// If yes, the Certificate will be retrieve based on the <see cref="CertificateInfo"/> object.
#if NET6_0_OR_GREATER
    /// If no CertificateStore section exists, the File is checked and a certificate will be created bqsed
    /// on the pem files (private and public keys).
#endif
    /// </summary>
    /// <param name="configuration"><see cref="IConfiguration"/></param>
    /// <param name="sectionName">The name of the section if the <see cref="IConfiguration"/></param>
    /// <returns></returns>
    public X509Certificate2? FindCertificate(IConfiguration configuration, string sectionName)
    {
        var certSectionPath = $"{sectionName}:CertificateStore";
        var certificate = configuration.GetSection(certSectionPath).Get<CertificateInfo>();

#if NET6_0_OR_GREATER
        // For this configuration, no decryption exists. Simply skip this provider.
        if (certificate is null)
        {
            // Do we have pem files?
            certSectionPath = $"{sectionName}:File";
            if (configuration.GetSection(certSectionPath).Exists())
            {
                var cert = configuration.GetSection(certSectionPath).Get<CertificateFilePathInfo>();

                if (cert is not null)
                {
                    if (!File.Exists(cert.Cert))
                    {
                        _logger?.Technical().LogError($"Public key file doesn't exist.");
                        return null;
                    }

                    if (!File.Exists(cert.Key))
                    {
                        _logger?.Technical().LogError($"Private key file doesn't exist.");
                        return null;
                    }

                    return X509Certificate2.CreateFromPemFile(cert.Cert, cert.Key);
                }
            }

            return null;
        }
#endif
        // The FindCertificate(tempRoot, certificate) is not used because the method throws an exception if no section is defined!
        return FindCertificate(certificate);
    }
}
