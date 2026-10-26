namespace ContosoDashboard.Services;

public record FileScanResult(bool IsClean, string? Reason = null)
{
    public static FileScanResult Clean() => new(true);
    public static FileScanResult Rejected(string reason) => new(false, reason);
}

/// <summary>
/// Malware scanning abstraction. The training build uses a basic offline check; production can
/// swap in a real antivirus service via DI.
/// </summary>
public interface IFileScanService
{
    Task<FileScanResult> ScanAsync(Stream fileStream, string extension);
}

public class BasicFileScanService : IFileScanService
{
    private static readonly byte[] PdfSignature = "%PDF"u8.ToArray();
    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] JpegSignature = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] ZipSignature = { 0x50, 0x4B, 0x03, 0x04 };          // .docx/.xlsx/.pptx
    private static readonly byte[] OleSignature = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }; // .doc/.xls/.ppt

    private static readonly byte[][] ExecutableSignatures =
    {
        new byte[] { 0x4D, 0x5A },             // MZ (Windows PE)
        new byte[] { 0x7F, 0x45, 0x4C, 0x46 }, // ELF
        "#!"u8.ToArray()                       // Script shebang
    };

    public async Task<FileScanResult> ScanAsync(Stream fileStream, string extension)
    {
        try
        {
            var originalPosition = fileStream.Position;
            var header = new byte[512];
            var read = await fileStream.ReadAsync(header.AsMemory(0, header.Length));
            fileStream.Position = originalPosition;

            if (read == 0)
            {
                return FileScanResult.Rejected("The file is empty.");
            }

            var span = header.AsSpan(0, read);

            foreach (var signature in ExecutableSignatures)
            {
                if (span.StartsWith(signature))
                {
                    return FileScanResult.Rejected("The file contains executable content.");
                }
            }

            var matches = extension.ToLowerInvariant() switch
            {
                ".pdf" => span.StartsWith(PdfSignature),
                ".png" => span.StartsWith(PngSignature),
                ".jpg" or ".jpeg" => span.StartsWith(JpegSignature),
                ".docx" or ".xlsx" or ".pptx" => span.StartsWith(ZipSignature),
                ".doc" or ".xls" or ".ppt" => span.StartsWith(OleSignature),
                ".txt" => !span.Contains((byte)0), // Binary content in a text file is suspicious
                _ => false
            };

            return matches
                ? FileScanResult.Clean()
                : FileScanResult.Rejected("The file content does not match its file type.");
        }
        catch (Exception)
        {
            // If the scan cannot complete, the upload is rejected
            return FileScanResult.Rejected("The file could not be scanned.");
        }
    }
}
