using System;
using System.Collections.Generic;
using Azure.Functions.Cli.Interfaces;

namespace Azure.Functions.Cli.Actions.DeployActions.Platforms
{
    public static class PlatformFactory
    {
        public static IHostingPlatform CreatePlatform(string name, ISecretsManager secretsManager)
        {
            switch (name)
            {
                case "kubernetes":
                    return new KubernetesPlatform(secretsManager);
                case "knative":
                    return new KnativePlatform(null);
                default:
                    return null;
            }
        }
    }
}