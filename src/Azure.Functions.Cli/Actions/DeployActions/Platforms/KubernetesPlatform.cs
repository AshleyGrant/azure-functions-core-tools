using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Functions.Cli.Helpers;
using Azure.Functions.Cli.Interfaces;
using Colors.Net;
using KubeClient.Models;
using Azure.Functions.Cli.Common;
using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Azure.Functions.Cli.Kubernetes;
using Azure.Functions.Cli.Kubernetes.Models;
using YamlDotNet.Serialization;

namespace Azure.Functions.Cli.Actions.DeployActions.Platforms
{
    public class KubernetesPlatform : IHostingPlatform
    {
        private const string FUNCTIONS_NAMESPACE = "azure-functions";
        private readonly ISecretsManager _secretsManager;

        public KubernetesPlatform(ISecretsManager secretsManager)
        {
            _secretsManager = secretsManager;
        }

        public void SerializeDeployment(string name, string imageName, string serializationFormat)
        {
            (var secrets, var deployment, var scaledObject) = Build(name, imageName);
            var seperator = string.Equals(serializationFormat, "yaml", StringComparison.OrdinalIgnoreCase)
                ? "---"
                : string.Empty;

            ColoredConsole.WriteLine(serialize(secrets));
            ColoredConsole.WriteLine(seperator);
            ColoredConsole.WriteLine(serialize(deployment));
            ColoredConsole.WriteLine(seperator);
            ColoredConsole.WriteLine(serialize(scaledObject));

            string serialize(object obj)
            {
                if (string.Equals(serializationFormat, "yaml", StringComparison.OrdinalIgnoreCase))
                {
                    var yaml = new SerializerBuilder().DisableAliases().EmitDefaults().Build();
                    var writer = new StringWriter();
                    yaml.Serialize(writer, obj);
                    return writer.ToString();
                }
                else
                {
                    return JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented,
                        new Newtonsoft.Json.JsonSerializerSettings
                        {
                            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                        });
                }
            }
        }

        public async Task Deploy(string name, string imageName)
        {
            (var secrets, var deployment, var scaledObject) = Build(name, imageName);
            await CreateNamespace();
            await KubectlHelper.KubectlCreate(secrets, showOutput: true);
            await KubectlHelper.KubectlCreate(deployment, showOutput: true);
            if (await KubernetesHelper.HasScaledObjectCrd() && await KubernetesHelper.HasKore())
            {
                await KubectlHelper.KubectlCreate(scaledObject, showOutput: true);
            }
            else
            {
                ColoredConsole
                    .WriteLine("It looks like you don't have Kore")
                    .WriteLine("You can install kore to your kubernetes cluster");
            }
        }

        private (Secrets, Deployment, ScaledObject) Build(string name, string imageName)
        {
            var secrets = KubernetesHelper.GenerateSecrets($"{name}-secrets", FUNCTIONS_NAMESPACE, _secretsManager);
            var deployment = KubernetesHelper.GenerateDeployment($"{name}-deployment", FUNCTIONS_NAMESPACE, secrets, imageName, 1);
            var scaledObject = KubernetesHelper.GenerateScaledObject($"{name}-scaledobject", FUNCTIONS_NAMESPACE, deployment);
            return (secrets, deployment, scaledObject);
        }

        private async Task CreateNamespace()
        {
            await KubectlHelper.RunKubectl($"create ns {FUNCTIONS_NAMESPACE}", ignoreError: true, showOutput: false);
        }
    }
}