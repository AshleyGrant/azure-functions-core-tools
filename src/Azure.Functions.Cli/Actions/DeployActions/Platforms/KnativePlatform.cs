using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Functions.Cli.Common;
using Azure.Functions.Cli.Helpers;
using Azure.Functions.Cli.Interfaces;
using Colors.Net;
using KubeClient;
using KubeClient.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Azure.Functions.Cli.Kubernetes;
using Azure.Functions.Cli.Kubernetes.Models;

namespace Azure.Functions.Cli.Actions.DeployActions.Platforms
{
    public class KnativePlatform : IHostingPlatform
    {
        private string configFile = string.Empty;
        private const string FUNCTIONS_NAMESPACE = "azure-functions";
        private static KubeApiClient client;

        public async Task Deploy(string functionName, string image)
        {
            await Deploy(functionName, image, FUNCTIONS_NAMESPACE);
        }

        public KnativePlatform(string configFile)
        {
            this.configFile = configFile;
            KubeClientOptions options;

            if (!string.IsNullOrEmpty(configFile))
            {
                options = K8sConfig.Load(configFile).ToKubeClientOptions(defaultKubeNamespace: FUNCTIONS_NAMESPACE);
            }
            else
            {
                options = K8sConfig.Load().ToKubeClientOptions(defaultKubeNamespace: FUNCTIONS_NAMESPACE);
            }

            client = KubeApiClient.Create(options);
        }

        private async Task Deploy(string name, string image, string nameSpace)
        {
            var isHTTP = IsHTTPTrigger(name);

            await CreateNamespace(nameSpace);
            client.DefaultNamespace = nameSpace;

            ColoredConsole.WriteLine();
            ColoredConsole.WriteLine("Deploying function to Knative...");

            var knativeService = GetKnativeService(name, image, nameSpace, isHTTP);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(knativeService,
                            Newtonsoft.Json.Formatting.None,
                            new Newtonsoft.Json.JsonSerializerSettings
                            {
                                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                            });

            File.WriteAllText("deployment.json", json);
            await KubectlHelper.RunKubectl($"apply -f deployment.json");
            File.Delete("deployment.json");

            var externalIP = await GetIstioClusterIngressIP();
            if (string.IsNullOrEmpty(externalIP))
            {
                ColoredConsole.WriteLine("Couldn't find Istio Cluster Ingress External IP");
                return;
            }

            var host = GetFunctionHost(name, nameSpace);

            ColoredConsole.WriteLine();
            ColoredConsole.WriteLine("Function deployed successfully!");
            ColoredConsole.WriteLine();
            ColoredConsole.WriteLine($"Function URL: http://{externalIP}");
            ColoredConsole.WriteLine($"Function Host: {host}");
            ColoredConsole.WriteLine();
            ColoredConsole.WriteLine("Plese note: it may take a few minutes for the knative service to be reachable");
        }

        private string GetFunctionHost(string functionName, string nameSpace)
        {
            return string.Format("{0}.{1}.example.com", functionName, nameSpace);
        }

        private bool IsHTTPTrigger(string functionName)
        {
            var str = File.ReadAllText(string.Format("{0}/function.json", functionName));
            var jObj = JsonConvert.DeserializeObject<FunctionJson>(str);
            return jObj.bindings.Any(d => d.type == "httpTrigger");
        }

        private KnativeService GetKnativeService(string name, string image, string nameSpace, bool isHTTP)
        {
            var knativeService = new KnativeService();
            knativeService.kind = "Service";
            knativeService.apiVersion = "serving.knative.dev/v1alpha1";
            knativeService.metadata = new Metadata();
            knativeService.metadata.name = name;
            knativeService.metadata.@namespace = nameSpace;

            knativeService.spec = new KnativeSpec();
            knativeService.spec.runLatest = new RunLatest();
            knativeService.spec.runLatest.configuration = new Configuration();
            knativeService.spec.runLatest.configuration.revisionTemplate = new RevisionTemplate();
            knativeService.spec.runLatest.configuration.revisionTemplate.spec = new RevisionTemplateSpec();
            knativeService.spec.runLatest.configuration.revisionTemplate.spec.container = new KnativeContainer();
            knativeService.spec.runLatest.configuration.revisionTemplate.spec.container.image = image;

            knativeService.spec.runLatest.configuration.revisionTemplate.metadata = new RevisionTemplateMetadata();
            knativeService.spec.runLatest.configuration.revisionTemplate.metadata.annotations = new Dictionary<string, string>();

            // opt out of knative scale-to-zero for non-http triggers
            if (!isHTTP)
            {
                knativeService.spec.runLatest.configuration.revisionTemplate.metadata.annotations.Add("autoscaling.knative.dev/minScale", 1.ToString());
            }

            // if (max > 0)
            // {
            //     knativeService.spec.runLatest.configuration.revisionTemplate.metadata.annotations.Add("autoscaling.knative.dev/maxScale", max.ToString());
            // }

            return knativeService;
        }

        private async Task<string> GetIstioClusterIngressIP()
        {
            var gateway = await client.ServicesV1().Get("istio-ingressgateway", "istio-system");
            if (gateway == null)
            {
                return "";
            }

            return gateway.Status.LoadBalancer.Ingress[0].Ip;
        }

        private async Task CreateNamespace(string name)
        {
            await KubectlHelper.RunKubectl($"create ns {name}");
        }

        public void SerializeDeployment(string deploymentName, string image, string serializationFormat)
        {
            throw new NotImplementedException();
        }
    }

    public class Binding
    {
        public string authLevel { get; set; }
        public string type { get; set; }
        public string direction { get; set; }
        public string name { get; set; }
        public List<string> methods { get; set; }
    }

    public class FunctionJson
    {
        public bool disabled { get; set; }
        public List<Binding> bindings { get; set; }
        public string scriptFile { get; set; }
    }
}

