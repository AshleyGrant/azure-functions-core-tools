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
        public static DeploymentV1Beta1 GenerateDeployment(string name, string @namespace, Secrets secrets, string image, int replicaCount)
        {
            return new DeploymentV1Beta1
            {
                ApiVersion = "apps/v1beta1",
                Kind = "Deployment",
                Metadata = new ObjectMetaV1
                {
                    Namespace = @namespace,
                    Name = name,
                    Labels = new Dictionary<string, string>
                    {
                        { "app", name }
                    },
                },
                Spec = new DeploymentSpecV1Beta1
                {
                    Replicas = replicaCount,
                    Selector = new LabelSelectorV1
                    {
                        MatchLabels = new Dictionary<string, string>
                        {
                            { "app",  name }
                        }
                    },
                    Template = new PodTemplateSpecV1
                    {
                        Metadata = new ObjectMetaV1
                        {
                            Labels = new Dictionary<string, string>
                            {
                                { "app", name }
                            }
                        },
                        Spec = new PodSpecV1
                        {
                            Containers = new List<ContainerV1>
                            {
                                new ContainerV1
                                {
                                    Name = name,
                                    Image = image,
                                    ImagePullPolicy = "Always",
                                    Env = secrets.data
                                        .Select(s => new EnvVarV1
                                        {
                                            Name = s.Key,
                                            ValueFrom = new EnvVarSourceV1
                                            {
                                                SecretKeyRef = new SecretKeySelectorV1
                                                {
                                                    Name = secrets.metadata.name,
                                                    Key = s.Key
                                                }
                                            }
                                        }).ToList()
                                }
                            }
                        }
                    }
                }
            };
        }

        public static ScaledObject GenerateScaledObject(string name, string @namespace, DeploymentV1Beta1 deployment)
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
                        deploymentName = deployment.Metadata.Name
                    },
                    triggers = triggers
                }
            };
        }

        internal static async Task<bool> HasKore()
        {
            var koreResult = await KubectlHelper.KubectlGet<KubernetesSearchResult<DeploymentV1Beta1>>("deployments --selector=app=kore-edgess --all-namespaces");
            return koreResult.items.Any();
        }

        internal static async Task<bool> HasScaledObjectCrd()
        {
            var crdResult = await KubectlHelper.KubectlGet<KubernetesSearchResult<CustomResourceDefinitionV1Beta1>>("crd");
            return crdResult.items.Any(i => i.Metadata.Name == "scaledobjects.kore.k8s.io");
        }

        public static async Task CreateKore()
        {

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