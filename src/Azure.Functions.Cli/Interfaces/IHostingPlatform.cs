using System.Threading.Tasks;

namespace Azure.Functions.Cli.Interfaces
{
    public interface IHostingPlatform
    {
        Task Deploy(string deploymentName, string image);
        Task SerializeDeployment(string deploymentName, string image, string serializationFormat);
    }
}