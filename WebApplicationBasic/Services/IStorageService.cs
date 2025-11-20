using System.IO;
using System.Threading.Tasks;

namespace WebApplicationBasic.Services
{
    public interface IStorageService
    {
        /// <summary>
        /// Upload de arquivo para o storage
        /// </summary>
        /// <param name="fileStream">Stream do arquivo</param>
        /// <param name="fileName">Nome do arquivo</param>
        /// <param name="contentType">Tipo MIME do arquivo</param>
        /// <returns>URL pública do arquivo</returns>
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);

        /// <summary>
        /// Deleta um arquivo do storage
        /// </summary>
        /// <param name="fileName">Nome do arquivo a ser deletado</param>
        /// <returns>True se deletado com sucesso</returns>
        Task<bool> DeleteFileAsync(string fileName);

        /// <summary>
        /// Obtém a URL pública de um arquivo
        /// </summary>
        /// <param name="fileName">Nome do arquivo</param>
        /// <returns>URL pública do arquivo</returns>
        string GetFileUrl(string fileName);

        /// <summary>
        /// Verifica se um arquivo existe no storage
        /// </summary>
        /// <param name="fileName">Nome do arquivo</param>
        /// <returns>True se o arquivo existe</returns>
        Task<bool> FileExistsAsync(string fileName);

        /// <summary>
        /// Cria o bucket se não existir
        /// </summary>
        /// <returns>True se criado ou já existe</returns>
        Task<bool> EnsureBucketExistsAsync();
    }
}