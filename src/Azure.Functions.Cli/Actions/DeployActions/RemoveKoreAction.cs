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
using Azure.Functions.Cli.Kubernetes;
using Colors.Net;
using Fclp;
using Microsoft.Azure.WebJobs.Script;
using static Azure.Functions.Cli.Common.OutputTheme;
using static Colors.Net.StringStaticMethods;

namespace Azure.Functions.Cli.Actions.LocalActions
{
    [Action(Name = "remove", Context = Context.Kubernetes, HelpText = "")]
    internal class RemoveKoreAction : BaseAction
    {
        public string Namespace { get; private set; } = "default";

        public override ICommandLineParserResult ParseArgs(string[] args)
        {
            Parser
                .Setup<string>("namespace")
                .Callback(s => Namespace = s);

            return base.ParseArgs(args);
        }

        public async override Task RunAsync()
        {
            await KubernetesHelper.RemoveKore(Namespace);
        }
    }
}