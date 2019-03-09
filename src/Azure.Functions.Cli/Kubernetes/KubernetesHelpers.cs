using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Functions.Cli.Common;
using Azure.Functions.Cli.Interfaces;
using Azure.Functions.Cli.Kubernetes.Models;
using Colors.Net;
using KubeClient.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Azure.Functions.Cli.Common.OutputTheme;

namespace Azure.Functions.Cli.Kubernetes
{
    public static class KubernetesHelper
    {
        public static Deployment GenerateDeployment(string name, string @namespace, Secrets secrets, string image, int replicaCount)
        {
            var env = secrets.data
                .Select(s => new ContainerEnvironment
                {
                    name = s.Key,
                    valueFrom = new EnvironmentValueFrom
                    {
                        secretKeyRef = new ValueFromSecretKeyRef
                        {
                            name = secrets.metadata.name,
                            key = s.Key
                        }
                    }
                });

            var deployment = new Deployment();
            deployment.apiVersion = "apps/v1beta1";
            deployment.kind = "Deployment";

            var metadata = new Metadata();
            metadata.@namespace = @namespace;
            metadata.name = name;
            metadata.labels = new Labels();
            metadata.labels.app = name;

            deployment.metadata = metadata;
            deployment.spec = new DeploymentSpec();
            deployment.spec.replicas = replicaCount;
            deployment.spec.selector = new Selector();

            deployment.spec.selector.matchLabels = new MatchLabels();
            deployment.spec.selector.matchLabels.app = name;

            deployment.spec.template = new Models.Template();
            deployment.spec.template.metadata = new Metadata();
            deployment.spec.template.metadata.labels = new Labels();
            deployment.spec.template.metadata.labels.app = name;

            deployment.spec.template.spec = new TemplateSpec();
            deployment.spec.template.spec.containers = new List<Container>();
            deployment.spec.template.spec.containers.Add(new Container()
            {
                name = name,
                image = image,
                env = env
            });

            return deployment;
        }


        public static ScaledObject GenerateScaledObject(string name, string @namespace, Deployment deployment)
        {
            var functionJsonFiles = FileSystemHelpers
                    .GetDirectories(Environment.CurrentDirectory)
                    .Select(d => Path.Combine(d, "function.json"))
                    .Where(FileSystemHelpers.FileExists)
                    .Select(f => (filePath: f, content: FileSystemHelpers.ReadAllTextFromFile(f)));

            var functionsJsons = functionJsonFiles
                .Select(t => (filePath: t.filePath, jObject: JsonConvert.DeserializeObject<JObject>(t.content)))
                .Where(b => b.jObject["bindings"] != null);

            var triggers = functionsJsons
                .Select(b => b.jObject["bindings"])
                .SelectMany(i => i)
                .Where(b => b?["type"] != null)
                .Where(b => b["type"].ToString().IndexOf("Trigger", StringComparison.OrdinalIgnoreCase) != -1)
                .Select(t => new ScaledObjectTrigger
                {
                    type = t["type"].ToString() == "queueTrigger" ? "azure-queue" : t["type"].ToString(),
                    metadata = t.ToObject<Dictionary<string, string>>()
                });

            return new ScaledObject
            {
                apiVersion = "kore.k8s.io/v1alpha1",
                kind = "ScaledObject",
                metadata = new Metadata
                {
                    name = name,
                    @namespace = @namespace
                },
                spec = new ScaledObjectSpec
                {
                    scaleTargetRef = new ScaledObjectScaleTargetRef
                    {
                        deploymentName = deployment.metadata.name
                    },
                    triggers = triggers
                }
            };
        }

        internal static async Task<bool> HasKore()
        {
            var koreResult = await KubectlHelper.KubectlGet<KubernetesSearchResult<Deployment>>("deployments --selector=app=kore-edgess --all-namespaces");
            return koreResult.items.Any();
        }

        internal static async Task<bool> HasScaledObjectCrd()
        {
            var crdResult = await KubectlHelper.KubectlGet<KubernetesSearchResult<CustomResourceDefinitionV1Beta1>>("crd");
            return crdResult.items.Any(i => i.Metadata.Name == "scaledobjects.kore.k8s.io");
        }

        public static Secrets GenerateSecrets(string name, string @namespace, ISecretsManager secretsManager)
        {
            var secrets = secretsManager.GetSecrets()
                .ToDictionary(k => k.Key, v => Convert.ToBase64String(Encoding.UTF8.GetBytes(v.Value)));

            return new Secrets
            {
                apiVersion = "v1",
                kind = "Secret",
                metadata = new Metadata
                {
                    name = name,
                    @namespace = @namespace
                },
                data = secrets
            };
        }
    }
}