#nullable disable
using System.ComponentModel.DataAnnotations;

namespace PluginBuilder.ViewModels;

public class CreateBuildViewModel
{
    [Required(AllowEmptyStrings = false)]
    [Display(Name = "Git repository")]
    public string GitRepository { get; set; }

    [Display(Name = "Git branch or tag (optional)")]
    public string GitRef { get; set; }

    [Display(Name = "Directory to the plugin's project (optional)")]
    public string PluginDirectory { get; set; }

    [Display(Name = "Dotnet build configuration (optional)")]
    public string BuildConfig { get; set; }

    public PluginBuildParameters ToBuildParameter()
    {
        return new PluginBuildParameters(Normalize(GitRepository))
        {
            BuildConfig = BuildConfig,
            GitRef = GitRef,
            PluginDirectory = PluginDirectory is null ? null : Normalize(PluginDirectory)
        };
    }

    private static string Normalize(string str)
    {
        ArgumentNullException.ThrowIfNull(str);
        str = str.Trim();
        // Strip trailing /
        if (str.EndsWith('/'))
            str = str.Substring(0, str.Length - 1);
        return str;
    }
}
