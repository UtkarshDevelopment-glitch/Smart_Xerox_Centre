using System;
using System.Collections.Generic;

namespace Smart_Xerox_Centre.Models;

public partial class OrderFile
{
    public long OrderFileId { get; set; }

    public long OrderId { get; set; }

    public string OriginalFileName { get; set; } = null!;

    public string StoredFileName { get; set; } = null!;

    public string StoragePath { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public string FileExtension { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    public int? PageCount { get; set; }

    public string UploadStatus { get; set; } = null!;

    public DateTime UploadedAtUtc { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<PrintSetting> PrintSettings { get; set; } = new List<PrintSetting>();
}
