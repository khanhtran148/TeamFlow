using System.Text.RegularExpressions;

namespace TeamFlow.Domain.Entities;

public sealed class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public Guid? CreatedByUserId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Team> Teams { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
    public ICollection<OrganizationMember> Members { get; set; } = [];

    /// <summary>
    /// Generates a URL-safe slug from an organization name.
    /// Lowercase, spaces to hyphens, strips special characters, collapses multiple hyphens.
    /// </summary>
    public static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Lowercase and trim
        var slug = name.Trim().ToLowerInvariant();

        // Replace whitespace sequences with a single hyphen
        slug = Regex.Replace(slug, @"\s+", "-");

        // Remove any character that is not alphanumeric or hyphen
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", string.Empty);

        // Collapse multiple consecutive hyphens
        slug = Regex.Replace(slug, @"-{2,}", "-");

        // Trim leading/trailing hyphens
        slug = slug.Trim('-');

        return slug;
    }
}
