using System.Collections.Generic;
using Newtonsoft.Json;

namespace Azure.Functions.Cli.Kubernetes.Models
{
    public class Labels
    {
        public string app { get; set; }
    }

    public class Metadata
    {
        public string name { get; set; }
        [JsonProperty("namespace")]
        public string @namespace { get; set; }
        public Labels labels { get; set; }
    }

    public class MatchLabels
    {
        public string app { get; set; }
    }

    public class Selector
    {
        public MatchLabels matchLabels { get; set; }
    }

    public class Requests
    {
        public string memory { get; set; }
        public string cpu { get; set; }
    }

    public class Resources
    {
        public Requests requests { get; set; }
    }

    public class Port
    {
        public int containerPort { get; set; }
    }

    public class Container
    {
        public string name { get; set; }
        public string image { get; set; }
        public Resources resources { get; set; }
        public List<Port> ports { get; set; }
        public IEnumerable<ContainerEnvironment> env { get; set; }
    }

    public class Toleration
    {
        public string key { get; set; }
        public string effect { get; set; }
    }

    public class TemplateSpec
    {
        public List<Container> containers { get; set; }
        public string dnsPolicy { get; set; }
        public List<Toleration> tolerations { get; set; }
    }

    public class Template
    {
        public Metadata metadata { get; set; }
        public TemplateSpec spec { get; set; }
    }

    public class DeploymentSpec
    {
        public int replicas { get; set; }
        public Selector selector { get; set; }
        public Template template { get; set; }
    }

    public class Deployment
    {
        public string apiVersion { get; set; }
        public string kind { get; set; }
        public Metadata metadata { get; set; }
        public DeploymentSpec spec { get; set; }
    }

    public class Secrets
    {
        public string apiVersion { get; set; }
        public string kind { get; set; }
        public Metadata metadata { get; set; }
        public Dictionary<string, string> data { get; set; }
    }

    public class ScaledObject
    {
        public string apiVersion { get; internal set; }
        public string kind { get; internal set; }
        public Metadata metadata { get; set; }
        public ScaledObjectSpec spec { get; set; }
    }

    public class ScaledObjectSpec
    {
        public ScaledObjectScaleTargetRef scaleTargetRef { get; set; }
        public int? pollingInterval { get; set; }
        public int? cooldownPeriod { get; set; }
        public IEnumerable<ScaledObjectTrigger> triggers { get; internal set; }
    }

    public class ScaledObjectScaleTargetRef
    {
        public string deploymentName { get; set; }
    }

    public class ScaledObjectTrigger
    {
        public string type { get; set; }
        public string name { get; set; }
        public Dictionary<string, string> metadata { get; set; }
    }

    public class KubernetesSearchResult<T>
    {
        public string apiVersion { get; set; }
        public IEnumerable<T> items { get; set; }
    }

    public class ContainerEnvironment
    {
        public string name { get; set; }
        public EnvironmentValueFrom valueFrom { get; set; }
    }

    public class EnvironmentValueFrom
    {
        public ValueFromSecretKeyRef secretKeyRef { get; set; }
    }

    public class ValueFromSecretKeyRef
    {
        public string name { get; set; }
        public string key { get; set; }
    }
}