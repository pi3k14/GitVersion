using Microsoft.Build.Framework;

namespace GitVersion.MsBuild.Tasks;

public class UpdateAssemblyInfo : GitVersionTaskBase
{
    [Required]
    public string ProjectFile { get; set; }

    [Required]
    public string IntermediateOutputPath { get; set; }

    [Required]
    public ITaskItem[] CompileFiles { get; set; } = [];

    [Required]
    public string Language { get; set; } = "C#";

    [Output]
    public string AssemblyInfoTempFilePath { get; set; }
}
