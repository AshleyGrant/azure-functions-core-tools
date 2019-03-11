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
}