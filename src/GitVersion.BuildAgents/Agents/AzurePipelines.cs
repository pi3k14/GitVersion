using System.IO.Abstractions;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class AzurePipelines(IEnvironment environment, ILog log, IFileSystem fileSystem) : BuildAgentBase(environment, log, fileSystem)
{
    public const string EnvironmentVariableName = "TF_BUILD";

    protected override string EnvironmentVariable => EnvironmentVariableName;

    public override string[] GenerateSetParameterMessage(string name, string? value) =>
    [
        $"##vso[task.setvariable variable=GitVersion.{name}]{value}",
        $"##vso[task.setvariable variable=GitVersion.{name};isOutput=true]{value}"
    ];

    public override string? GetCurrentBranch(bool usingDynamicRepos) => Environment.GetEnvironmentVariable("GIT_BRANCH")
        ?? Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCH");

    public override bool PreventFetch() => true;

    public override string GenerateSetVersionMessage(GitVersionVariables variables)
    {
        // For AzurePipelines, we'll get the Build Number and insert GitVersion variables where
        // specified
        var buildNumberEnv = Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER");
        if (buildNumberEnv.IsNullOrWhiteSpace())
            return variables.FullSemVer;

        var newBuildNumber = variables.OrderBy(x => x.Key).Aggregate(buildNumberEnv, ReplaceVariables);

        // If no variable substitution has happened, use FullSemVer
        if (buildNumberEnv == newBuildNumber)
        {
            var buildNumber = variables.FullSemVer.EndsWith("+0")
                ? variables.FullSemVer[..^2]
                : variables.FullSemVer;

            return $"##vso[build.updatebuildnumber]{buildNumber}";
        }

        return $"##vso[build.updatebuildnumber]{newBuildNumber}";
    }

    private static string ReplaceVariables(string buildNumberEnv, KeyValuePair<string, string?> variable)
    {
        var pattern = $@"\$\(GITVERSION[_\.]{variable.Key}\)";
        var replacement = variable.Value;
        return replacement switch
        {
            null => buildNumberEnv,
            _ => buildNumberEnv.RegexReplace(pattern, replacement)
        };
    }
}
