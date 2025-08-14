using Microsoft.Security.Extensions;
using YAAM.Core.Utils.Native;

namespace YAAM.Core.Utils;

internal static class SignatureChecker
{
    internal static bool IsMicrosoftSigned(string commandLinePath)
    {
        if (string.IsNullOrWhiteSpace(commandLinePath))
        {
            return false;
        }

        try
        {
            var filePath = CommandLineHelper.GetFileName(commandLinePath);

            return CheckSignature(filePath);
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckSignature(string fileName)
    {
        try
        {
            try
            {
                using var fs = File.OpenRead(fileName);
                return IsStreamMicrosoftSigned(fs);
            }
            catch (Exception ex)
                when (ex is FileNotFoundException or DirectoryNotFoundException or UnauthorizedAccessException)
            {
                using (new Wow64FsRedirection())
                using (var fs = File.OpenRead(fileName))
                {
                    return IsStreamMicrosoftSigned(fs);
                }
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool IsStreamMicrosoftSigned(FileStream fileStream)
    {
        var signatureInfo = FileSignatureInfo.GetFromFileStream(fileStream);
        return signatureInfo.State == SignatureState.SignedAndTrusted
               && signatureInfo.SigningCertificate?.IssuerName?.Name != null
               && signatureInfo.SigningCertificate.IssuerName.Name.Contains("CN=Microsoft");
    }
}
