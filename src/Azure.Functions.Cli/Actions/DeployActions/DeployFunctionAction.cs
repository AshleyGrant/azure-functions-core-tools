using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Functions.Cli.Actions.DeployActions.Platforms;
using Azure.Functions.Cli.Common;
using Azure.Functions.Cli.Extensions;
using Azure.Functions.Cli.Helpers;
using Azure.Functions.Cli.Interfaces;
using Colors.Net;
using Fclp;
using Microsoft.Azure.WebJobs.Script;
using static Azure.Functions.Cli.Common.OutputTheme;
using static Colors.Net.StringStaticMethods;

namespace Azure.Functions.Cli.Actions.LocalActions
{
    [Action(Name = "deploy", HelpText = "Deploy a function app to custom hosting backends")]
    [Action(Name = "deploy", Context = Context.Kubernetes, HelpText = "Deploy a function app to custom hosting backends")]
    internal class DeployAction : BaseAction
    {
        public string Registry { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
        public string ImageName { get; set; } = string.Empty;
        public string SerializingFormat { get; set; } = string.Empty;
        public bool IsDryRun { get; set; }

        private readonly IEnumerable<string> _platforms = new[] { "kubernetes", "knative" };
        private readonly ISecretsManager _secretsManager;
        private IEnumerable<string> _serializingOptions = new[] { "json", "yaml" };

        public DeployAction(ISecretsManager secretsManager)
        {
            _secretsManager = secretsManager;
        }

        public override ICommandLineParserResult ParseArgs(string[] args)
        {
            Parser
                .Setup<string>("platform")
                .WithDescription($"Hosting platform for the function app. Valid options: {string.Join(",", _platforms)}")
                .Callback(t => Platform = t)
                .Required();

            Parser
                .Setup<string>('n', "name")
                .WithDescription("Name for the image and deployment to build")
                .Callback(t => Name = t)
                .Required();

            Parser
                .Setup<string>("registry")
                .WithDescription("A Docker Registry name that you are logged into. Only needed if no image-name is provided.")
                .Callback(t => Registry = t);

            Parser
                .Setup<string>("image-name")
                .WithDescription("Fully qualified image name to use for deployment.")
                .Callback(n => ImageName = n);

            Parser
                .Setup<string>('o', "output")
                .WithDescription($"Serialize deployment specifications to stdout. Only used with --dry-run. Options: {string.Join(",", _serializingOptions)}")
                .SetDefault(_serializingOptions.First())
                .Callback(o => SerializingFormat = o);

            Parser
                .Setup<bool>("dry-run")
                .WithDescription("Only print the deployment specification. Don't run the actual deployment")
                .Callback(f => IsDryRun = f);

            if (args.Any() && !args.First().StartsWith("-"))
            {
                FolderName = args.First();
            }

            return base.ParseArgs(args);
        }

        public override async Task RunAsync()
        {
            if (!string.IsNullOrEmpty(FolderName))
            {
                var folderPath = Path.Combine(Environment.CurrentDirectory, FolderName);
                FileSystemHelpers.EnsureDirectory(folderPath);
                Environment.CurrentDirectory = folderPath;
            }

            if (!_platforms.Contains(Platform))
            {
                throw new CliException($"platform {Platform} is not supported. Valid options are: {string.Join(",", _platforms)}");
            }

            if (!CommandChecker.CommandExists("kubectl"))
            {
                throw new CliException($"kubectl is required for deploying to kubernetes and knative. Please make sure to install kubectl and try again.");
            }

            var imageName = ImageName;

            if (string.IsNullOrEmpty(imageName))
            {
                if (string.IsNullOrEmpty(Registry))
                {
                    throw new CliException("a --registry is required if no --image-name is specified. Please either specify an image-name or a registry.");
                }

                imageName = $"{Registry}/{Name.SanitizeImageName()}";
                await BuildDockerImage(imageName);
            }

            var platform = PlatformFactory.CreatePlatform(Platform, _secretsManager);

            if (platform == null)
            {
                throw new CliException($"Platform {Platform} is not supported");
            }

            if (IsDryRun)
            {
                await platform.SerializeDeployment(Name, imageName, SerializingFormat);
            }
            else
            {
                await platform.Deploy(Name, imageName);
            }
        }

        public static async Task BuildDockerImage(string imageName)
        {
            var dockerFilePath = Path.Combine(Environment.CurrentDirectory, "Dockerfile");

            if (!FileSystemHelpers.FileExists(dockerFilePath))
            {
                throw new CliException($"Dockerfile not found in directory {Environment.CurrentDirectory}." + Environment.NewLine +
                                        "Try running \"func init . --docker-only\", add a Dockerfile, or provide --image-name.");
            }

            ColoredConsole.WriteLine("Building Docker image...");
            await DockerHelpers.DockerBuild(imageName, Environment.CurrentDirectory);

            ColoredConsole.WriteLine("Pushing function image to registry...");
            await DockerHelpers.DockerPush(imageName);
        }
    }
}
