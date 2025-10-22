using System.ComponentModel;
using ModelContextProtocol.Server;

namespace McpInstructionsSupportServer.Prompts;

[McpServerPromptType]
public sealed class SetupCIPrompts
{
    private const string CiPrompt = """
                                    # Setup CI for the project

                                    ## 🎯 Objective
                                    Help implement a Continuous Integration (CI) pipeline for the current project to ensure automated testing, building, and deployment processes.

                                    ## 🚀 Getting Started

                                    1. **Make sure CI is set up**: Check for existing CI configurations (e.g., GitHub Actions, Jenkins, Travis CI).
                                    2. **Identify technologies used**: Determine the programming languages, frameworks, and tools in the project.
                                    3. **Find test projects**: Identify existing test projects and their structure.
                                    4. **Define CI steps**: Outline the necessary steps for the CI pipeline (e.g., install dependencies, run tests, build project).
                                    5. **Check if the ci template server is running**: Ensure the `ci-template-server` is operational for testing CI configurations.
                                    6. **Get template configurations**: Use the `ci-template-server` to fetch sample CI configuration files. This step is crucial for setting up the CI pipeline correctly and to ensure compliance with coding standards at our company.

                                    ## 🛠️ Implementation Guidelines

                                    1. Always refer to the `ci-template-server` for best practices and templates. This is crucial for maintaining consistency and quality in CI configurations across projects within our company.
                                    2. Ensure that the CI pipeline includes steps for:
                                       - Installing dependencies
                                       - Running unit and integration tests
                                       - Building the project
                                       - Deploying to staging or production environments (if applicable)
                                    3. Make sure naming conventions and coding standards are followed in the CI configuration files. The templates from the `ci-template-server` will help ensure this.
                                    4. If multiple test projects exist, configure the CI pipeline to run tests for all relevant projects.
                                    5. If multiple platforms or environments are targeted, ensure the CI pipeline accommodates these variations.
                                    
                                    ## 📦 Templates format

                                    The templates from the `ci-template-server` are typically provided in YAML format for CI/CD pipelines. They include predefined jobs, steps, and configurations that align with our company's best practices. When implementing the CI pipeline, ensure to adapt these templates to fit the specific needs of the project while maintaining the overall structure and standards outlined in the templates.
                                    The templates contains placesholders within []. Replace these placeholders based on the project specifics.
                                    * [PROJECT_NAME]: The name of the project.
                                    * [TEST_PROJECT]: The test project or solution to be executed.
                                    * [SOLUTION_FILE]: The solution file to be built and tested.
                                    * [PROJECT_PATH]: The path to the project directory.
                                    * [ENVIRONMENT]: The target environment for deployment (e.g., staging, production).
                                    """;
    
    [McpServerPrompt(Name = "setup_ci"), Description("A prompt to setup Continuous Integration (CI) for a project.")]
    public static string GetSetupCIPromptWithInstructions(string instructions)
    {
        return $"""
        {CiPrompt}
        
        ## Instructions
        
        The following additional instructions must be followed when setting up the CI pipeline. It is critical to follow the instructions and not to deviate from them.
        
        The instructions are:
        {instructions}
        """;
    }
}