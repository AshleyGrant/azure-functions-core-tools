using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Functions.Cli.Common;
using Colors.Net;
using KubeClient.Models;
using Newtonsoft.Json;
using static Azure.Functions.Cli.Common.OutputTheme;

namespace Azure.Functions.Cli.Kubernetes
{
    public static class KubectlHelper
    {
        public static async Task KubectlCreate(object obj, bool showOutput)
        {
            var payload = JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.None,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                });

            var file = Path.GetTempFileName();
            await FileSystemHelpers.WriteAllTextToFileAsync(file, payload);
            try
            {
                await RunKubectl($"create -f {file}", showOutput: showOutput);
            }
            finally
            {
                FileSystemHelpers.FileDelete(file);
            }
        }

        public static async Task<T> KubectlGet<T>(string resource)
        {

            (var output, var error) = await RunKubectl($"get {resource} --output json");
            return JsonConvert.DeserializeObject<T>(output);
        }

        public static async Task<(string output, string error)> RunKubectl(string cmd, bool ignoreError = false, bool showOutput = false)
        {
            var docker = new Executable("kubectl", cmd);
            var sbError = new StringBuilder();
            var sbOutput = new StringBuilder();

            var exitCode = await docker.RunAsync(l => sbOutput.AppendLine(l), e => sbError.AppendLine(e));

            if (exitCode != 0 && !ignoreError)
            {
                throw new CliException($"Error running {docker.Command}.\n" +
                    $"output: {sbOutput.ToString()}\n{sbError.ToString()}");
            }

            return (sbOutput.ToString(), sbError.ToString());
        }
    }
}