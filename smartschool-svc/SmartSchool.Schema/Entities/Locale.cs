using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartSchool.Schema.Entities;

[Table("locale")]
public class Locale
{
    [Key, MaxLength(8)]
    public string Code { get; set; } = default!;

    [MaxLength(64)]
    public string Name { get; set; } = default!;

    [MaxLength(64)]
    public string? NativeName { get; set; }

    public bool IsRtl { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}

[Table("translation")]
public class Translation
{
    [Key]
    public long Id { get; set; }

    [ForeignKey(nameof(Locale)), MaxLength(8)]
    public string LocaleCode { get; set; } = default!;
    public Locale? Locale { get; set; }

    [MaxLength(64)]
    public string Namespace { get; set; } = "common";

    [MaxLength(256)]
    public string Key { get; set; } = default!;

    public string Value { get; set; } = default!;

    public DateTime? LastModifiedTime { get; set; }
}
