using System.Threading.Tasks;

namespace Azure.Functions.Cli.Interfaces
{
    public interface IHostingPlatform
    {
        Task Deploy(string deploymentName, string image);
        void SerializeDeployment(string deploymentName, string image, string serializationFormat);
    }
}