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

        internal static async Task RemoveKore(string @namespace)
        {
            await KubectlHelper.RunKubectl($"delete deployment.apps/kore-deployment --namespace {@namespace}", ignoreError: false, showOutput: true);
            await KubectlHelper.RunKubectl($"delete clusterrolebinding/kore-cluster-role-binding --namespace {@namespace}", ignoreError: true, showOutput: true);
            await KubectlHelper.RunKubectl($"delete serviceaccount/kore-service-account --namespace {@namespace}", ignoreError: false, showOutput: true);
            await KubectlHelper.RunKubectl($"delete secrets/kore-docker-auth --namespace {@namespace}", ignoreError: false, showOutput: true);
            await KubectlHelper.RunKubectl($"delete crd/scaledobjects.kore.k8s.io", ignoreError: false, showOutput: true);
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
                    pollingInterval = 5,
                    cooldownPeriod = 20,
                    triggers = triggers
                }
            };
        }

        internal static async Task<bool> HasKore()
        {
            var koreEdgeResult = await KubectlHelper.KubectlGet<KubernetesSearchResult<Deployment>>("deployments --selector=app=kore-edge --all-namespaces");
            var koreResult = await KubectlHelper.KubectlGet<KubernetesSearchResult<Deployment>>("deployments --selector=app=kore --all-namespaces");
            return koreResult.items.Any() || koreEdgeResult.items.Any();
        }

        internal static async Task<bool> HasScaledObjectCrd()
        {
            var crdResult = await KubectlHelper.KubectlGet<KubernetesSearchResult<CustomResourceDefinitionV1Beta1>>("crd");
            return crdResult.items.Any(i => i.Metadata.Name == "scaledobjects.kore.k8s.io");
        }

        public static async Task CreateKore(string @namespace)
        {
            // Create CRD
            await KubectlHelper.KubectlApply(@"apiVersion: apiextensions.k8s.io/v1beta1
kind: CustomResourceDefinition
metadata:
  name: scaledobjects.kore.k8s.io
  labels:
    app: kore
spec:
  group: kore.k8s.io
  version: v1alpha1
  names:
    kind: ScaledObject
    plural: scaledobjects
  scope: Namespaced", showOutput: true, ignoreError: true);

            await KubectlHelper.KubectlApply($@"apiVersion: v1
data:
  .dockerconfigjson: eyJhdXRocyI6eyJwcm9qZWN0a29yZS5henVyZWNyLmlvIjp7InVzZXJuYW1lIjoiYjUxNGI2MGMtNjhjYy00ZjEyLWIzNjEtMzg1ODg3OGIyNDc5IiwicGFzc3dvcmQiOiI0alg1dmtQVFNyVVE5NlVCYlUvQjdDUXJCb0p3VDYyV1NzNVdmWnRGYkI4PSIsImF1dGgiOiJZalV4TkdJMk1HTXROamhqWXkwMFpqRXlMV0l6TmpFdE16ZzFPRGczT0dJeU5EYzVPalJxV0RWMmExQlVVM0pWVVRrMlZVSmlWUzlDTjBOUmNrSnZTbmRVTmpKWFUzTTFWMlphZEVaaVFqZzkifX19
kind: Secret
metadata:
  labels:
    app: kore
  name: kore-docker-auth
  namespace: {@namespace}
type: kubernetes.io/dockerconfigjson", showOutput: false, ignoreError: true);

            // Create ServiceAccount
            await KubectlHelper.KubectlApply($@"apiVersion: v1
kind: ServiceAccount
metadata:
  labels:
    app: kore
    release: core-tools
  name: kore-service-account
  namespace: {@namespace}", showOutput: true, ignoreError: true);

            // Create ClusterRoleBinding
            await KubectlHelper.KubectlApply($@"apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  labels:
    app: kore
    release: core-tools
  name: kore-cluster-role-binding
  namespace: {@namespace}
roleRef:
  kind: ClusterRole
  name: cluster-admin
  apiGroup: rbac.authorization.k8s.io
subjects:
- kind: ServiceAccount
  name: kore-service-account
  namespace: {@namespace}", showOutput: true, ignoreError: true);

            // Create Deployment
            var deployment = $@"apiVersion: apps/v1
kind: Deployment
metadata:
  labels:
    app: kore
    release: core-tools
  name: kore-deployment
  namespace: {@namespace}
spec:
  replicas: 1
  selector:
    matchLabels:
      name: kore-deployment
      instance: kore-deployment-instance
  template:
    metadata:
      labels:
        name: kore-deployment
        instance: kore-deployment-instance
    spec:
      serviceAccountName: kore-service-account
      containers:
      - name: kore
        image: ""projectkore.azurecr.io/kore:ahmels-33-2""
        imagePullPolicy: Always
      imagePullSecrets:
      - name: kore-docker-auth";
            await KubectlHelper.KubectlApply(deployment, showOutput: true, ignoreError: true);
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