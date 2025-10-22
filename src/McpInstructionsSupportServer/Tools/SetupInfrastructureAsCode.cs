using System.ComponentModel;
using ModelContextProtocol.Server;

namespace McpInstructionsSupportServer.Tools;

[McpServerToolType]
public sealed class SetupInfrastructureAsCode
{
    private const string GitActionsStructure = """
                                               .github/workflows/
                                               ├── ci-build-test-pr.yml  # CI workflow for Pull Requests
                                               ├── ci-build-main.yml     # CI workflow for main branch 
                                               ├── ci-code-coverage.yml  # CI workflow for code coverage        
                                               ├── deploy.yml            # CD workflow for deployments
                                               """;

    [McpServerTool, Description("Gets the Git Actions structure for setting up a build pipelines.")]
    public static async Task<string> GetGitActionsStructure(ILogger<SetupBuildPipelineTool> logger)
    {
        logger.LogInformation("Get Git Actions Structure called");

        return GitActionsStructure;
    }

    [McpServerTool, Description("Gets the CI naming conventions for all projects.")]
    public static async Task<string> GetCINamingConventions(ILogger<SetupBuildPipelineTool> logger)
    {
        logger.LogInformation("Get Git Actions Structure called");

        return "All projects should follow the following structure: CI -> Test -> Staging -> Production";
    }

    private const string DotNetBuildTestTemplate = """
                                                   build-dotnet:
                                                     name: Build [PROJECT_NAME] 
                                                     runs-on: ubuntu-latest

                                                     steps:
                                                       - uses: actions/checkout@08c6903cd8c0fde910a37f88322edcfb5dd907a8 # v5.0.0

                                                       - name: Setup .NET
                                                         uses: actions/setup-dotnet@d4c94342e560b34958eacfc5d055d21461ed1c5d # v5.0.0
                                                         with:
                                                           dotnet-version: |
                                                             9.0.x

                                                       - name: Restore dependencies
                                                         run: dotnet restore [PROJECT_NAME]

                                                       - name: Build
                                                         run: dotnet build [PROJECT_NAME] --no-restore --configuration Release

                                                       - name: Test
                                                         run: dotnet test [TEST_PROJECT] --no-build --configuration Release --logger "trx;LogFileName=testresults.trx" --results-directory ./TestResults --collect:"XPlat Code Coverage"

                                                       - name: Upload test results
                                                         uses: actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02 # v4.6.2
                                                         with:
                                                           name: testresults-${{ github.run_number }}-${{ github.run_id }}
                                                   """;

    private const string NodeBuildTemplate = """
                                             build-node:
                                               name: Build [PROJECT_NAME] 
                                               runs-on: ubuntu-latest

                                               steps:
                                               - name: Checkout code
                                                 uses: actions/checkout@v4

                                               - name: Setup Node.js
                                                 uses: actions/setup-node@v4
                                                 with:
                                                   node-version: '20'

                                               - name: Install dependencies
                                                 run: npm ci
                                                 working-directory: [PROJECT_PATH]

                                               - name: Lint frontend code
                                                 run: npm run lint
                                                 working-directory: [PROJECT_PATH]

                                               - name: Build frontend
                                                 run: npm run build
                                                 working-directory: [PROJECT_PATH]
                                             """;

    private const string DockerBuildTemplate = """
                                               docker-build:
                                               name: Docker Build [PROJECT_NAME]
                                               runs-on: ubuntu-latest

                                               steps:
                                               - name: Checkout code
                                                 uses: actions/checkout@v4

                                               - name: Set up Docker Buildx
                                                 uses: docker/setup-buildx-action@v3

                                               - name: Build [PROJECT_NAME] Docker image
                                                 uses: docker/build-push-action@v5
                                                 with:
                                                   context: ./[PROJECT_PATH]
                                                   file: ./[PROJECT_PATH]/Dockerfile
                                                   push: false
                                                   tags: [PROJECT_NAME]:{{ github.sha }}
                                               """;
    

    [McpServerTool, Description("Gets the Continues Integration (CI) build and test template")]
    public static async Task<string> GetCIBuildTemplate(ILogger<SetupBuildPipelineTool> logger,
        [Description("The framework stack. Allowed stacks, .NET, Node and Docker")]
        string stack)
    {
        logger.LogInformation("Get CI Build Template called for stack: {Stack}", stack);

        return stack.ToLowerInvariant() switch
        {
            "node" => NodeBuildTemplate,
            "docker" => DockerBuildTemplate,
            ".net" or "dotnet" => DotNetBuildTestTemplate,
            _ => throw new ArgumentException($"Unsupported stack: {stack}. Supported stacks are .NET, Node, and Docker.")
        };
    }
}