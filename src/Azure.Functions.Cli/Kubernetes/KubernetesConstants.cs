using System.Collections.Generic;
using KubeClient.Models;

namespace Azure.Functions.Cli.Kubernetes
{
    public static class KubernetesConstants
    {
        public static readonly CustomResourceDefinitionV1Beta1 ScaledObjectCrd = new CustomResourceDefinitionV1Beta1
        {
            ApiVersion = "apiextensions.k8s.io/v1beta1",
            Kind = "CustomResourceDefinition",
            Metadata = new ObjectMetaV1
            {
                Name = "scaledobjects.kore.k8s.io",
                Annotations = new Dictionary<string, string>
                {
                    { "app", "kore" }
                }
            },
            Spec = new CustomResourceDefinitionSpecV1Beta1
            {
                Group = "kore.k8s.io",
                Version = "v1alpha1",
                Names = new CustomResourceDefinitionNamesV1Beta1
                {
                    Kind = "ScaledObject",
                    Plural = "scaledobjects"
                },
                Scope = "Namespaced"
            }
        };

        public static readonly ServiceAccountV1 KoreServiceAccount = new ServiceAccountV1
        {
            ApiVersion = "",
            Kind = "",
            Metadata = new ObjectMetaV1
            {
                Labels = new Dictionary<string, string>
                {
                    {"", ""}
                },
                Name = "",
                Namespace = ""
            }
        };

        public static readonly ClusterRoleBin
    }
}