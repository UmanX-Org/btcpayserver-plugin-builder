using PluginBuilder.DataModels;

namespace PluginBuilder.ViewModels.Admin;

public class PluginEditViewModel
{
    public string Slug { get; set; }
    public string Identifier { get; set; }
    public string Settings { get; set; }
    public PluginVisibilityEnum Visibility { get; set; }
}
